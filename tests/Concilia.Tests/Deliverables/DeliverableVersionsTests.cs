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
}
