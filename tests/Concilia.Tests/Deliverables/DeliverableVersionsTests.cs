using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Concilia.Api.Deliverables;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Core.Deliverables;

namespace Concilia.Tests.Deliverables;

// Step 101: at start-up, the deliverables from before the history get their
// first version, once, with the page of the mockup copied.
public class DeliverableVersionsTests
{
    [Fact]
    public async Task The_deliverables_from_before_the_history_get_a_first_version_once()
    {
        using var temp = new TempDirectory();
        var storage = new FileStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = temp.Path }).Build(),
            new HostingEnvironment { ContentRootPath = temp.Path });
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var created = new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
        var edited = created.AddHours(2);
        var catalog = new Deliverable { ProjectId = 1, Kind = DeliverableKind.Catalog, JobId = Guid.NewGuid(), Status = DeliverableStatus.Validated, Content = """{"requisitos":[{"id":"RF-001"}]}""", CreatedAt = created, UpdatedAt = edited };
        var mockup = new Deliverable { ProjectId = 1, Kind = DeliverableKind.Mockup, JobId = Guid.NewGuid(), Content = """{"historias":3,"pantallas":4,"bytes":10,"avisos":[]}""", CreatedAt = created };
        db.Deliverables.AddRange(catalog, mockup);
        await db.SaveChangesAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(storage.MockupFile(1))!);
        File.WriteAllText(storage.MockupFile(1), "<!doctype html><p>mockup</p>");

        await DeliverableVersions.RecordExistingAsync(db, storage);
        await DeliverableVersions.RecordExistingAsync(db, storage); // the next start-up adds nothing

        var versions = await db.DeliverableVersions.OrderBy(v => v.Kind).ToListAsync();
        Assert.Equal(2, versions.Count);
        var first = versions.Single(v => v.Kind == DeliverableKind.Catalog);
        Assert.Equal((1, VersionOrigin.Existing, DeliverableStatus.Validated, edited, 1), (first.Number, first.Origin, first.Status, first.CreatedAt, first.Elements));
        Assert.Equal(created, versions.Single(v => v.Kind == DeliverableKind.Mockup).CreatedAt);
        Assert.Equal("<!doctype html><p>mockup</p>", File.ReadAllText(storage.MockupVersionFile(1, 1)));
    }

    // Step 110: the ids given in the history of a draft are those of its
    // versions with the same job; a new generation numbers from 1 again.
    [Fact]
    public async Task The_ids_of_the_history_are_those_of_the_versions_of_the_same_job()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var job = Guid.NewGuid();
        var older = Guid.NewGuid();
        DeliverableVersion Version(int number, Guid of, string content) =>
            new() { ProjectId = 1, Kind = DeliverableKind.Catalog, Number = number, JobId = of, Origin = VersionOrigin.Edited, Content = content };
        db.DeliverableVersions.AddRange(
            Version(1, older, """{"requisitos":[{"id":"RF-040","tipo":"RF"}]}"""),
            Version(2, job, """{"requisitos":[{"id":"RF-001","tipo":"RF"},{"id":"RF-002","tipo":"RF"}]}"""),
            Version(3, job, """{"requisitos":[{"id":"RF-001","tipo":"RF"}]}"""));
        await db.SaveChangesAsync();
        var catalog = new Deliverable { ProjectId = 1, Kind = DeliverableKind.Catalog, JobId = job, Content = """{"requisitos":[{"id":"RF-001","tipo":"RF"}]}""" };

        var used = await DeliverableVersions.IdsOfJobAsync(db, catalog);

        Assert.Equal(["RF-001", "RF-002"], used.Order());
        var saved = CatalogDraft.Normalize("""{"requisitos":[{"id":"RF-001","tipo":"RF"},{"id":"nuevo","tipo":"RF"}]}""", catalog.Content, used);
        Assert.Equal(["RF-001", "RF-003"], CatalogDraft.Ids(saved)); // RF-002 was given before
    }
}
