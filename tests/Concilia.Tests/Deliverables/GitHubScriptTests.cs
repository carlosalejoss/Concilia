using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Concilia.Core.Deliverables.Export;

namespace Concilia.Tests.Deliverables;

// Step 142: what importar.sh does against a repository, run with bash and a
// fake `gh` that keeps the issues, boards and comments in a folder and writes
// down every call (no test calls GitHub). Issue types in an organization, the
// board of GitHub Projects and the comments of level 3 of the iterations.
public class GitHubScriptTests
{
    private const string Backlog = """
        {"epicas": [
          {"id": "EP-01", "titulo": "Reserva de salas", "descripcion": "Reservar salas.",
           "historias": [
             {"id": "HU-001", "titulo": "Reservar una sala", "como": "empleado", "quiero": "reservar una sala", "para": "tenerla asegurada",
              "requisitos": ["RF-001"], "criterios": [], "tareas": [{"id": "T-001", "titulo": "Pantalla de nueva reserva"}],
              "origen": "ambas", "discrepancia": true, "explicacion": "interna", "revisada": false}
           ]}
        ]}
        """;

    // The next iteration: HU-001 modified by an update (with its history) and T-001 gone.
    private const string Updated = """
        {"epicas": [
          {"id": "EP-01", "titulo": "Reserva de salas", "descripcion": "Reservar salas.",
           "historias": [
             {"id": "HU-001", "titulo": "Reservar una sala por horas", "como": "empleado", "quiero": "reservar una sala por horas", "para": "tenerla asegurada",
              "requisitos": ["RF-001"], "criterios": [], "tareas": [],
              "origen": "ambas", "discrepancia": false, "explicacion": "interna", "revisada": false,
              "historial": [{"accion": "modificada", "iteraciones": [2], "motivo": "La iteración 2 reserva por horas & no por días"}]}
           ]}
        ]}
        """;

    private static readonly Guid Generation = Guid.Parse("ab12cd34-0000-0000-0000-000000000000");
    private static readonly Dictionary<string, IReadOnlyList<int>> Iterations = new() { ["RF-001"] = [1] };

    private static byte[] Zip(string json) =>
        BacklogGitHub.Write(BacklogExport.Build("Reserva Salas", true, DateTimeOffset.Now, DateTimeOffset.Now, json, Iterations), Generation);

    [BashFact]
    public void In_a_personal_repository_the_level_is_a_label_and_the_issues_go_to_a_board()
    {
        using var gh = new FakeGitHub(owner: "User", projectScope: true);

        var first = gh.Run(Zip(Backlog));

        Assert.Equal(3, gh.Calls("issue create").Count);
        Assert.Contains(gh.Calls("issue create"), c => c.Contains("--label epica") && !c.Contains("--type"));
        Assert.Contains(gh.Calls("issue create"), c => c.Contains("--label historia --label discrepancia-sin-revisar --label iteracion-1"));
        Assert.Single(gh.Calls("project create"), c => c.Contains("--owner autor --title Backlog de Reserva Salas"));
        Assert.Single(gh.Calls("project link"), c => c.Contains("--repo autor/repo"));
        Assert.Equal(2, gh.Calls("project field-create").Count); // Épica and Iteración
        Assert.Equal(3, gh.Calls("project item-add").Count);
        Assert.Contains(gh.Calls("project item-edit"), c => c.Contains("--single-select-option-id OPT_TODO"));
        Assert.Contains(gh.Calls("project item-edit"), c => c.Contains("--text EP-01 · Reserva de salas"));
        Assert.Contains(gh.Calls("project item-edit"), c => c.EndsWith("--text 1"));
        Assert.Contains("Añadidos al tablero «Backlog de Reserva Salas»: 3.", first);

        // Again: nothing new, the same board, nothing added twice.
        gh.ClearCalls();
        var second = gh.Run(Zip(Backlog));
        Assert.Contains("Creados: 0. Sin cambios: 3.", second);
        Assert.Empty(gh.Calls("issue create"));
        Assert.Empty(gh.Calls("project create"));
        Assert.Empty(gh.Calls("project field-create"));
        Assert.Empty(gh.Calls("project item-add"));
    }

