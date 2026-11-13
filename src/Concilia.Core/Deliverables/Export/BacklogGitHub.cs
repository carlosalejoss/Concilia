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
// Epics, stories and tasks are issues, joined as sub-issues. In a personal
// repository the level goes as a label (`epica`, `historia`, `tarea`; `epic`,
// `story`, `task` in a project in English, step 140). The requirements go in
// the body, not as labels: a project would fill the repository with hundreds.
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
//
// Step 142 (decisions of the author of 10-11-2026), still with `gh` and no
// token in Concilia:
//   - issue types: in a repository of an organization, the native type of
//     GitHub that it has (Epic, Feature, Task…) instead of the label of the level;
//   - a board of GitHub Projects with every issue, its status (Todo) and two
//     fields, the epic and the iterations; it needs the `project` permission of
//     gh, which the script explains if it is missing;
//   - level 3 of the iterations: on an issue that changed or was deleted in
//     Concilia, a comment once (a mark in the comment says it is there), and
//     still nothing is changed or closed by the script.
public static class BacklogGitHub
{
    public static string EpicLabel(ExportLanguage language) => language.Text("epica", "epic");
    public static string StoryLabel(ExportLanguage language) => language.Text("historia", "story");
    public static string TaskLabel(ExportLanguage language) => language.Text("tarea", "task");

