namespace ReqArbiter.Runner.Execution;

// Folder layout of one run, inside data/runs/{runId}/:
//   input/          files the model may read
//   output/         files the model must write
//   claude-config/  empty CLAUDE_CONFIG_DIR, so no user settings or sessions leak in
//   home/           HOME of the child process, for the same reason
public record RunFolders(string Root)
{
    public string Input => Path.Combine(Root, "input");
    public string Output => Path.Combine(Root, "output");
    public string ClaudeConfig => Path.Combine(Root, "claude-config");
    public string Home => Path.Combine(Root, "home");

    public static RunFolders Create(string workDirectory)
    {
        var folders = new RunFolders(Path.GetFullPath(workDirectory));
        Directory.CreateDirectory(folders.Input);
        Directory.CreateDirectory(folders.Output);
        Directory.CreateDirectory(folders.ClaudeConfig);
        Directory.CreateDirectory(folders.Home);
        return folders;
    }
}