    [BashFact]
    public void In_an_organization_each_level_takes_the_issue_type_it_has()
    {
        using var gh = new FakeGitHub(owner: "Organization", projectScope: true, issueTypes: ["Task", "Bug", "Feature", "Epic"]);

        var output = gh.Run(Zip(Backlog));

        var created = gh.Calls("issue create");
        Assert.Contains(created, c => c.Contains("--type Epic") && !c.Contains("--label epica"));
        Assert.Contains(created, c => c.Contains("--type Feature --label discrepancia-sin-revisar") && !c.Contains("--label historia"));
        Assert.Contains(created, c => c.Contains("--type Task") && !c.Contains("--label tarea"));
        // The labels of the levels are not created; the others are.
        Assert.DoesNotContain(gh.Calls("label create"), c => c.Contains("label create epica") || c.Contains("label create historia"));
        Assert.Contains(gh.Calls("label create"), c => c.Contains("label create discrepancia-sin-revisar"));
        Assert.Contains("Cada historia será un issue de tipo «Feature».", output);
    }

    [BashFact]
    public void Without_an_issue_type_for_a_level_it_keeps_its_label()
    {
        // The default types of an organization: no Epic.
        using var gh = new FakeGitHub(owner: "Organization", projectScope: true, issueTypes: ["Task", "Bug", "Feature"]);

        gh.Run(Zip(Backlog));

        // An epic does not take Feature, which would not tell it from a story.
        Assert.Contains(gh.Calls("issue create"), c => c.Contains("--title EP-01") && c.Contains("--label epica") && !c.Contains("--type"));
        Assert.Contains(gh.Calls("issue create"), c => c.Contains("--title HU-001") && c.Contains("--type Feature"));
    }

    [BashFact]
    public void Without_the_project_permission_the_issues_are_created_and_the_script_says_how_to_add_it()
    {
        using var gh = new FakeGitHub(owner: "User", projectScope: false);

        var output = gh.Run(Zip(Backlog));

        Assert.Equal(3, gh.Calls("issue create").Count);
        Assert.Empty(gh.Calls("project"));
        Assert.Contains("gh auth refresh -s project", output);
    }

    [BashFact]
    public void After_an_iteration_each_changed_or_deleted_issue_gets_one_comment()
    {
        using var gh = new FakeGitHub(owner: "User", projectScope: false);
        gh.Run(Zip(Backlog));

        var output = gh.Run(Zip(Updated));

        var comments = gh.Comments();
        Assert.Equal(2, comments.Count);
        Assert.Contains(comments, c => c.Contains("**Concilia:** este elemento ha cambiado en Concilia (iteración 2: La iteración 2 reserva por horas & no por días). El cambio no se ha aplicado aquí")
                                       && c.Contains("<!-- concilia-aviso:HU-001 hash="));
        Assert.Contains(comments, c => c.Contains("**Concilia:** este elemento se ha eliminado en Concilia. Sigue abierto aquí") && c.Contains("<!-- concilia-aviso:T-001 eliminado -->"));
        Assert.Contains("Comentarios nuevos en los issues que cambiaron o se eliminaron: 2.", output);
        // Nothing is changed or closed.
        Assert.Empty(gh.Calls("issue edit"));
        Assert.Empty(gh.Calls("issue close"));

        // Again: the marks say the comments are there.
        gh.Run(Zip(Updated));
        Assert.Equal(2, gh.Comments().Count);
    }

    [BashFact]
    public void Simulating_creates_comments_and_boards_nothing()
    {
        using var gh = new FakeGitHub(owner: "User", projectScope: true);

        var output = gh.Run(Zip(Backlog), "--simular");

        Assert.Empty(gh.Calls("issue create"));
        Assert.Empty(gh.Calls("label create"));
        Assert.Empty(gh.Calls("project"));
        Assert.Contains("Se crearía: HU-001: Reservar una sala", output);
        Assert.Contains("Se añadirían al tablero «Backlog de Reserva Salas»", output);
    }

