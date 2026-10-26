using System.Runtime.CompilerServices;
using Concilia.Core.Catalog;
using Concilia.Core.Deliverables;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;

namespace Concilia.Tests.Phases;

// Milestone 22, step 112 (requirement of the author): the modes of always
// ("Todas las iteraciones" and "Desde cero") must give exactly the same prompt
// and the same files of input/ as the prompts v0.12. For fixed inputs, each
// phase and role is compared with what v0.12 gave, kept in
// tests/fixtures/v0.12-snapshot/ (recorded on 26-10-2026, before any change of
// the milestone). If one of these tests fails, a mode of always has changed:
// the fix is in the code, never in the snapshot. Only for a new version of the
// prompts of always would they be recorded again, with
// CONCILIA_UPDATE_SNAPSHOTS=1.
public class V012SnapshotTests
{
    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    private static readonly IReadOnlyList<Fragment> Fragments =
        Fragmenter.FragmentDocument("DOC-01", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt")));

    private static PhaseInput Input() => new()
    {
        SystemName = "el sistema ReservaSalas",
        Language = "es",
        Glossary = "",
        Documentation = DocumentationFile.Build(Fragments, new Dictionary<string, string> { ["DOC-01"] = "DOC-01.txt" }),
    };

    private static readonly PhaseQuestion[] Questions =
    [
        new("P-001", "¿Quién puede anular una reserva?", "respondida", "Solo quien la hizo y el administrador."),
        new("P-002", "¿Hay límite de duración por reserva?", "pendiente", null),
    ];

    public static TheoryData<string> Cases => ["preguntas-propuesta", "preguntas-arbitro", "catalogo-propuesta", "catalogo-arbitro", "backlog-propuesta", "backlog-arbitro", "sugerencias"];

    [Theory]
    [MemberData(nameof(Cases))]
    public void The_modes_of_always_give_the_same_prompt_and_inputs_as_v012(string name)
    {
        var (phase, input, pending) = Case(name);
        using var work = new TempDirectory();

        var request = PhaseTask.BuildRequest(phase, input, Fragments, Guid.Empty, "modelo", work.Path, new PhaseOptions(), pending);

        Assert.Null(request.Error);
        var actual = new Dictionary<string, string> { ["prompt.txt"] = request.Request!.Prompt };
        foreach (var file in Directory.GetFiles(Path.Combine(work.Path, "input")).Order(StringComparer.Ordinal))
            actual[$"input/{Path.GetFileName(file)}"] = File.ReadAllText(file);
        CompareWithSnapshot(name, actual);
    }

    private static (PhaseDefinition Phase, PhaseInput Input, IReadOnlyList<string>? Pending) Case(string name) => name switch
    {
        "preguntas-propuesta" => (PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Proposal),
            Input() with { PreviousQuestions = PhasePrompt.PreviousQuestionsBlock(Questions) }, null),
        "preguntas-arbitro" => (PhaseDefinition.For(PhaseKind.Questions, PhaseRole.Arbiter),
            Input() with { PreviousQuestions = PhasePrompt.PreviousQuestionsBlock(Questions), Proposal1 = Example("preguntas_propuesta_1.json"), Proposal2 = Example("preguntas_propuesta_2.json"), Matching = Matching(PhaseKind.Questions, "preguntas") }, null),
        "catalogo-propuesta" => (PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Proposal),
            Input() with { Answers = PhasePrompt.AnswersBlock(Questions) }, null),
        "catalogo-arbitro" => (PhaseDefinition.For(PhaseKind.Catalog, PhaseRole.Arbiter),
            Input() with { Answers = PhasePrompt.AnswersBlock(Questions), Proposal1 = Example("catalogo_propuesta_1.json"), Proposal2 = Example("catalogo_propuesta_2.json"), Matching = Matching(PhaseKind.Catalog, "catalogo") }, null),
        "backlog-propuesta" => (PhaseDefinition.For(PhaseKind.Backlog, PhaseRole.Proposal),
            Input() with { Answers = PhasePrompt.AnswersBlock(Questions), Catalog = CatalogDraft.ForNextPhase(Example("catalogo_arbitro.json")) }, null),
        "backlog-arbitro" => (PhaseDefinition.For(PhaseKind.Backlog, PhaseRole.Arbiter),
            Input() with { Answers = PhasePrompt.AnswersBlock(Questions), Catalog = CatalogDraft.ForNextPhase(Example("catalogo_arbitro.json")), Proposal1 = Example("backlog_propuesta_1.json"), Proposal2 = Example("backlog_propuesta_2.json"), Matching = Matching(PhaseKind.Backlog, "backlog") }, null),
        "sugerencias" => (PhaseDefinition.For(PhaseKind.Suggestions, PhaseRole.Proposal),
            Input() with { Answers = PhasePrompt.AnswersBlock(Questions), PendingQuestions = PhasePrompt.PendingQuestionsBlock([("P-002", "¿Hay límite de duración por reserva?", "RF de reservas")]) }, ["P-002"]),
        _ => throw new ArgumentException(name),
    };

    // The input that a job launched as always saved in its plan (the full flow
    // with Fake, FullFlowTests): the documentation, the questions, the answers
    // and the catalog, as the API built them, the same as with v0.12.
    internal static void ComparePlanInput(string name, string jobFolder)
    {
        var input = PhasePlan.Load(jobFolder).Input;
        CompareWithSnapshot(name, new Dictionary<string, string>
        {
            ["documentacion.txt"] = input.Documentation,
            ["preguntas-anteriores.txt"] = input.PreviousQuestions,
            ["respuestas.txt"] = input.Answers,
            ["catalogo.json"] = input.Catalog,
            ["resto.txt"] = string.Join("\n", input.SystemName, input.Language, input.Glossary, input.Backlog, input.PendingQuestions),
        });
    }

    // The matching the arbiter gets (input/emparejamiento.txt), with the Fake
    // embedder, which always gives the same numbers.
    private static string Matching(PhaseKind kind, string prefix) =>
        MatchingFile.Write(new ArbiterMatching(new FakeTextEmbedder(), new MatchThresholds(Same: 0.9, Similar: 0.7))
            .Compute(kind, Example($"{prefix}_propuesta_1.json"), Example($"{prefix}_propuesta_2.json")), ["A", "B"]);

    // Each file the same as in the snapshot (line endings aside: git may turn
    // them into CRLF on Windows), and no file more or less.
    private static void CompareWithSnapshot(string name, Dictionary<string, string> actual)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "fixtures", "v0.12-snapshot", name);
        if (Environment.GetEnvironmentVariable("CONCILIA_UPDATE_SNAPSHOTS") == "1")
        {
            var source = Path.Combine(SourceFixtures(), "v0.12-snapshot", name);
            foreach (var (file, content) in actual)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(source, file))!);
                File.WriteAllText(Path.Combine(source, file), content);
            }
            return;
        }
        var expected = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(folder, f).Replace('\\', '/'), File.ReadAllText);
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), actual.Keys.Order(StringComparer.Ordinal));
        foreach (var (file, content) in actual)
            Assert.True(Normalize(expected[file]) == Normalize(content), $"{name}/{file} differs from the v0.12 snapshot.");
    }

    private static string Normalize(string text) => text.ReplaceLineEndings("\n");

    // tests/fixtures/ in the source tree, from the path of this file.
    private static string SourceFixtures([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "fixtures"));
}
