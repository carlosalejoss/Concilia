namespace Concilia.Core.Deliverables.Export;

// Step 140 (decisions of the author of 13-11-2026): what the client receives
// goes in the language of the project ("es" or "en"), not only what the
// models write: Word and PDF, Gherkin, the CSV, the issues of GitHub and the
// narrative of the stories. What only the analyst reads (the web, and the
// README and the messages of the GitHub script) stays in Spanish, like the
// rest of the platform.
//
// Each text is written with its two versions side by side, where it is used:
// Text("Requisitos funcionales", "Functional requirements").
public sealed record ExportLanguage(string Code)
{
    public static readonly ExportLanguage Spanish = new("es");

    public bool English => Code == "en";

    public string Text(string spanish, string english) => English ? english : spanish;

    // The dates: 20-10-2026 in Spanish, 2026-10-20 in English (a reader of
    // English might take 10-11 for the 11th of October).
    public string Date(DateTimeOffset date) => date.ToString(English ? "yyyy-MM-dd" : "dd-MM-yyyy");

    // The narrative of a story. The three fields are kept without the words
    // around them, so each language puts its own:
    //   es  «Como empleado, quiero reservar una sala, para tenerla asegurada.»
    //   en  «As an employee, I want to book a room, so that I can have it secured.»
    // In English the model writes the role with its article ("an authorized
    // user") and "quiero" and "para" starting with a verb, so the template is
    // the one of step 104 (the input of AQUSA): "to" and "I can" go around
    // them, and a "to" the model already wrote is not doubled.
    public string Narrative(string role, string want, string soThat)
    {
        if (!English)
            return $"Como {role}, quiero {want}, para {soThat}.";
        var means = want.Trim();
        if (means.StartsWith("to ", StringComparison.OrdinalIgnoreCase))
            means = means[3..];
        return $"As {role.Trim()}, I want to {means}, so that I can {soThat.Trim().TrimEnd('.')}.";
    }

    // The labels of Jira, Azure DevOps, GitHub and the tags of Gherkin:
    // without accents or spaces, as they need.
    public string PendingDiscrepancyLabel => Text("discrepancia-sin-revisar", "unreviewed-discrepancy");

    public IEnumerable<string> IterationLabels(IReadOnlyList<int> iterations) =>
        iterations.Select(n => Text($"iteracion-{n}", $"iteration-{n}"));
}