    // A repository behind a fake gh: the .zip is unpacked in a folder of its
    // own and run there with bash; the state of the fake stays between runs.
    private sealed class FakeGitHub : IDisposable
    {
        private readonly TempDirectory _temp = new();
        private readonly string _state;
        private readonly string _bin;
        private readonly Dictionary<string, string> _environment;
        private int _runs;

        public FakeGitHub(string owner, bool projectScope, string[]? issueTypes = null)
        {
            _state = Directory.CreateDirectory(Path.Combine(_temp.Path, "gh")).FullName;
            _bin = Directory.CreateDirectory(Path.Combine(_temp.Path, "bin")).FullName;
            File.WriteAllText(Path.Combine(_bin, "gh"), FakeGh.ReplaceLineEndings("\n"));
            File.WriteAllText(Path.Combine(_state, "tipos.txt"), string.Join("\n", issueTypes ?? []) + "\n");
            foreach (var file in new[] { "llamadas.log", "issues.tsv", "proyectos.tsv", "campos.tsv", "items.txt" })
                File.WriteAllText(Path.Combine(_state, file), "");
            _environment = new()
            {
                ["FAKE_GH"] = _state,
                ["FAKE_OWNER_TYPE"] = owner,
                ["FAKE_PROJECT_SCOPE"] = projectScope ? "1" : "",
                ["CONCILIA_PAUSA"] = "0",
                ["PATH"] = _bin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"),
            };
        }

        // Runs importar.sh against "autor/repo"; its output and errors, together.
        public string Run(byte[] zip, params string[] options)
        {
            var folder = Path.Combine(_temp.Path, $"run{++_runs}");
            ZipFile.ExtractToDirectory(new MemoryStream(zip), folder);
            var start = new ProcessStartInfo(GitBash.Path!)
            {
                WorkingDirectory = folder, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8, StandardErrorEncoding = System.Text.Encoding.UTF8,
            };
            foreach (var argument in new[] { "importar.sh", "autor/repo" }.Concat(options))
                start.ArgumentList.Add(argument);
            foreach (var (name, value) in _environment)
                start.Environment[name] = value;
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            Assert.True(process.WaitForExit(60_000), "importar.sh did not end in 60 s.");
            var text = output.Result + errors.Result;
            Assert.True(process.ExitCode == 0, $"importar.sh ended with {process.ExitCode}:\n{text}");
            return text.ReplaceLineEndings("\n");
        }

        // The calls to gh that start with these words ("issue create").
        public List<string> Calls(string command) =>
            File.ReadAllLines(Path.Combine(_state, "llamadas.log")).Where(l => l.StartsWith("gh " + command, StringComparison.Ordinal)).ToList();

        public void ClearCalls() => File.WriteAllText(Path.Combine(_state, "llamadas.log"), "");

        public List<string> Comments() =>
            Directory.Exists(Path.Combine(_state, "comentarios"))
                ? Directory.GetFiles(Path.Combine(_state, "comentarios")).SelectMany(f => File.ReadAllText(f).Split("\u0001", StringSplitOptions.RemoveEmptyEntries)).ToList()
                : [];

        public void Dispose() => _temp.Dispose();
    }

