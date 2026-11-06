using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Concilia.Core.Deliverables.Export;

// The backlog for GitHub (step 124). GitHub has no importer of files, unlike
// Jira and Azure DevOps, so the export is a .zip with what is needed to create
// the issues with the GitHub CLI (`gh`), run by the user: Concilia never sees a
// token. Decisions of the author (06-11-2026):
//
//   importar.sh       one line per issue: epic → story → task
//   cuerpos/HU-001.md the body of each issue
//   README.md         how to use it
//
// Epics, stories and tasks are issues, joined as sub-issues. As it is a
// personal repository, the type goes as a label (`epica`, `historia`, `tarea`):
// issue types only exist in organizations. The requirements go in the body, not
// as labels: a project would fill the repository with hundreds of them.
//
// The state lives in GitHub. The last line of each body is a mark, invisible
// when the issue is read:
//
//   <!-- concilia:HU-001 generacion=ab12cd34 hash=0123456789ab -->
//
// so the script compares the issues of the repository with the .zip and needs
// no file of its own (the .zip of the next iteration is a new download):
//   - what is not in GitHub is created, under its parent;
//   - if the hash differs, it changed in Concilia: it is listed with the command
//     that applies it, because editing it could overwrite work done in GitHub;
//   - what is in GitHub and not in the .zip was deleted: listed the same way.
// Running it twice creates nothing new, and a run cut short can be repeated.
//
// `generacion` is needed because ids are stable only through updates: a backlog
// generated from scratch numbers from 1 again, so its HU-012 can be another
// story. If the repository holds another generation, the script stops.
public static class BacklogGitHub
{
    public const string EpicLabel = "epica";
    public const string StoryLabel = "historia";
    public const string TaskLabel = "tarea";

    // An issue of the export. Parent: the id of its parent, empty in an epic.
    public record Issue(string Id, string Parent, string Title, string Body, IReadOnlyList<string> Labels, string Hash);

