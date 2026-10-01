using AxView.Hosting;
using AxView.Protocol;
using AxView.Resolution;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class HostSessionTests
{
    [Fact]
    public async Task RunAsync_ReturnsTheExitCodeOfTheHostAsync()
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
    public async Task RunAsync_StartsANewHostForTheFileOfTheHandOffAsync()
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
    public async Task RunAsync_StartsTheHostAgainOnThePreviousFileWhenTheNextFileFailsAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string first = AppFixture.WriteProject(dir, "First");
        _ = dir.Write("src/Unbuilt/Unbuilt.csproj", "<Project />");
        string unbuilt = dir.Write("src/Unbuilt/Views/Main.axaml");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.SwitchProject, nextFile: unbuilt);
        hosts.Enqueue(ExitReason.Ok);
        List<string> opened = [];

        int exitCode = await Session(hosts).RunAsync(first, outputDir: null, snapshotPath: null, opened.Add);

        Assert.Equal((int)ExitReason.Ok, exitCode);
        Assert.Equal([first], opened);
        Assert.Equal([first, first], hosts.Runs.Select(options => options.AxamlPath));
        Assert.Null(hosts.Runs[0].SetupError);
        Assert.Contains("Unbuilt", hosts.Runs[1].SetupError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_StopsWhenTheHostAsksForASwitchWithoutAHandOffAsync()
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
    public async Task RunAsync_StartsNoHostWhenTheSetupFailsAsync()
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