    // The fake gh, in bash: only what importar.sh calls. Each call goes to
    // llamadas.log, one line, with its arguments separated by spaces.
    private const string FakeGh = """
        #!/usr/bin/env bash
        set -u
        S="$FAKE_GH"
        echo "gh $*" >> "$S/llamadas.log"
        opcion() { # opcion --title args...
          local nombre=$1
          shift
          while (( $# > 1 )); do
            if [[ "$1" == "$nombre" ]]; then
              printf '%s' "$2"
              return
            fi
            shift
          done
        }
        lineas() { wc -l < "$1" | tr -d ' '; }
        case "$1 ${2:-}" in
          "--version ")
            echo "gh version 2.103.3 (2026-10-26)" ;;
          "auth status")
            echo "  - Token scopes: 'gist', 'read:org', 'repo'${FAKE_PROJECT_SCOPE:+, 'project'}" ;;
          "api users/"*)
            echo "$FAKE_OWNER_TYPE" ;;
          "api orgs/"*)
            grep -v '^$' "$S/tipos.txt" || true ;;
          "label create")
            ;;
          "issue create")
            if [[ " $* " == *" --help "* ]]; then
              echo "  --type name   Set the issue type by name"
              exit 0
            fi
            marca=$(grep -o '<!-- concilia:[^ ]* generacion=[^ ]* hash=[^ ]* -->' "$(opcion --body-file "$@")")
            id=$(sed 's/.*concilia:\([^ ]*\) .*/\1/' <<< "$marca")
            generacion=$(sed 's/.*generacion=\([^ ]*\) .*/\1/' <<< "$marca")
            hash=$(sed 's/.*hash=\([^ ]*\) .*/\1/' <<< "$marca")
            url="https://github.com/autor/repo/issues/$(( $(lineas "$S/issues.tsv") + 1 ))"
            printf '%s\t%s\t%s\t%s\tOPEN\n' "$id" "$generacion" "$hash" "$url" >> "$S/issues.tsv"
            echo "$url" ;;
          "issue list")
            cat "$S/issues.tsv" ;;
          "issue view")
            fichero="$S/comentarios/$(tr '/:' '__' <<< "$3")"
            if [[ -f "$fichero" ]]; then tr '\001' '\n' < "$fichero"; fi ;;
          "issue comment")
            mkdir -p "$S/comentarios"
            { cat "$(opcion --body-file "$@")"; printf '\001'; } >> "$S/comentarios/$(tr '/:' '__' <<< "$3")" ;;
          "project list")
            cat "$S/proyectos.tsv" ;;
          "project create")
            numero=$(( $(lineas "$S/proyectos.tsv") + 1 ))
            printf '%s\t%s\n' "$numero" "$(opcion --title "$@")" >> "$S/proyectos.tsv"
            echo "$numero" ;;
          "project link")
            ;;
          "project view")
            echo "PVT_1" ;;
          "project field-list")
            if [[ "$*" == *'select(.name == "Status")'* ]]; then
              printf 'OPT_TODO\tTodo\nOPT_PROGRESS\tIn Progress\nOPT_DONE\tDone\n'
            else
              printf 'F_TITLE\tTitle\nF_STATUS\tStatus\n'
              cat "$S/campos.tsv"
            fi ;;
          "project field-create")
            campo="F_$(( $(lineas "$S/campos.tsv") + 1 ))"
            printf '%s\t%s\n' "$campo" "$(opcion --name "$@")" >> "$S/campos.tsv"
            echo "$campo" ;;
          "project item-list")
            cat "$S/items.txt" ;;
          "project item-add")
            opcion --url "$@" >> "$S/items.txt"
            echo >> "$S/items.txt"
            echo "ITEM_$(lineas "$S/items.txt")" ;;
          "project item-edit")
            ;;
          *)
            echo "gh simulado: no sabe hacer «$*»" >&2
            exit 1 ;;
        esac
        """;
}

// Git Bash on Windows (never the bash of WSL, in System32) or the bash of the
// system elsewhere; null if there is none.
public static class GitBash
{
    public static readonly string? Path = Find();

    private static string? Find()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return File.Exists("/bin/bash") ? "/bin/bash" : null;
        var git = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(folder => System.IO.Path.Combine(folder.Trim(), "git.exe")).FirstOrDefault(File.Exists);
        var roots = new List<string>();
        if (git is not null)
        {
            var folder = System.IO.Path.GetDirectoryName(git)!; // …\Git\cmd, …\Git\bin or …\Git\mingw64\bin
            roots.Add(System.IO.Path.GetFullPath(System.IO.Path.Combine(folder, "..")));
            roots.Add(System.IO.Path.GetFullPath(System.IO.Path.Combine(folder, "..", "..")));
        }
        roots.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git"));
        return roots.SelectMany(root => new[] { System.IO.Path.Combine(root, "usr", "bin", "bash.exe"), System.IO.Path.Combine(root, "bin", "bash.exe") })
            .FirstOrDefault(File.Exists);
    }
}

// A [Fact] that is skipped, with the reason, when there is no bash.
public sealed class BashFactAttribute : FactAttribute
{
    public BashFactAttribute()
    {
        if (GitBash.Path is null)
            Skip = "No hay bash (en Windows, Git Bash).";
    }
}
