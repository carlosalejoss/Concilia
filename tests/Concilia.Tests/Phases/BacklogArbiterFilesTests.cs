using System.Text;
using System.Text.Json.Nodes;
using Concilia.Core.Catalog;
using Concilia.Core.Phases;

namespace Concilia.Tests.Phases;

// Step 143 (prompts v0.14): what the arbiter of the backlog reads, smaller
// than the two whole proposals and the whole catalog.
public class BacklogArbiterFilesTests
{
    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    private static readonly PhaseDefinition Arbiter = PhaseDefinition.For(PhaseKind.Backlog, PhaseRole.Arbiter);

    // Proposal 1: one epic with a story of two criteria and one task, and a
    // story without criteria. Proposal 2: one story with one criterion.
    private const string Proposal1 = """
        {"epicas":[{"id":"EP-01","titulo":"Citas","descripcion":"Pedir y anular citas.","historias":[
          {"id":"HU-001","titulo":"Anular una cita","como":"recepcionista","quiero":"anular una cita","para":"liberar el hueco","requisitos":["RF-001","RNF-001"],
           "criterios":[{"id":"CA-001","escenario":"Con aviso","dado":"una cita futura","cuando":"la anula","entonces":"se avisa al cliente en 1 hora"},
                        {"id":"CA-002","escenario":"Cita pasada","dado":"una cita pasada","cuando":"la anula","entonces":"se rechaza"}],
           "tareas":[{"id":"T-001","titulo":"Botón de anular"}]},
          {"id":"HU-002","titulo":"Ver citas","como":"cliente","quiero":"ver mis citas","para":"recordarlas","requisitos":["RF-002"],"criterios":[],"tareas":[]}]}]}
        """;

    private const string Proposal2 = """
        {"epicas":[{"id":"EP-01","titulo":"Agenda","descripcion":"La agenda.","historias":[
          {"id":"HU-001","titulo":"Cancelar cita","como":"recepcionista","quiero":"cancelar una cita","para":"dar el hueco a otro","requisitos":["RF-001","RF-003"],
           "criterios":[{"id":"CA-001","escenario":"Cancelación","dado":"una cita","cuando":"la cancela","entonces":"queda libre"}],
           "tareas":[]}]}]}
        """;

    private const string Catalog = """
        {"requisitos":[
          {"id":"RF-001","texto":"El sistema DEBERÁ permitir anular una cita."},
          {"id":"RF-002","texto":"El sistema DEBERÁ mostrar al cliente sus citas."},
          {"id":"RF-003","texto":"El sistema DEBERÁ liberar el hueco de una cita anulada."},
          {"id":"RNF-001","texto":"El sistema DEBERÁ avisar en menos de 1 hora."}]}
        """;

    private static string Content(IReadOnlyList<InputFile> files, string path) => files.Single(f => f.Path == path).Content;

    [Fact]
    public void Each_story_is_one_line_that_says_where_the_text_of_its_criteria_and_tasks_is()
    {
        var files = BacklogArbiterFiles.Build(Proposal1, Proposal2, Catalog, "");

        Assert.Equal(
            [
                "1:EP-01 «Citas» · Pedir y anular citas.",
                "1:HU-001 «Anular una cita» · como recepcionista · quiero anular una cita · para liberar el hueco · requisitos: RF-001, RNF-001 · " +
                "criterios: 1:CA-001 «Con aviso» (cifras: 1), 1:CA-002 «Cita pasada» · tareas: 1:T-001 · texto en criterios.txt, líneas 1–3",
                "1:HU-002 «Ver citas» · como cliente · quiero ver mis citas · para recordarlas · requisitos: RF-002 · criterios: (ninguno) · tareas: (ninguno)",
            ],
            Content(files, BacklogArbiterFiles.Proposal1File).TrimEnd('\n').Split('\n'));
        // The lines of proposal 2 go on after those of proposal 1, in the same file.
        Assert.EndsWith("criterios: 2:CA-001 «Cancelación» · tareas: (ninguno) · texto en criterios.txt, línea 4",
            Content(files, BacklogArbiterFiles.Proposal2File).TrimEnd('\n').Split('\n')[1]);
        Assert.Equal(
            [
                "1:CA-001 «Con aviso» · Dado una cita futura · Cuando la anula · Entonces se avisa al cliente en 1 hora",
                "1:CA-002 «Cita pasada» · Dado una cita pasada · Cuando la anula · Entonces se rechaza",
                "1:T-001 Botón de anular",
                "2:CA-001 «Cancelación» · Dado una cita · Cuando la cancela · Entonces queda libre",
            ],
            Content(files, BacklogArbiterFiles.CriteriaFile).TrimEnd('\n').Split('\n'));
    }

