using System.IO.Compression;
using System.Text;
using Concilia.Core.Catalog;
using Concilia.Core.Deliverables;
using Concilia.Core.Mockup;

namespace Concilia.Tests.Mockup;

// Step 78: a mockup of the client, uploaded as a .zip, as the style reference
// of our mockup. The .zip files are built in memory.
public class StyleReferenceTests
{
    private static readonly byte[] Logo = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];

    private static byte[] Zip(params (string Path, byte[] Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in entries)
            {
                using var entry = archive.CreateEntry(path).Open();
                entry.Write(content);
            }
        }
        return stream.ToArray();
    }

    private static byte[] Text(string text) => Encoding.UTF8.GetBytes(text);

    private static byte[] ClientMockup() => Zip(
        ("index.html", Text("<!doctype html><link rel=\"stylesheet\" href=\"css/app.css\"><h1>Portal</h1>")),
        ("css/app.css", Text(":root { --primario: #c0392b; } body { font-family: Lato; }")),
        ("img/logo.png", Logo),
        ("js/app.js", Text("alert('no')")),
        ("__MACOSX/._index.html", Text("x")),
        ("../fuera.css", Text("body {}")));

    [Fact]
    public void Every_zip_is_a_style_reference()
    {
        Assert.True(StyleReference.IsStyleFile("Mockup Cliente.ZIP"));
        Assert.False(StyleReference.IsStyleFile("mockup.html"));
    }

    [Fact]
    public void A_zip_with_pages_or_stylesheets_is_valid()
    {
        Assert.Null(StyleReference.Validate(ClientMockup()));
    }

    [Fact]
    public void A_zip_without_html_or_css_is_rejected()
    {
        var error = StyleReference.Validate(Zip(("img/logo.png", Logo), ("leeme.txt", Text("hola"))));

        Assert.StartsWith("El ZIP no contiene ningún .html ni .css", error);
    }

    [Fact]
    public void A_file_that_is_not_a_zip_is_rejected()
    {
        Assert.StartsWith("No se ha podido leer el ZIP", StyleReference.Validate(Text("esto no es un zip")));
    }

    [Fact]
    public void Only_pages_stylesheets_images_and_fonts_are_unpacked_and_never_outside_the_folder()
    {
        using var temp = new TempDirectory();

        StyleReference.Extract(ClientMockup(), temp.Path, "cliente");

        var files = Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(temp.Path, f).Replace('\\', '/'))
            .Order();
        Assert.Equal(["cliente/css/app.css", "cliente/img/logo.png", "cliente/index.html"], files);
        Assert.Equal(Logo, File.ReadAllBytes(Path.Combine(temp.Path, "cliente", "img", "logo.png")));
    }

    [Fact]
    public void A_minified_stylesheet_gets_a_line_per_rule()
    {
        using var temp = new TempDirectory();
        var minified = string.Concat(Enumerable.Range(1, 100).Select(i => $".c{i}{{color:#{i:000000}}}"));

        StyleReference.Extract(Zip(("app.min.css", Text(minified))), temp.Path, "x");

        var lines = File.ReadAllLines(Path.Combine(temp.Path, "x", "app.min.css"));
        Assert.Equal(".c1{color:#000001}", lines[0]);
        Assert.All(lines, line => Assert.True(line.Length < 100));
    }

    [Theory]
    [InlineData("Mockup cliente v2.zip", "Mockup-cliente-v2")]
    [InlineData("diseño_final.zip", "diseño_final")]
    [InlineData("...zip", "estilo")]
    public void The_folder_of_a_zip_is_named_after_it(string fileName, string folder)
    {
        Assert.Equal(folder, StyleReference.FolderName(fileName));
    }

    [Fact]
    public void Without_a_reference_the_prompt_asks_for_a_plain_design()
    {
        using var temp = new TempDirectory();
        var plan = MockupPlan.From("s", "es", BacklogDraft.ForNextPhase(MockupTaskTests.ExampleBacklog()));

        var request = MockupTask.BuildRequest(plan, Guid.NewGuid(), "m", temp.Path, new MockupRunOptions());

        Assert.Null(StyleReference.Describe(temp.Path));
        Assert.Contains(MockupTask.NoStyle, request.Prompt);
    }

    [Fact]
    public void The_prompt_lists_the_files_of_the_reference_and_the_images_it_may_use()
    {
        using var temp = new TempDirectory();
        var job = Path.Combine(temp.Path, "estilo");
        StyleReference.Extract(ClientMockup(), job, "cliente");
        var work = Path.Combine(temp.Path, "run");
        var plan = MockupPlan.From("s", "es", BacklogDraft.ForNextPhase(MockupTaskTests.ExampleBacklog()));

        var request = MockupTask.BuildRequest(plan, Guid.NewGuid(), "m", work, new MockupRunOptions(), job);

        Assert.True(File.Exists(Path.Combine(work, "input", "estilo", "cliente", "css", "app.css")));
        Assert.Contains("- `input/estilo/cliente/index.html` (1 líneas)", request.Prompt);
        Assert.Contains("- `input/estilo/cliente/css/app.css` (1 líneas)", request.Prompt);
        Assert.Contains("- `input/estilo/cliente/img/logo.png` (1 KB)", request.Prompt);
        Assert.DoesNotContain(MockupTask.NoStyle, request.Prompt);
        Assert.DoesNotContain("{{", request.Prompt);
        // The pages before the stylesheets.
        Assert.True(request.Prompt.IndexOf("index.html`", StringComparison.Ordinal) < request.Prompt.IndexOf("app.css`", StringComparison.Ordinal));
    }

    [Fact]
    public void The_images_of_the_reference_go_inside_the_page_and_unknown_paths_are_errors()
    {
        using var temp = new TempDirectory();
        StyleReference.Extract(ClientMockup(), Path.Combine(temp.Path, StyleReference.Folder), "cliente");
        var html = "<img src=\"input/estilo/cliente/img/logo.png\"><style>h1 { background: url('input/estilo/cliente/img/fondo.png') }</style>";

        var (inlined, missing) = StyleReference.Inline(temp.Path, html);

        Assert.Contains($"<img src=\"data:image/png;base64,{Convert.ToBase64String(Logo)}\">", inlined);
        Assert.Equal(["input/estilo/cliente/img/fondo.png"], missing);
    }

    [Fact]
    public void A_path_that_climbs_out_of_the_reference_is_not_read()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, "secreto.png"), "x");

        var (inlined, missing) = StyleReference.Inline(temp.Path, "<img src=\"input/estilo/../../secreto.png\">");

        Assert.DoesNotContain("data:", inlined);
        Assert.Single(missing);
    }
}
