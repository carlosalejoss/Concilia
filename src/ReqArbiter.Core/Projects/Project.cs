namespace ReqArbiter.Core.Projects;

// A workspace that groups the uploaded files, the model runs and their results.
public class Project
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
