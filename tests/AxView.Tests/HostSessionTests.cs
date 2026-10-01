using AxView.Protocol;

namespace AxView.Tests;

public sealed class HostSessionTests
{
    [Fact]
    public async Task RunAsync_ReturnsTheExitCodeOfTheHost()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.RenderFailed);
        List<string> opened = [];

        int exitCode = await Session(hosts).RunAsync(view, outputDir: null, snapshotPath: "shot.png", opened.Add);

        Assert.Equal((int)ExitReason.RenderFailed, exitCode);
        Assert.Equal([view], opened);
        HostOptions options = Assert.Single(hosts.Runs);
        Assert.Equal(view, options.AxamlPath);
        Assert.Equal("shot.png", options.SnapshotPath);
    }

    [Fact]
    public async Task RunAsync_StartsANewHostForTheFileOfTheHandOff()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string first = AppFixture.WriteProject(dir, "First");
        string second = AppFixture.WriteProject(dir, "Second");
        string firstOutput = Path.Combine(dir.Path, "src", "First", "bin", "Debug", "net9.0");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.SwitchProject, nextFile: second);
        hosts.Enqueue(ExitReason.Ok);
        List<string> opened = [];

        int exitCode = await Session(hosts).RunAsync(first, firstOutput, snapshotPath: null, opened.Add);

        Assert.Equal((int)ExitReason.Ok, exitCode);
        Assert.Equal([first, second], opened);
        Assert.Equal([first, second], hosts.Runs.Select(options => options.AxamlPath));
        Assert.Equal("Second", hosts.Runs[1].AssemblyName);
        Assert.False(File.Exists(hosts.Runs[0].HandOffFile));
    }

    [Fact]
    public async Task RunAsync_StopsWhenTheHostAsksForASwitchWithoutAHandOff()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.SwitchProject);

        int exitCode = await Session(hosts).RunAsync(view, outputDir: null, snapshotPath: null, _ => { });

        Assert.Equal((int)ExitReason.SwitchProject, exitCode);
        _ = Assert.Single(hosts.Runs);
    }

    [Fact]
    public async Task RunAsync_StartsNoHostWhenTheSetupFails()
    {
        using TempDir dir = new();
        FakeHostProcess hosts = new();

        _ = await Assert.ThrowsAsync<PreviewSetupException>(
            () => Session(hosts).RunAsync(Path.Combine(dir.Path, "Missing.axaml"), null, null, _ => { }));

        Assert.Empty(hosts.Runs);
    }

    private static HostSession Session(FakeHostProcess hosts) =>
        new(new PreviewTargetResolver(new FakeLoaderSource()), hosts, TextWriter.Null);
}
