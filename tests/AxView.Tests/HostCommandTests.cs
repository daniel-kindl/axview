namespace AxView.Tests;

public sealed class HostCommandTests
{
    private static readonly string Root = Path.GetTempPath();
    private static readonly string OutDir = Path.Combine(Root, "out");

    private static readonly AppOutput Output = new(
        OutDir, Path.Combine(OutDir, "Desktop.deps.json"), Path.Combine(OutDir, "Desktop.runtimeconfig.json"));

    private static readonly PreviewTarget Target = new(
        AxamlPath: Path.Combine(Root, "src", "Ui", "Views", "Main.axaml"),
        ProjectDirectory: Path.Combine(Root, "src", "Ui"),
        AssemblyName: "Ui",
        Output: Output,
        LoaderPath: Path.Combine(Root, "nuget", "loader.dll"));

    [Fact]
    public void Arguments_RunTheHostInsideTheTargetRuntime()
    {
        string host = Path.Combine(Root, "tool", "host", "AxView.Host.dll");

        IReadOnlyList<string> args = HostCommand.Arguments(Target, host);

        Assert.Equal(
            [
                "exec",
                "--runtimeconfig", Output.RuntimeConfig,
                "--depsfile", Output.DepsFile,
                host,
                "--axaml", Target.AxamlPath,
                "--project", Target.ProjectDirectory,
                "--assembly", "Ui",
                "--app-assembly", "Desktop",
                "--loader", Target.LoaderPath,
            ],
            args);
    }

    [Fact]
    public void Arguments_PassTheSnapshotPathToTheHost()
    {
        IReadOnlyList<string> args = HostCommand.Arguments(Target, "host.dll", snapshotPath: "shot.png");

        Assert.Equal(["--snapshot", "shot.png"], args.TakeLast(2));
    }

    [Fact]
    public void Arguments_PassTheSwitchFileToTheHost()
    {
        IReadOnlyList<string> args = HostCommand.Arguments(Target, "host.dll", switchFile: "switch.txt");

        Assert.Equal(["--switch-file", "switch.txt"], args.TakeLast(2));
    }
}
