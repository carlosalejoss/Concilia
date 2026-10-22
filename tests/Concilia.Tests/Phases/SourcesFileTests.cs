using Concilia.Core.Phases;

namespace Concilia.Tests.Phases;

// Step 107 (prompts v0.12): the arbiter of the catalog consults only the
// fragments cited by the requirements it has to decide (input/fuentes.txt).
public class SourcesFileTests
{
    private const string Documentation =
        "=== DOC-01 · uno.txt ===\n" +
        "[DOC-01#F001 §1] Las reservas duran como mucho 1 hora.\n" +
        "[DOC-01#F002 §1] El empleado cancela su reserva.\n" +
        "[DOC-01#F003 §2] Texto largo…\n" +
        "[DOC-01#F003 sigue] …que sigue en otra línea.\n" +
        "=== DOC-02 · dos.txt ===\n" +
        "[DOC-02#F001 §1] El administrador da de alta las salas.\n" +
        "[DOC-02#F002 §1] Las salas tienen un aforo.\n";

    private static string Proposal(params (string Id, string Text, string Source)[] requirements) =>
        "{\"requisitos\":[" + string.Join(",", requirements.Select(r =>
            $"{{\"id\":\"{r.Id}\",\"tipo\":\"RF\",\"texto\":\"{r.Text}\",\"fuentes\":[\"{r.Source}\"]}}")) + "]}";

    [Fact]
    public void Only_the_sources_of_what_the_arbiter_has_to_decide_go_in_the_file()
    {
        var proposal1 = Proposal(
            ("RF-001", "El sistema DEBERÁ limitar las reservas a 1 hora.", "DOC-01#F001"),
            ("RF-002", "El sistema DEBERÁ permitir cancelar una reserva.", "DOC-01#F002"),
            ("RF-003", "El sistema DEBERÁ permitir dar de alta salas.", "DOC-02#F001"));
        var proposal2 = Proposal(
            ("RF-001", "El sistema DEBERÁ limitar las reservas a 24 horas.", "DOC-01#F001"),
            ("RF-002", "El sistema DEBERÁ dejar cancelar reservas.", "DOC-01#F002"),
            ("RF-003", "El sistema DEBERÁ guardar el aforo de cada sala.", "DOC-02#F002"));
        var matching =
            "1:RF-001 2:RF-001 mismo 0.95\n" +   // "mismo", but 1 hora and 24 horas: to check
            "1:RF-002 2:RF-002 mismo 0.93\n" +   // "mismo" with the same numbers: settled
            "1:RF-003 2:RF-003 parecido 0.74\n"; // "parecido": to check

        var file = SourcesFile.Build(Documentation, proposal1, proposal2, matching);

        Assert.Equal(
            "=== DOC-01 · uno.txt ===\n" +
            "[DOC-01#F001 §1] Las reservas duran como mucho 1 hora.\n" +
            "=== DOC-02 · dos.txt ===\n" +
            "[DOC-02#F001 §1] El administrador da de alta las salas.\n" +
            "[DOC-02#F002 §1] Las salas tienen un aforo.\n", file);
    }

    [Fact]
    public void A_requirement_without_a_pair_needs_its_source_and_its_continuation_lines_go_with_it()
    {
        var proposal1 = Proposal(("RF-001", "Texto.", "DOC-01#F003"));
        var proposal2 = Proposal(("RF-001", "Otro.", "DOC-02#F002"));

        var file = SourcesFile.Build(Documentation, proposal1, proposal2, "1:RF-001 sin-par\n2:RF-001 sin-par\n");

        Assert.Equal(
            "=== DOC-01 · uno.txt ===\n" +
            "[DOC-01#F003 §2] Texto largo…\n" +
            "[DOC-01#F003 sigue] …que sigue en otra línea.\n" +
            "=== DOC-02 · dos.txt ===\n" +
            "[DOC-02#F002 §1] Las salas tienen un aforo.\n", file);
    }

    [Fact]
    public void Without_a_matching_every_requirement_needs_its_source()
    {
        var proposal1 = Proposal(("RF-001", "Uno.", "DOC-01#F002"));
        var proposal2 = Proposal(("RF-001", "Dos.", "DOC-02#F001"));

        var file = SourcesFile.Build(Documentation, proposal1, proposal2, "");

        Assert.Contains("[DOC-01#F002 §1]", file);
        Assert.Contains("[DOC-02#F001 §1]", file);
        Assert.DoesNotContain("DOC-01#F001", file);
    }

    [Fact]
    public void When_nothing_needs_the_source_the_file_says_so()
    {
        var proposal1 = Proposal(("RF-001", "Cancelar una reserva.", "DOC-01#F002"));
        var proposal2 = Proposal(("RF-001", "Cancelar reservas.", "DOC-01#F002"));

        Assert.Equal(SourcesFile.Empty, SourcesFile.Build(Documentation, proposal1, proposal2, "1:RF-001 2:RF-001 mismo 0.96\n"));
    }

    [Theory]
    [InlineData("1 hora", "24 horas", false)]
    [InlineData("1,5 días", "1.5 días", true)]
    [InlineData("seis años", "seis años", true)]
    [InlineData("10 y 20", "20 y 10", true)]
    public void Numbers_are_compared_as_a_set(string a, string b, bool same) =>
        Assert.Equal(same, SourcesFile.Numbers(a).SetEquals(SourcesFile.Numbers(b)));

    [Fact]
    public void The_line_numbers_of_the_arbiter_are_those_of_its_sources_file()
    {
        var sources = SourcesFile.Filter(Documentation, new HashSet<string> { "DOC-02#F002" });

        Assert.Equal(new Dictionary<int, string> { [2] = "DOC-02#F002" }, DocumentationFile.FragmentAtLine(sources));
    }
}
