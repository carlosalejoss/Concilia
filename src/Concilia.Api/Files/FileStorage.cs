namespace Concilia.Api.Files;

// Knows where uploaded files live on disk: {RootPath}/projects/{projectId}/.
// RootPath comes from the "Storage:RootPath" setting.
public class FileStorage(IConfiguration configuration, IHostEnvironment environment)
{
    // A relative path is resolved from the API's content root, so "../../data"
    // in development points to the data/ folder at the root of the repository.
    public string RootPath { get; } = Path.GetFullPath(
        configuration["Storage:RootPath"] ?? "data",
        environment.ContentRootPath);

    public string ProjectFolder(int projectId) =>
        Path.Combine(RootPath, "projects", projectId.ToString());

    // The current mockup of a project: {RootPath}/projects/{projectId}/mockup/mockup.html.
    public string MockupFile(int projectId) =>
        Path.Combine(ProjectFolder(projectId), "mockup", "mockup.html");

    // Folder of a job (A/B over the same input): {RootPath}/jobs/{jobId}/, with its plan.
    public string JobFolder(Guid jobId) =>
        Path.Combine(RootPath, "jobs", jobId.ToString());

    // Work folder of a model run: {RootPath}/runs/{runId}/ (see RunFolders).
    public string RunFolder(Guid runId) =>
        Path.Combine(RootPath, "runs", runId.ToString());

    // Deletes everything a project left on disk: its folder (files, extracted
    // texts and mockup) and the folders of its jobs and runs. A folder that
    // is not there is skipped.
    public void DeleteProjectData(int projectId, IEnumerable<Guid> jobIds, IEnumerable<Guid> runIds)
    {
        var folders = new[] { ProjectFolder(projectId) }
            .Concat(jobIds.Select(JobFolder))
            .Concat(runIds.Select(RunFolder));
        foreach (var folder in folders.Where(Directory.Exists))
            Directory.Delete(folder, recursive: true);
    }
}
