using Concilia.Core.Projects;

namespace Concilia.Tests.Projects;

// Step 109: the rules of the iterations of a project (IterationRules).
public class IterationRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 10, 0, 0, TimeSpan.Zero);

    private static Iteration Closed(int number) => new() { Number = number, Name = IterationRules.DefaultName(number), Status = IterationStatus.Closed, ClosedAt = Now };

    private static Iteration Open(int number) => new() { Number = number, Name = IterationRules.DefaultName(number), Status = IterationStatus.Open };

    [Fact]
    public void A_new_project_starts_with_the_first_iteration_open()
    {
        var first = IterationRules.First(7, Now);

        Assert.Equal((7, 1, "Iteración 1", IterationStatus.Open, Now), (first.ProjectId, first.Number, first.Name, first.Status, first.CreatedAt));
        Assert.Null(first.ClosedAt);
    }

    [Fact]
    public void A_new_iteration_follows_the_last_one_only_when_none_is_open()
    {
        var (created, error) = IterationRules.Create(7, [Closed(1), Closed(2)], "Reunión del 15-11", "Notas", Now);

        Assert.Null(error);
        Assert.Equal((3, "Reunión del 15-11", "Notas", IterationStatus.Open), (created!.Number, created.Name, created.Notes, created.Status));

        (created, error) = IterationRules.Create(7, [Closed(1), Open(2)], "Otra", null, Now);
        Assert.Null(created);
        Assert.Equal("La iteración 2 sigue abierta: cierra su documentación antes de crear otra.", error);
    }

    [Fact]
    public void Closing_needs_a_document_and_an_open_iteration()
    {
        var iteration = Open(1);

        Assert.Equal("Sube al menos un documento antes de cerrar la documentación.", IterationRules.Close(iteration, 0, Now));
        Assert.Equal(IterationStatus.Open, iteration.Status);
        Assert.Null(IterationRules.Close(iteration, 2, Now));
        Assert.Equal((IterationStatus.Closed, Now), (iteration.Status, iteration.ClosedAt));
        Assert.Equal("La documentación de esta iteración ya está cerrada.", IterationRules.Close(iteration, 2, Now));
    }

    [Fact]
    public void Only_the_last_iteration_can_be_reopened()
    {
        var first = Closed(1);
        var second = Closed(2);

        Assert.Equal("Solo se puede reabrir la última iteración.", IterationRules.Reopen(first, [first, second]));
        Assert.Null(IterationRules.Reopen(second, [first, second]));
        Assert.Equal(IterationStatus.Open, second.Status);
        Assert.Null(second.ClosedAt);
        Assert.Equal("Esta iteración ya está abierta.", IterationRules.Reopen(second, [first, second]));
    }

    [Fact]
    public void Only_the_last_open_and_empty_iteration_can_be_deleted_and_never_the_first()
    {
        Assert.Equal("La iteración 1 no se puede eliminar.", IterationRules.Delete(Open(1), [Open(1)], 0));
        Assert.Equal("Solo se puede eliminar la última iteración, mientras está abierta.", IterationRules.Delete(Closed(2), [Closed(1), Closed(2)], 0));
        Assert.Equal("Solo se puede eliminar la última iteración, mientras está abierta.", IterationRules.Delete(Closed(2), [Closed(1), Closed(2), Open(3)], 0));
        Assert.Equal("La iteración tiene documentos: no se puede eliminar.", IterationRules.Delete(Open(2), [Closed(1), Open(2)], 1));
        Assert.Null(IterationRules.Delete(Open(2), [Closed(1), Open(2)], 0));
    }

    [Fact]
    public void The_name_is_required_and_name_and_notes_have_a_limit()
    {
        Assert.Empty(IterationRules.Validate("Iteración 2", null));
        Assert.Equal(["El nombre es obligatorio."], IterationRules.Validate("", null)["name"]);
        Assert.Contains("name", IterationRules.Validate(new string('x', IterationRules.MaxNameLength + 1), null).Keys);
        Assert.Contains("notes", IterationRules.Validate("Iteración 2", new string('x', IterationRules.MaxNotesLength + 1)).Keys);
    }

    [Fact]
    public void The_style_reference_belongs_to_the_project_and_any_other_file_to_an_iteration()
    {
        Assert.False(IterationRules.BelongsToIteration("estilo.ZIP"));
        Assert.True(IterationRules.BelongsToIteration("acta.docx"));
        Assert.True(IterationRules.BelongsToIteration("antiguo.odt")); // a file with no text from before the extraction
    }

    // A project from before the iterations: closed with its documents, as it
    // was, or open if it has none (as a new project).
    [Fact]
    public void A_project_from_before_gets_its_first_iteration_closed_or_open_if_it_has_no_documents()
    {
        var project = new Project { Id = 3, Name = "Clínicas", SystemName = "el sistema Clínicas", CreatedAt = Now.AddDays(-10) };
        ProjectFile Uploaded(int days) => new() { FileName = "a.md", StoredName = "a.md", ContentType = "text/markdown", Sha256 = "", UploadedAt = Now.AddDays(-days) };

        var closed = IterationRules.ForExisting(project, [Uploaded(9), Uploaded(4)]);
        var open = IterationRules.ForExisting(project, []);

        Assert.Equal((3, 1, "Iteración 1", IterationStatus.Closed, project.CreatedAt, Now.AddDays(-4)),
            (closed.ProjectId, closed.Number, closed.Name, closed.Status, closed.CreatedAt, closed.ClosedAt));
        Assert.Equal((IterationStatus.Open, (DateTimeOffset?)null), (open.Status, open.ClosedAt));
    }
}
