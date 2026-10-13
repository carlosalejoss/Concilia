namespace Concilia.Tests;

// A fresh folder under the system temp directory, deleted at the end of the test
// (use it with "using var temp = new TempDirectory();").
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "concilia-tests", Guid.NewGuid().ToString("N"));

    public TempDirectory()
    {
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A process killed by a test may still hold a file for a moment;
            // leaving a temp folder behind is harmless.
        }
    }
}
