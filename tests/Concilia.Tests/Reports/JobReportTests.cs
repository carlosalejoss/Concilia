using System.Text.Json.Nodes;
using Concilia.Core.Phases;
using Concilia.Core.Reports;

namespace Concilia.Tests.Reports;

// Step 64: what the report counts from the arbiter's output and from the
// user's changes to a draft.
public class JobReportTests
{
    [Fact]
    public void The_merge_counts_each_origin_the_discrepancies_and_the_discards()
    {
        const string arbiter = """
            {"requisitos":[
              {"id":"RF-001","de":{"propuesta_1":["RF-001"],"propuesta_2":["RF-002"]},"discrepancia":true},
              {"id":"RF-002","de":{"propuesta_1":["RF-003"],"propuesta_2":[]},"discrepancia":false},
              {"id":"RF-003","de":{"propuesta_1":[],"propuesta_2":["RF-001"]},"discrepancia":false}],
             "descartados":[{"propuesta":1,"id":"RF-004","motivo":"repetido"}]}
            """;
        // The arbiter saw B as "Propuesta 1" and A as "Propuesta 2".
        var plan = new PhasePlan(PhaseKind.Catalog, null!, [], [], ["B", "A"]);

        var merge = JobReports.Merge(PhaseKind.Catalog, arbiter, plan, a: 2, b: 4);

        Assert.Equal(new ElementCounts(2, 4, Merged: 3, Both: 1, OnlyA: 1, OnlyB: 1, Discrepancies: 1, Discarded: 1), merge);
    }

    private static string Catalog(params (string Id, string Text)[] requirements) =>
        new JsonObject
        {
            ["requisitos"] = new JsonArray(requirements.Select(r => (JsonNode)new JsonObject
            {
                ["id"] = r.Id, ["tipo"] = "RF", ["texto"] = r.Text, ["origen"] = "ambas", ["revisada"] = false,
            }).ToArray()),
        }.ToJsonString();

    // The user modifies RF-001, deletes RF-002 (RF-003 becomes RF-002) and adds one.
    [Fact]
    public void The_users_changes_are_counted_by_content_not_by_id()
    {
        var original = Catalog(("RF-001", "reservar"), ("RF-002", "cancelar"), ("RF-003", "consultar"));
        var current = Catalog(("RF-001", "reservar una sala"), ("RF-002", "consultar"), ("RF-003", "valorar"));

        var changes = JobReports.Diff(PhaseKind.Catalog, original, current);

        Assert.Equal(new UserChanges(Original: 3, Current: 3, Kept: 1, Modified: 1, Added: 1, Deleted: 1), changes);
    }

    [Fact]
    public void Reviewing_a_discrepancy_is_not_a_change()
    {
        var original = Catalog(("RF-001", "reservar"));
        var reviewed = original.Replace("\"revisada\":false", "\"revisada\":true");

        Assert.Equal(new UserChanges(1, 1, 1, 0, 0, 0), JobReports.Diff(PhaseKind.Catalog, original, reviewed));
    }
}
