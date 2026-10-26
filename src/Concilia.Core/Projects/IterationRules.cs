using Concilia.Core.Mockup;

namespace Concilia.Core.Projects;

// The rules of the iterations of a project (milestone 22, step 109): only one
// open at a time, documents only go to the open one, only the last one can be
// reopened, and an iteration created by mistake (the last, open and empty)
// can be deleted. Each check returns an error for the user, or null if the
// action is allowed.
public static class IterationRules
{
    public const int MaxNameLength = 100;
    public const int MaxNotesLength = 2_000;

    public static string DefaultName(int number) => $"Iteración {number}";

    // The style reference of the mockup (.zip) belongs to the project, not to
    // an iteration; any other file is documentation of an iteration.
    public static bool BelongsToIteration(string fileName) => !StyleReference.IsStyleFile(fileName);

    // Errors by field (empty if valid), with the values already trimmed.
    public static Dictionary<string, string[]> Validate(string? name, string? notes)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(name))
            errors["name"] = ["El nombre es obligatorio."];
        else if (name.Length > MaxNameLength)
            errors["name"] = [$"El nombre no puede superar {MaxNameLength} caracteres."];
        if (notes is { Length: > MaxNotesLength })
            errors["notes"] = [$"Las notas no pueden superar {MaxNotesLength:N0} caracteres."];
        return errors;
    }

    // The first iteration of a new project: open, for its first documents.
    public static Iteration First(int projectId, DateTimeOffset now) => new()
    {
        ProjectId = projectId,
        Number = 1,
        Name = DefaultName(1),
        Status = IterationStatus.Open,
        CreatedAt = now,
    };

    // The first iteration of a project from before the iterations: closed with
    // all its documents, so it behaves as before, with the date of its last
    // upload. A project without documents gets it open, as a new one: if not,
    // its first document would have to go to an "Iteración 2".
    public static Iteration ForExisting(Project project, IReadOnlyList<ProjectFile> documents)
    {
        var iteration = First(project.Id, project.CreatedAt);
        if (documents.Count > 0)
        {
            iteration.Status = IterationStatus.Closed;
            iteration.ClosedAt = documents.Max(f => f.UploadedAt);
        }
        return iteration;
    }

    // The number of the next iteration of a project.
    public static int NextNumber(IReadOnlyList<Iteration> existing) =>
        existing.Count == 0 ? 1 : existing.Max(i => i.Number) + 1;

    // A new iteration, after the last one, only when none is open.
    public static (Iteration? Iteration, string? Error) Create(
        int projectId, IReadOnlyList<Iteration> existing, string name, string? notes, DateTimeOffset now)
    {
        if (existing.FirstOrDefault(i => i.Status == IterationStatus.Open) is { } open)
            return (null, $"La iteración {open.Number} sigue abierta: cierra su documentación antes de crear otra.");
        return (new Iteration
        {
            ProjectId = projectId,
            Number = NextNumber(existing),
            Name = name,
            Notes = notes,
            Status = IterationStatus.Open,
            CreatedAt = now,
        }, null);
    }

    // "Cerrar documentación": only with at least one document, so that no
    // empty iteration is left behind.
    public static string? Close(Iteration iteration, int documents, DateTimeOffset now)
    {
        if (iteration.Status == IterationStatus.Closed)
            return "La documentación de esta iteración ya está cerrada.";
        if (documents == 0)
            return "Sube al menos un documento antes de cerrar la documentación.";
        iteration.Status = IterationStatus.Closed;
        iteration.ClosedAt = now;
        return null;
    }

    // Only the last iteration can be reopened (decision of the author, 26-10-2026):
    // the documents of the earlier ones stay as they were.
    public static string? Reopen(Iteration iteration, IReadOnlyList<Iteration> all)
    {
        if (iteration.Status == IterationStatus.Open)
            return "Esta iteración ya está abierta.";
        if (iteration.Number != all.Max(i => i.Number))
            return "Solo se puede reabrir la última iteración.";
        iteration.Status = IterationStatus.Open;
        iteration.ClosedAt = null;
        return null;
    }

    // An iteration created by mistake: the last one, open and without
    // documents. The first one is never deleted (documents need somewhere to go).
    public static string? Delete(Iteration iteration, IReadOnlyList<Iteration> all, int documents)
    {
        if (iteration.Number == 1)
            return "La iteración 1 no se puede eliminar.";
        if (iteration.Number != all.Max(i => i.Number) || iteration.Status != IterationStatus.Open)
            return "Solo se puede eliminar la última iteración, mientras está abierta.";
        if (documents > 0)
            return "La iteración tiene documentos: no se puede eliminar.";
        return null;
    }
}
