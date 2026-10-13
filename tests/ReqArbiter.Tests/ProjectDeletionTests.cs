using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using ReqArbiter.Api.Files;
using ReqArbiter.Core.Projects;

namespace ReqArbiter.Tests;

// Deleting a project (13-10-2026): it asks for the name of the project typed
// by the user, and removes the folders the project left on disk.
public class ProjectDeletionTests
{
    [Theory]
    [InlineData("Calendario", true)]
    [InlineData("  Calendario ", true)]
    [InlineData("calendario", false)]
    [InlineData("Calendari", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Deleting_asks_for_the_exact_name_of_the_project(string? typed, bool confirms)
    {
        Assert.Equal(confirms, ProjectSettings.ConfirmsDeletion("Calendario", typed));
    }

    [Fact]
    public void The_folders_of_the_project_its_jobs_and_its_runs_are_deleted()
    {
        using var temp = new TempDirectory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = temp.Path })
            .Build();
        var storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = temp.Path });
        Guid job = Guid.NewGuid(), run = Guid.NewGuid(), missingRun = Guid.NewGuid(), otherRun = Guid.NewGuid();
        foreach (var folder in new[] { Path.Combine(storage.ProjectFolder(7), "mockup"), storage.JobFolder(job), storage.RunFolder(run), storage.RunFolder(otherRun), storage.ProjectFolder(8) })
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "x.txt"), "x");
        }

        storage.DeleteProjectData(7, [job], [run, missingRun]);

        Assert.False(Directory.Exists(storage.ProjectFolder(7)));
        Assert.False(Directory.Exists(storage.JobFolder(job)));
        Assert.False(Directory.Exists(storage.RunFolder(run)));
        // What belongs to other projects stays.
        Assert.True(Directory.Exists(storage.RunFolder(otherRun)));
        Assert.True(Directory.Exists(storage.ProjectFolder(8)));
    }
}