    // generation: the job that generated the backlog from scratch
    // (DeliverableVersions.GenerationAsync); its first characters identify it.
    public static byte[] Write(BacklogExport backlog, Guid generation)
    {
        var mark = Generation(generation);
        var issues = Issues(backlog, mark);
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            Add(zip, "README.md", Readme(backlog, issues));
            Add(zip, "importar.sh", Script(backlog, issues, mark));
            foreach (var issue in issues)
                Add(zip, $"cuerpos/{issue.Id}.md", issue.Body);
        }
        return stream.ToArray();
    }

    public static string Generation(Guid job) => job.ToString("N")[..8];

    // Each epic followed by its stories and each story by its tasks, because a
    // parent has to exist before its sub-issues.
    public static IReadOnlyList<Issue> Issues(BacklogExport backlog, string generation)
    {
        var issues = new List<Issue>();
        foreach (var epic in backlog.Epics)
        {
            issues.Add(Build(generation, epic.Id, "", epic.Title, Text(epic.Description),
                [EpicLabel, .. BacklogCsv.IterationLabels(epic.Iterations)]));
            foreach (var story in epic.Stories)
            {
                issues.Add(Build(generation, story.Id, epic.Id, story.Title, StoryBody(story),
                    [StoryLabel, .. story.PendingDiscrepancy ? new[] { BacklogCsv.PendingDiscrepancyLabel } : [], .. BacklogCsv.IterationLabels(story.Iterations)]));
                foreach (var task in story.Tasks)
                    issues.Add(Build(generation, task.Id, story.Id, task.Title, $"Tarea de la historia {story.Id}.",
                        [TaskLabel, .. BacklogCsv.IterationLabels(story.Iterations)]));
            }
        }
        return issues;
    }

    // The hash covers what the issue says and where it hangs, not its labels: a
    // discrepancy reviewed changes a label, not the story.
    private static Issue Build(string generation, string id, string parent, string title, string body, IReadOnlyList<string> labels)
    {
        var fullTitle = BacklogCsv.OneLine(title) is { Length: > 0 } line ? $"{id}: {line}" : id;
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{fullTitle}\n{parent}\n{body}")))[..12];
        var mark = $"<!-- concilia:{id} generacion={generation} hash={hash} -->";
        return new Issue(id, parent, fullTitle, body.Length > 0 ? $"{body}\n\n{mark}\n" : $"{mark}\n", labels, hash);
    }

    private static string StoryBody(ExportStory story)
    {
        var text = new StringBuilder(Text(story.Narrative)).Append("\n\n## Criterios de aceptación\n");
        foreach (var c in story.Criteria)
        {
            var gherkin = new StringBuilder($"Scenario: {c.Id} {BacklogCsv.OneLine(c.Scenario)}\n");
            BacklogGherkin.Steps(gherkin, "Given", c.Given, "  ");
            BacklogGherkin.Steps(gherkin, "When", c.When, "  ");
            BacklogGherkin.Steps(gherkin, "Then", c.Then, "  ");
            var fence = Fence(gherkin.ToString());
            text.Append($"\n{fence}gherkin\n{gherkin}{fence}\n");
        }
        return text.Append($"\n## Requisitos\n\n{string.Join(", ", story.Requirements)}").ToString();
    }

    // A code block is closed by a run of backticks as long as the one that
    // opened it, so the fence has to be longer than any run in the text.
    private static string Fence(string code) =>
        new('`', Math.Max(3, Regex.Matches(code, "`+").Select(m => m.Length).DefaultIfEmpty(0).Max() + 1));

    // Free text in Markdown, written by a model or by the user. Three things
    // that do not happen in Jira or Azure DevOps: "@ana" would notify a real
    // user of GitHub and "#12" would link to another issue, so both go as code;
    // and "<reglas>" would be read as an HTML tag and disappear, so "<" is escaped
    // (which also keeps a text from forging the mark of the last line).
    private static readonly Regex References = new(@"(?<![\w`/])(@[A-Za-z0-9][A-Za-z0-9-]*|#\d+)", RegexOptions.Compiled);

    public static string Text(string value) =>
        References.Replace(value.ReplaceLineEndings("\n").Trim().Replace("<", "\\<"), "`$1`");

    private static string Script(BacklogExport backlog, IReadOnlyList<Issue> issues, string generation)
    {
        var lines = issues.Select(i => "crear " + string.Join(' ', new[] { i.Id, i.Parent, i.Hash, i.Title }.Concat(i.Labels).Select(Quote)));
        var labels = issues.SelectMany(i => i.Labels).Distinct().Select(Quote);
        return ScriptTemplate
            .Replace("{{PROYECTO}}", BacklogCsv.OneLine(backlog.Project))
            .Replace("{{ESTADO}}", backlog.Status)
            .Replace("{{GENERACION}}", Quote(generation))
            .Replace("{{ETIQUETAS}}", string.Join(' ', labels))
            .Replace("{{ISSUES}}", string.Join('\n', lines));
    }

    // A value for the shell: between single quotes nothing is interpreted, so
    // a title with "$(...)" stays text. A quote inside closes, escapes and reopens.
    public static string Quote(string value) => $"'{value.Replace("'", "'\\''")}'";

    private const string ScriptTemplate = """
        #!/usr/bin/env bash
        # Importa en GitHub el backlog de {{PROYECTO}} ({{ESTADO}}).
        # Generado por Concilia. Uso: bash importar.sh propietario/repositorio [--simular]
        # El README.md explica qué hace. Se puede repetir: lo que ya está no se crea.
        set -euo pipefail
        cd "$(dirname "$0")"
        export MSYS_NO_PATHCONV=1 # en Git Bash, un título que empieza por / no es una ruta

        GENERACION={{GENERACION}}
        PAUSA=2 # segundos entre issues: GitHub limita a 80 creaciones por minuto
        REPO="${1:-}"
        MODO="${2:-}"

        if [[ -z "$REPO" || ( -n "$MODO" && "$MODO" != "--simular" ) ]]; then
          echo "Uso: bash importar.sh propietario/repositorio [--simular]" >&2
          exit 2
        fi
        if (( BASH_VERSINFO[0] < 4 )); then
          echo "Hace falta bash 4 o superior." >&2
          exit 1
        fi
        if ! command -v gh > /dev/null; then
          echo "Hace falta GitHub CLI (gh): https://cli.github.com" >&2
          exit 1
        fi
        version=$(gh --version | sed -n 's/^gh version \([0-9]*\)\.\([0-9]*\).*/\1 \2/p')
        read -r mayor menor <<< "$version"
        if (( mayor < 2 || ( mayor == 2 && menor < 94 ) )); then
          echo "Hace falta gh 2.94 o superior para crear sub-issues; tienes la $mayor.$menor." >&2
          exit 1
        fi
        trap 'echo "Se ha parado antes de terminar. Lo creado se queda: vuelve a ejecutarlo dentro de unos minutos y seguirá por donde iba." >&2' ERR

        # Lo que ya hay en el repositorio, por el id de Concilia de cada issue.
        declare -A URL HASH ABIERTO VISTO
        filtro='.[] | .url as $url | .state as $estado
          | .body | capture("<!-- concilia:(?<id>[^ ]+) generacion=(?<generacion>[^ ]+) hash=(?<hash>[^ ]+) -->")
          | [.id, .generacion, .hash, $url, $estado] | @tsv'
        existentes=$(gh issue list --repo "$REPO" --state all --limit 5000 --json url,state,body --jq "$filtro" | tr -d '\r')
        while IFS=$'\t' read -r id generacion hash url estado; do
          [[ -n "$id" ]] || continue
          if [[ "$generacion" != "$GENERACION" ]]; then
            trap - ERR
            echo "El repositorio tiene issues de otra generación de este backlog o de otro proyecto (por ejemplo, $url)." >&2
            echo "Un backlog generado desde cero vuelve a numerar sus ids, así que HU-001 puede ser otra historia. Usa otro repositorio." >&2
            exit 1
          fi
          URL[$id]=$url
          HASH[$id]=$hash
          if [[ "$estado" == "OPEN" ]]; then
            ABIERTO[$id]=1
          fi
        done <<< "$existentes"

        if [[ "$MODO" != "--simular" ]]; then
          for etiqueta in {{ETIQUETAS}}; do
            gh label create "$etiqueta" --repo "$REPO" > /dev/null 2>&1 || true # si ya existe, se queda como está
          done
        fi

        creados=0
        iguales=0
        modificados=()

        # Un texto entre comillas simples, para copiarlo en un comando.
        comillas() {
          local escapada="'\\''"
          printf "'%s'" "${1//\'/$escapada}"
        }

        # crear id padre hash título etiquetas...
        crear() {
          local id=$1 padre=$2 hash=$3 titulo=$4
          shift 4
          VISTO[$id]=1
          if [[ -n "${URL[$id]:-}" ]]; then
            if [[ "${HASH[$id]}" == "$hash" || -z "${ABIERTO[$id]:-}" ]]; then
              iguales=$((iguales + 1))
            else
              modificados+=("gh issue edit ${URL[$id]} --title $(comillas "$titulo") --body-file cuerpos/$id.md")
            fi
            return 0
          fi
          creados=$((creados + 1))
          if [[ "$MODO" == "--simular" ]]; then
            echo "Se crearía: $titulo"
            URL[$id]="(nuevo)"
            return 0
          fi
          local opciones=(--repo "$REPO" --title "$titulo" --body-file "cuerpos/$id.md")
          local etiqueta
          for etiqueta in "$@"; do
            opciones+=(--label "$etiqueta")
          done
          if [[ -n "$padre" ]]; then
            opciones+=(--parent "${URL[$padre]}")
          fi
          URL[$id]=$(gh issue create "${opciones[@]}" | tr -d '\r')
          echo "Creado: $titulo"
          sleep "$PAUSA"
        }

        {{ISSUES}}

        # Lo que está abierto en GitHub y ya no está en el backlog.
        eliminados=()
        for id in "${!URL[@]}"; do
          if [[ -z "${VISTO[$id]:-}" && -n "${ABIERTO[$id]:-}" ]]; then
            eliminados+=("gh issue close ${URL[$id]}  # $id")
          fi
        done

        echo
        echo "Creados: $creados. Sin cambios: $iguales. Modificados en Concilia: ${#modificados[@]}. Eliminados en Concilia: ${#eliminados[@]}."
        if (( ${#modificados[@]} > 0 )); then
          echo
          echo "Modificados en Concilia. No se tocan, porque se perdería lo editado en GitHub."
          echo "Para aplicar uno, ejecuta su línea en esta carpeta (sustituye el título y el cuerpo):"
          printf '  %s\n' "${modificados[@]}"
        fi
        if (( ${#eliminados[@]} > 0 )); then
          echo
          echo "Eliminados en Concilia. Siguen abiertos en GitHub; para cerrar uno, ejecuta su línea:"
          printf '  %s\n' "${eliminados[@]}"
        fi
        """;

    private static string Readme(BacklogExport backlog, IReadOnlyList<Issue> issues) => $"""
        # Backlog de {BacklogCsv.OneLine(backlog.Project)} para GitHub

        {backlog.Status}, {backlog.Generated}. {issues.Count} issues: épicas, historias y tareas, unidas como sub-issues.

        ## Antes de empezar

        - Instala [GitHub CLI](https://cli.github.com), versión 2.94 o superior, e inicia sesión con `gh auth login`.
        - En Windows, usa Git Bash.
        - Elige un repositorio privado sin seguidores: cada issue creado envía una notificación.

        ## Importar

        En esta carpeta:

        ```
        bash importar.sh propietario/repositorio --simular
        bash importar.sh propietario/repositorio
        ```

        El primero solo dice qué se crearía. El segundo crea los issues, con una pausa entre uno y otro. Si GitHub lo corta por sus límites, vuelve a lanzarlo unos minutos después: seguirá por donde iba.

        ## Después de una iteración

        Descarga de nuevo este `.zip` y lanza el mismo comando:

        - lo nuevo se crea, bajo su padre;
        - lo que cambió en Concilia **no se toca**, para no perder lo editado en GitHub: el script da el comando que lo aplica;
        - lo que se eliminó en Concilia tampoco se cierra solo: el script da el comando;
        - lo que hayas cerrado en GitHub no se recrea ni se avisa.

        Aplicar un cambio sustituye el título y el cuerpo enteros de ese issue.

        ## Cuidado

        - No borres la última línea del cuerpo de un issue, invisible al leerlo (`<!-- concilia:… -->`): es lo que lo identifica. Sin ella, el script lo volvería a crear.
        - Un backlog generado desde cero en Concilia vuelve a numerar sus ids, así que necesita otro repositorio: si el script lo detecta, se para sin crear nada.

        """;

    private static void Add(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content.ReplaceLineEndings("\n"));
    }
}