    [Fact]
    public void The_proposals_are_read_entirely_and_the_criteria_and_the_requirements_only_consulted()
    {
        var files = BacklogArbiterFiles.Build(Proposal1, Proposal2, Catalog, "");

        Assert.Equal(
            [(BacklogArbiterFiles.Proposal1File, true), (BacklogArbiterFiles.Proposal2File, true), (BacklogArbiterFiles.CriteriaFile, false), (BacklogArbiterFiles.RequirementsFile, false)],
            files.Select(f => (f.Path, f.Required)));
    }

    [Fact]
    public void Only_the_requirements_of_the_stories_without_a_pair_mismo_are_to_check()
    {
        // The two HU-001 are "mismo": only RF-002, of 1:HU-002, has to be checked.
        const string matching = "1:HU-001 2:HU-001 mismo 0.93\n1:HU-002 sin-par\n";

        var files = BacklogArbiterFiles.Build(Proposal1, Proposal2, Catalog, matching);

        Assert.Equal("RF-002 El sistema DEBERÁ mostrar al cliente sus citas.\n", Content(files, BacklogArbiterFiles.RequirementsFile));
    }

    [Fact]
    public void A_pair_parecido_or_no_matching_needs_the_requirements_in_the_order_of_the_catalog()
    {
        const string matching = "1:HU-001 2:HU-001 parecido 0.80\n1:HU-002 sin-par\n";

        Assert.Equal(["RF-001", "RF-002", "RF-003", "RNF-001"],
            Ids(Content(BacklogArbiterFiles.Build(Proposal1, Proposal2, Catalog, matching), BacklogArbiterFiles.RequirementsFile)));
        Assert.Equal(["RF-001", "RF-002", "RF-003", "RNF-001"],
            Ids(Content(BacklogArbiterFiles.Build(Proposal1, Proposal2, Catalog, ""), BacklogArbiterFiles.RequirementsFile)));

        static IEnumerable<string> Ids(string file) => file.TrimEnd('\n').Split('\n').Select(l => l.Split(' ')[0]);
    }

    [Fact]
    public void When_every_story_has_a_pair_mismo_the_file_says_so()
    {
        const string onlyOne = """{"epicas":[{"id":"EP-01","titulo":"T","descripcion":"D","historias":[{"id":"HU-001","titulo":"A","como":"c","quiero":"q","para":"p","requisitos":["RF-001"],"criterios":[],"tareas":[]}]}]}""";

        var files = BacklogArbiterFiles.Build(onlyOne, onlyOne, Catalog, "1:HU-001 2:HU-001 mismo 0.99\n");

        Assert.Equal(BacklogArbiterFiles.NoRequirements, Content(files, BacklogArbiterFiles.RequirementsFile));
    }

    // The validator, the citations by id and FakeProvider still find the whole
    // proposals and the catalog in input/, but the prompt does not name them.
    [Fact]
    public void The_whole_proposals_and_the_catalog_stay_for_the_platform_outside_the_reading_plan()
    {
        using var temp = new TempDirectory();
        var fragments = Fragmenter.FragmentDocument("DOC-01", "Una sala se reserva por horas.");
        var input = new PhaseInput
        {
            SystemName = "el sistema ReservaSalas",
            Language = "es",
            Catalog = Example("catalogo_arbitro.json"),
            Proposal1 = Example("backlog_propuesta_1.json"),
            Proposal2 = Example("backlog_propuesta_2.json"),
        };

        var request = PhaseTask.BuildRequest(Arbiter, input, fragments, Guid.NewGuid(), "m", temp.Path, new PhaseOptions());

        var prompt = request.Request!.Prompt;
        Assert.DoesNotContain("propuesta_1.json", prompt);
        Assert.DoesNotContain("catalogo.json", prompt);
        Assert.Contains("input/propuesta_1.txt", prompt);
        Assert.Equal([BacklogArbiterFiles.Proposal1File, BacklogArbiterFiles.Proposal2File], request.RequiredReads);
        var context = PhaseTask.LoadContext(temp.Path, new HashSet<string>());
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(input.Proposal1), JsonNode.Parse(context.Proposal1!)));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(input.Catalog), JsonNode.Parse(context.Catalog!)));
    }

    // The point of the change: the compact proposals take much less than the
    // whole ones (about 100 tokens per story instead of about 378 with real
    // stories; less with the short stories of the example).
    [Fact]
    public void The_compact_proposals_take_much_less_than_the_whole_ones()
    {
        var whole = ProposalIds.Prefix(PhaseKind.Backlog, Example("backlog_propuesta_1.json"), 1);

        var line = 0;
        var compact = BacklogArbiterFiles.Compact(Example("backlog_propuesta_1.json"), 1, new StringBuilder(), ref line);

        Assert.True(compact.Length < whole.Length * 0.6, $"{compact.Length} de {whole.Length}");
    }
}