    // An issue of the export. Parent: the id of its parent, empty in an epic.
    // Level: "epica", "historia" or "tarea", the key of the script for the
    // type of issue and the label of the level. Epic and Iterations: the
    // values of the fields of the board ("EP-01 · Reserva de salas", "1, 2").
    public record Issue(string Id, string Parent, string Title, string Body, IReadOnlyList<string> Labels, string Hash,
        string Level = "", string Epic = "", string Iterations = "");

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
        var language = backlog.Language;
        foreach (var epic in backlog.Epics)
        {
            var epicField = BacklogCsv.OneLine(epic.Title) is { Length: > 0 } title ? $"{epic.Id} · {title}" : epic.Id;
            issues.Add(Build(generation, epic.Id, "", epic.Title, Text(epic.Description),
                [EpicLabel(language), .. language.IterationLabels(epic.Iterations)]) with
            { Level = "epica", Epic = epicField, Iterations = string.Join(", ", epic.Iterations) });
            foreach (var story in epic.Stories)
            {
                var iterations = string.Join(", ", story.Iterations);
                issues.Add(Build(generation, story.Id, epic.Id, story.Title, StoryBody(backlog, story),
                    [StoryLabel(language), .. story.PendingDiscrepancy ? new[] { language.PendingDiscrepancyLabel } : [], .. language.IterationLabels(story.Iterations)]) with
                { Level = "historia", Epic = epicField, Iterations = iterations });
                foreach (var task in story.Tasks)
                    issues.Add(Build(generation, task.Id, story.Id, task.Title, language.Text($"Tarea de la historia {story.Id}.", $"Task of the story {story.Id}."),
                        [TaskLabel(language), .. language.IterationLabels(story.Iterations)]) with
                    { Level = "tarea", Epic = epicField, Iterations = iterations });
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

    private static string StoryBody(BacklogExport backlog, ExportStory story)
    {
        var language = backlog.Language;
        var text = new StringBuilder(Text(backlog.Narrative(story))).Append($"\n\n## {language.Text("Criterios de aceptación", "Acceptance criteria")}\n");
        foreach (var c in story.Criteria)
        {
            var gherkin = new StringBuilder($"Scenario: {c.Id} {BacklogCsv.OneLine(c.Scenario)}\n");
            BacklogGherkin.Steps(gherkin, language, "Given", c.Given, "  ");
            BacklogGherkin.Steps(gherkin, language, "When", c.When, "  ");
            BacklogGherkin.Steps(gherkin, language, "Then", c.Then, "  ");
            var fence = Fence(gherkin.ToString());
            text.Append($"\n{fence}gherkin\n{gherkin}{fence}\n");
        }
        return text.Append($"\n## {language.Text("Requisitos", "Requirements")}\n\n{string.Join(", ", story.Requirements)}").ToString();
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

    // Why an issue changed or was deleted, for its comment: "iteración 2: …"
    // (only the stories an update changed have it; null without it).
    public static string? ChangeDetail(ExportLanguage language, ExportChange change)
    {
        var reason = BacklogCsv.OneLine(change.Reason);
        var iterations = change.Iterations.Count switch
        {
            0 => "",
            1 => $"{language.Text("iteración", "iteration")} {change.Iterations[0]}",
            _ => $"{language.Text("iteraciones", "iterations")} {string.Join(", ", change.Iterations)}",
        };
        var detail = iterations.Length > 0 && reason.Length > 0 ? $"{iterations}: {reason}" : iterations + reason;
        return detail.Length > 0 ? Text(detail) : null;
    }

    private static string Script(BacklogExport backlog, IReadOnlyList<Issue> issues, string generation)
    {
        var language = backlog.Language;
        var lines = issues.Select(i => "crear " + string.Join(' ',
            new[] { i.Id, i.Parent, i.Hash, i.Level, i.Epic, i.Iterations, i.Title }.Concat(i.Labels).Select(Quote)));
        var labels = issues.SelectMany(i => i.Labels).Distinct().Select(Quote);
        var levelLabels = $"[epica]={Quote(EpicLabel(language))} [historia]={Quote(StoryLabel(language))} [tarea]={Quote(TaskLabel(language))}";
        var changed = backlog.Epics.SelectMany(e => e.Stories).Select(h => h.LastChange).OfType<ExportChange>();
        return ScriptTemplate
            .Replace("{{PROYECTO}}", BacklogCsv.OneLine(backlog.Project))
            .Replace("{{ESTADO}}", (backlog with { Language = ExportLanguage.Spanish }).Status) // the script is for the analyst (step 140)
            .Replace("{{GENERACION}}", Quote(generation))
            .Replace("{{TABLERO}}", Quote(language.Text($"Backlog de {BacklogCsv.OneLine(backlog.Project)}", $"Backlog of {BacklogCsv.OneLine(backlog.Project)}")))
            .Replace("{{CAMPO_EPICA}}", Quote(language.Text("Épica", "Epic")))
            .Replace("{{CAMPO_ITERACION}}", Quote(language.Text("Iteración", "Iteration")))
            .Replace("{{ETIQUETAS_NIVEL}}", levelLabels)
            .Replace("{{MOTIVOS_CAMBIO}}", Reasons(language, changed))
            .Replace("{{MOTIVOS_ELIMINADO}}", Reasons(language, backlog.Deleted))
            .Replace("{{AVISO_CAMBIO}}", $"{Quote(language.Text("**Concilia:** este elemento ha cambiado en Concilia", "**Concilia:** this item changed in Concilia"))} " +
                Quote(language.Text(
                    ". El cambio no se ha aplicado aquí, para no perder lo editado en GitHub: quien gestiona el backlog decidirá si aplicarlo.",
                    ". The change has not been applied here, so that nothing edited in GitHub is lost: whoever manages the backlog will decide whether to apply it.")))
            .Replace("{{AVISO_ELIMINADO}}", $"{Quote(language.Text("**Concilia:** este elemento se ha eliminado en Concilia", "**Concilia:** this item was deleted in Concilia"))} " +
                Quote(language.Text(". Sigue abierto aquí: ciérralo si ya no hace falta.", ". It is still open here: close it if it is no longer needed.")))
            .Replace("{{ETIQUETAS}}", string.Join(' ', labels))
            .Replace("{{ISSUES}}", string.Join('\n', lines));
    }

    // The contents of an associative array of bash: [HU-002]='iteración 2: …'.
    private static string Reasons(ExportLanguage language, IEnumerable<ExportChange> changes) =>
        string.Join(' ', changes.Select(c => (c.Id, Detail: ChangeDetail(language, c))).Where(c => c.Detail is not null)
            .DistinctBy(c => c.Id).Select(c => $"[{Quote(c.Id)}]={Quote(c.Detail!)}"));

    // A value for the shell: between single quotes nothing is interpreted, so
    // a title with "$(...)" stays text. A quote inside closes, escapes and reopens.
    public static string Quote(string value) => $"'{value.Replace("'", "'\\''")}'";

    // Comments in Spanish: the script is read by the analyst. What it writes in
    // GitHub (issues, board and comments) goes in the language of the project.
    private const string ScriptTemplate = """
        #!/usr/bin/env bash
        # Importa en GitHub el backlog de {{PROYECTO}} ({{ESTADO}}).
        # Generado por Concilia. Uso: bash importar.sh propietario/repositorio [--simular] [--sin-tablero]
        # El README.md explica qué hace. Se puede repetir: lo que ya está no se crea.
        set -euo pipefail
        cd "$(dirname "$0")"
        export MSYS_NO_PATHCONV=1 # en Git Bash, un título que empieza por / no es una ruta

        GENERACION={{GENERACION}}
        PAUSA="${CONCILIA_PAUSA:-2}" # segundos entre issues: GitHub limita a 80 creaciones por minuto
        TABLERO={{TABLERO}}
        CAMPO_EPICA={{CAMPO_EPICA}}
        CAMPO_ITERACION={{CAMPO_ITERACION}}
        declare -A ETIQUETA_NIVEL=({{ETIQUETAS_NIVEL}})
        declare -A MOTIVO_CAMBIO=({{MOTIVOS_CAMBIO}})
        declare -A MOTIVO_ELIMINADO=({{MOTIVOS_ELIMINADO}})
        AVISO_CAMBIO=({{AVISO_CAMBIO}})
        AVISO_ELIMINADO=({{AVISO_ELIMINADO}})

        uso() {
          echo "Uso: bash importar.sh propietario/repositorio [--simular] [--sin-tablero]" >&2
          exit 2
        }
        REPO="${1:-}"
        [[ "$REPO" == */* ]] || uso
        shift
        SIMULAR=""
        SIN_TABLERO=""
        for opcion in "$@"; do
          case "$opcion" in
            --simular) SIMULAR=1 ;;
            --sin-tablero) SIN_TABLERO=1 ;;
            *) uso ;;
          esac
        done
        PROPIETARIO="${REPO%%/*}"

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

        # Los tipos de issue de GitHub (Epic, Feature, Task…) solo existen en las
        # organizaciones: allí, cada nivel lleva el que tenga la organización en
        # lugar de su etiqueta. En una cuenta personal, la etiqueta.
        declare -A TIPO=()
        if [[ "$(gh api "users/$PROPIETARIO" --jq .type | tr -d '\r')" == "Organization" ]]; then
          ayuda=$(gh issue create --help 2>&1 || true)
          if [[ "$ayuda" == *--type* ]]; then
            tipos=$(gh api "orgs/$PROPIETARIO/issue-types" --jq '.[] | select(.is_enabled != false) | .name' 2>/dev/null | tr -d '\r' || true)
            # El primero de la lista que tenga la organización, con su nombre exacto.
            elegir() {
              local tipo
              for tipo in "$@"; do
                if grep -qixF -- "$tipo" <<< "$tipos"; then
                  grep -ixF -m1 -- "$tipo" <<< "$tipos"
                  return 0
                fi
              done
            }
            # Una épica sin el tipo Epic sigue con su etiqueta: con Feature no se
            # distinguiría de una historia.
            TIPO[epica]=$(elegir Epic)
            TIPO[historia]=$(elegir "User Story" Story Feature)
            TIPO[tarea]=$(elegir Task)
          else
            echo "Tu gh no puede poner tipos de issue (--type): cada nivel irá como etiqueta. Actualiza gh para usar los de la organización." >&2
          fi
        fi
        for nivel in epica historia tarea; do
          if [[ -n "${TIPO[$nivel]:-}" ]]; then
            echo "Cada $nivel será un issue de tipo «${TIPO[$nivel]}»."
          else
            echo "Cada $nivel llevará la etiqueta «${ETIQUETA_NIVEL[$nivel]}»."
          fi
        done

        # La etiqueta de un nivel que ya va como tipo de issue no se usa.
        sustituida_por_tipo() {
          local nivel
          for nivel in epica historia tarea; do
            if [[ "$1" == "${ETIQUETA_NIVEL[$nivel]}" && -n "${TIPO[$nivel]:-}" ]]; then
              return 0
            fi
          done
          return 1
        }

        # El tablero necesita el permiso «project» de gh.
        if [[ -z "$SIN_TABLERO" ]]; then
          estado=$(gh auth status 2>&1 || true)
          if [[ "$estado" != *"'project'"* ]]; then
            echo "Sin tablero: gh no tiene el permiso «project». Para el tablero, ejecuta gh auth refresh -s project y vuelve a lanzar el script; añadirá al tablero lo que ya esté creado." >&2
            SIN_TABLERO=1
          fi
        fi

        # Lo que ya hay en el repositorio, por el id de Concilia de cada issue.
        declare -A URL HASH ABIERTO VISTO EPICA ITERACIONES CAMBIADO
        ORDEN=()
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

        if [[ -z "$SIMULAR" ]]; then
          for etiqueta in {{ETIQUETAS}}; do
            if sustituida_por_tipo "$etiqueta"; then
              continue
            fi
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

        # crear id padre hash nivel épica iteraciones título etiquetas...
        crear() {
          local id=$1 padre=$2 hash=$3 nivel=$4 epica=$5 iteraciones=$6 titulo=$7
          shift 7
          VISTO[$id]=1
          ORDEN+=("$id")
          EPICA[$id]=$epica
          ITERACIONES[$id]=$iteraciones
          if [[ -n "${URL[$id]:-}" ]]; then
            if [[ "${HASH[$id]}" == "$hash" || -z "${ABIERTO[$id]:-}" ]]; then
              iguales=$((iguales + 1))
            else
              modificados+=("gh issue edit ${URL[$id]} --title $(comillas "$titulo") --body-file cuerpos/$id.md")
              CAMBIADO[$id]=$hash
            fi
            return 0
          fi
          creados=$((creados + 1))
          if [[ -n "$SIMULAR" ]]; then
            echo "Se crearía: $titulo"
            URL[$id]="(nuevo)"
            return 0
          fi
          local opciones=(--repo "$REPO" --title "$titulo" --body-file "cuerpos/$id.md")
          local tipo=${TIPO[$nivel]:-}
          if [[ -n "$tipo" ]]; then
            opciones+=(--type "$tipo")
          fi
          local etiqueta
          for etiqueta in "$@"; do
            if [[ -n "$tipo" && "$etiqueta" == "${ETIQUETA_NIVEL[$nivel]}" ]]; then
              continue
            fi
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
        ids_eliminados=()
        for id in "${!URL[@]}"; do
          if [[ -z "${VISTO[$id]:-}" && -n "${ABIERTO[$id]:-}" ]]; then
            eliminados+=("gh issue close ${URL[$id]}  # $id")
            ids_eliminados+=("$id")
          fi
        done

        # El tablero: el de este backlog si ya existe, o uno nuevo unido al
        # repositorio. Cada issue que no esté en él se añade con su estado
        # (Todo) y los campos de la épica y las iteraciones.
        en_tablero=0
        if [[ -z "$SIN_TABLERO" && -n "$SIMULAR" ]]; then
          echo "Se añadirían al tablero «$TABLERO» los issues que no estén ya en él."
        elif [[ -z "$SIN_TABLERO" ]]; then
          proyecto=""
          lista=$(gh project list --owner "$PROPIETARIO" --limit 500 --format json --jq '.projects[] | [.number, .title] | @tsv' | tr -d '\r')
          while IFS=$'\t' read -r numero titulo; do
            if [[ "$titulo" == "$TABLERO" ]]; then
              proyecto=$numero
              break
            fi
          done <<< "$lista"
          if [[ -z "$proyecto" ]]; then
            proyecto=$(gh project create --owner "$PROPIETARIO" --title "$TABLERO" --format json --jq .number | tr -d '\r')
            echo "Tablero creado: $TABLERO"
          fi
          gh project link "$proyecto" --owner "$PROPIETARIO" --repo "$REPO" > /dev/null 2>&1 || true # si ya está unido, se queda
          proyecto_id=$(gh project view "$proyecto" --owner "$PROPIETARIO" --format json --jq .id | tr -d '\r')

          declare -A CAMPO=()
          campos=$(gh project field-list "$proyecto" --owner "$PROPIETARIO" --limit 100 --format json --jq '.fields[] | [.id, .name] | @tsv' | tr -d '\r')
          while IFS=$'\t' read -r campo nombre; do
            if [[ -n "$campo" ]]; then
              CAMPO[$nombre]=$campo
            fi
          done <<< "$campos"
          for nombre in "$CAMPO_EPICA" "$CAMPO_ITERACION"; do
            if [[ -z "${CAMPO[$nombre]:-}" ]]; then
              CAMPO[$nombre]=$(gh project field-create "$proyecto" --owner "$PROPIETARIO" --name "$nombre" --data-type TEXT --format json --jq .id | tr -d '\r')
            fi
          done
          # La opción «Todo» del campo Status que trae cada tablero (o la primera).
          pendiente=""
          opciones=$(gh project field-list "$proyecto" --owner "$PROPIETARIO" --limit 100 --format json --jq '.fields[] | select(.name == "Status") | .options[]? | [.id, .name] | @tsv' | tr -d '\r')
          while IFS=$'\t' read -r opcion nombre; do
            [[ -n "$opcion" ]] || continue
            if [[ -z "$pendiente" || "$nombre" == "Todo" ]]; then
              pendiente=$opcion
            fi
          done <<< "$opciones"

          declare -A YA_EN_TABLERO=()
          items=$(gh project item-list "$proyecto" --owner "$PROPIETARIO" --limit 5000 --format json --jq '.items[] | .content.url // empty' | tr -d '\r')
          while IFS= read -r url; do
            if [[ -n "$url" ]]; then
              YA_EN_TABLERO[$url]=1
            fi
          done <<< "$items"

          for id in ${ORDEN[@]+"${ORDEN[@]}"}; do
            url=${URL[$id]:-}
            [[ -n "$url" && -z "${YA_EN_TABLERO[$url]:-}" ]] || continue
            item=$(gh project item-add "$proyecto" --owner "$PROPIETARIO" --url "$url" --format json --jq .id | tr -d '\r')
            if [[ -n "${CAMPO[Status]:-}" && -n "$pendiente" ]]; then
              gh project item-edit --id "$item" --project-id "$proyecto_id" --field-id "${CAMPO[Status]}" --single-select-option-id "$pendiente" > /dev/null
            fi
            gh project item-edit --id "$item" --project-id "$proyecto_id" --field-id "${CAMPO[$CAMPO_EPICA]}" --text "${EPICA[$id]}" > /dev/null
            if [[ -n "${ITERACIONES[$id]}" ]]; then
              gh project item-edit --id "$item" --project-id "$proyecto_id" --field-id "${CAMPO[$CAMPO_ITERACION]}" --text "${ITERACIONES[$id]}" > /dev/null
            fi
            en_tablero=$((en_tablero + 1))
            sleep "$PAUSA"
          done
        fi

        # Un comentario en cada issue abierto que cambió o se eliminó en Concilia,
        # una sola vez: la marca del final del comentario dice que ya está
        # (con el hash del cambio, así que un cambio nuevo vuelve a avisar).
        comentados=0
        AVISO=""
        avisar() { # url clave texto
          local url=$1 marca="<!-- concilia-aviso:$2 -->" comentarios
          comentarios=$(gh issue view "$url" --json comments --jq '.comments[].body' | tr -d '\r')
          if [[ "$comentarios" == *"$marca"* ]]; then
            return 0
          fi
          comentados=$((comentados + 1))
          if [[ -n "$SIMULAR" ]]; then
            echo "Se comentaría en: $url"
            return 0
          fi
          printf '%s\n\n%s\n' "$3" "$marca" > "$AVISO"
          gh issue comment "$url" --body-file "$AVISO" > /dev/null
          sleep "$PAUSA"
        }
        detalle() {
          if [[ -n "$1" ]]; then
            printf ' (%s)' "$1"
          fi
        }
        AVISO=$(mktemp)
        trap 'rm -f "$AVISO"' EXIT
        for id in "${!CAMBIADO[@]}"; do
          avisar "${URL[$id]}" "$id hash=${CAMBIADO[$id]}" "${AVISO_CAMBIO[0]}$(detalle "${MOTIVO_CAMBIO[$id]:-}")${AVISO_CAMBIO[1]}"
        done
        for id in ${ids_eliminados[@]+"${ids_eliminados[@]}"}; do
          avisar "${URL[$id]}" "$id eliminado" "${AVISO_ELIMINADO[0]}$(detalle "${MOTIVO_ELIMINADO[$id]:-}")${AVISO_ELIMINADO[1]}"
        done

        echo
        echo "Creados: $creados. Sin cambios: $iguales. Modificados en Concilia: ${#modificados[@]}. Eliminados en Concilia: ${#eliminados[@]}."
        if [[ -z "$SIN_TABLERO" && -z "$SIMULAR" ]]; then
          echo "Añadidos al tablero «$TABLERO»: $en_tablero."
        fi
        if (( comentados > 0 )); then
          echo "Comentarios nuevos en los issues que cambiaron o se eliminaron: $comentados."
        fi
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

    // The README is for the analyst, so it stays in Spanish (step 140).
    private static string Readme(BacklogExport backlog, IReadOnlyList<Issue> issues) => Readme(backlog with { Language = ExportLanguage.Spanish }, issues.Count);

    private static string Readme(BacklogExport backlog, int count) => $"""
        # Backlog de {BacklogCsv.OneLine(backlog.Project)} para GitHub

        {backlog.Status}, {backlog.Generated}. {count} issues: épicas, historias y tareas, unidas como sub-issues.

        ## Antes de empezar

        - Instala [GitHub CLI](https://cli.github.com), versión 2.94 o superior, e inicia sesión con `gh auth login`.
        - Para el tablero, dale a `gh` el permiso de los tableros: `gh auth refresh -s project`. Sin él, el script crea los issues y avisa.
        - En Windows, usa Git Bash.
        - Elige un repositorio privado sin seguidores: cada issue y cada comentario envían una notificación.

        ## Importar

        En esta carpeta:

        ```
        bash importar.sh propietario/repositorio --simular
        bash importar.sh propietario/repositorio
        ```

        El primero solo dice qué se crearía. El segundo crea los issues, con una pausa entre uno y otro. Si GitHub lo corta por sus límites, vuelve a lanzarlo unos minutos después: seguirá por donde iba.

        - **En una organización,** cada nivel lleva el tipo de issue de GitHub que tenga la organización en lugar de su etiqueta: una épica, Epic; una historia, User Story, Story o Feature (el primero que haya), y una tarea, Task. Por defecto, una organización tiene Task, Bug y Feature: para que las épicas tengan el suyo, crea el tipo «Epic» (*Settings → Planning → Issue types*). Lo que no tenga tipo va con su etiqueta, como en una cuenta personal.
        - **El tablero:** el script añade los issues a un tablero de GitHub Projects, «Backlog de…», unido al repositorio, con el estado *Todo* y los campos de la épica y las iteraciones. Si ya existe, lo reutiliza y solo añade lo que falte. Para no usarlo, añade `--sin-tablero`.

        ## Después de una iteración

        Descarga de nuevo este `.zip` y lanza el mismo comando:

        - lo nuevo se crea, bajo su padre, y se añade al tablero;
        - lo que cambió en Concilia **no se toca**, para no perder lo editado en GitHub: el script da el comando que lo aplica;
        - lo que se eliminó en Concilia tampoco se cierra solo: el script da el comando;
        - en cada issue abierto que cambió o se eliminó, el script deja un comentario que lo dice, con el motivo de la iteración si lo hay, una sola vez aunque lo lances de nuevo;
        - lo que hayas cerrado en GitHub no se recrea ni se avisa.

        Aplicar un cambio sustituye el título y el cuerpo enteros de ese issue.

        ## Cuidado

        - No borres la última línea del cuerpo de un issue, invisible al leerlo (`<!-- concilia:… -->`): es lo que lo identifica. Sin ella, el script lo volvería a crear. La última línea de los comentarios del script (`<!-- concilia-aviso:… -->`) evita que se repitan.
        - Un backlog generado desde cero en Concilia vuelve a numerar sus ids, así que necesita otro repositorio: si el script lo detecta, se para sin crear nada.

        """;

    private static void Add(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content.ReplaceLineEndings("\n"));
    }
}
