using AxView.Hosting;
using AxView.Protocol;
using AxView.Resolution;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class HostSessionTests
{
    private const string SettingsFile = "/tools/axview/settings.json";

    [Fact]
    public async Task RunAsync_ReturnsTheExitCodeOfTheHostAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.RenderFailed);
        List<string> opened = [];

        int exitCode = await Session(hosts).RunAsync(view, outputDir: null, opened.Add, new SnapshotRequest(Path: "shot.png"));

        Assert.Equal((int)ExitReason.RenderFailed, exitCode);
        Assert.Equal([view], opened);
        HostOptions options = Assert.Single(hosts.Runs);
        Assert.Equal(view, options.AxamlPath);
        Assert.Equal("shot.png", options.SnapshotPath);
        Assert.Null(options.SettingsFile);
        Assert.Equal(Path.Combine(dir.Path, "src", "Ui", "bin", "Debug", "net9.0"), options.OutputDirectory);
    }

    [Fact]
    public async Task RunAsync_GivesTheSnapshotFolderToTheHostAndIgnoresTheSettingsAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Ok);

        _ = await Session(hosts).RunAsync(view, null, _ => { }, new SnapshotRequest(AllDirectory: "/out/shots"));

        HostOptions options = Assert.Single(hosts.Runs);
        Assert.Equal("/out/shots", options.SnapshotAllDirectory);
        Assert.Null(options.SettingsFile);
    }

    [Fact]
    public async Task RunAsync_GivesTheViewModelNameToTheHostAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Ok);

        _ = await Session(hosts).RunAsync(view, null, _ => { }, new SnapshotRequest("shot.png", ViewModelName: "Ui.MainViewModel"));

        Assert.Equal("Ui.MainViewModel", Assert.Single(hosts.Runs).ViewModelName);
    }

    [Fact]
    public async Task RunAsync_GivesTheRenderFlagsToTheHostAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Ok);
        PreviewSize size = new(PreviewSizePreset.Custom, 500, 300);

        _ = await Session(hosts).RunAsync(view, null, _ => { }, new SnapshotRequest("shot.png", Size: size, Theme: PreviewTheme.Dark, TreePath: "tree.json", ReportPath: "report.json", FitsContent: true));

        HostOptions options = Assert.Single(hosts.Runs);
        Assert.Equal(size, options.Size);
        Assert.Equal(PreviewTheme.Dark, options.Theme);
        Assert.Equal("tree.json", options.TreePath);
        Assert.Equal("report.json", options.ReportPath);
        Assert.True(options.FitsContent);
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

        int exitCode = await Session(hosts).RunAsync(first, firstOutput, opened.Add);

        Assert.Equal((int)ExitReason.Ok, exitCode);
        Assert.Equal([first, second], opened);
        Assert.Equal([first, second], hosts.Runs.Select(options => options.AxamlPath));
        Assert.Equal("Second", hosts.Runs[1].AssemblyName);
        Assert.False(File.Exists(hosts.Runs[0].HandOffFile));
        Assert.All(hosts.Runs, options => Assert.Equal(SettingsFile, options.SettingsFile));
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

        int exitCode = await Session(hosts).RunAsync(first, outputDir: null, opened.Add);

        Assert.Equal((int)ExitReason.Ok, exitCode);
        Assert.Equal([first], opened);
        Assert.Equal([first, first], hosts.Runs.Select(options => options.AxamlPath));
        Assert.Null(hosts.Runs[0].SetupError);
        Assert.StartsWith("Cannot open the file:", hosts.Runs[1].SetupError, StringComparison.Ordinal);
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

        int exitCode = await Session(hosts).RunAsync(view, outputDir: null, _ => { });

        Assert.Equal((int)ExitReason.SwitchProject, exitCode);
        _ = Assert.Single(hosts.Runs);
    }

    [Fact]
    public async Task RunAsync_BuildsTheAppAndStartsTheHostAgainOnTheFileOfTheHandOffAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        string other = dir.Write("src/Ui/Views/Other.axaml");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Rebuild, nextFile: other);
        hosts.Enqueue(ExitReason.Ok);
        FakeOutputBuilder builder = new();

        int exitCode = await Session(hosts, builder).RunAsync(view, outputDir: null, _ => { });

        Assert.Equal((int)ExitReason.Ok, exitCode);
        AppOutput built = Assert.Single(builder.Builds);
        Assert.Equal(Path.Combine(dir.Path, "src", "Ui", "bin", "Debug", "net9.0"), built.Directory);
        Assert.Equal([view, other], hosts.Runs.Select(options => options.AxamlPath));
        Assert.Null(hosts.Runs[1].SetupError);
        Assert.Equal(SettingsFile, hosts.Runs[1].SettingsFile);
    }

    [Fact]
    public async Task RunAsync_ShowsTheProgressOfTheBuildUntilTheBuildStopsAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Rebuild, nextFile: view);
        hosts.Enqueue(ExitReason.Ok);
        FakeOutputBuilder builder = new();
        FakeBuildDisplay display = new();

        _ = await Session(hosts, builder, display).RunAsync(view, outputDir: null, _ => { });

        FakeBuildProgress progress = Assert.Single(display.Shown);
        Assert.Equal("Ui", progress.AppName);
        Assert.Equal([FakeOutputBuilder.BuildLine], progress.Lines);
        Assert.Equal(progress.Cancellation, Assert.Single(builder.Cancellations));
        Assert.True(progress.IsDisposed);
    }

    [Fact]
    public async Task RunAsync_ClosesTheBuildProgressWhenTheBuildFailsAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Rebuild, nextFile: view);
        hosts.Enqueue(ExitReason.Ok);
        FakeBuildDisplay display = new();

        _ = await Session(hosts, new FakeOutputBuilder("error"), display).RunAsync(view, null, _ => { });

        Assert.True(Assert.Single(display.Shown).IsDisposed);
    }

    [Fact]
    public async Task RunAsync_ShowsTheBuildErrorInTheNextHostWhenTheBuildFailsAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        string other = dir.Write("src/Ui/Views/Other.axaml");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Rebuild, nextFile: other);
        hosts.Enqueue(ExitReason.Ok);
        FakeOutputBuilder builder = new("Program.cs(3,1): error CS1002: ; expected");

        _ = await Session(hosts, builder).RunAsync(view, outputDir: null, _ => { });

        Assert.Equal([view, other], hosts.Runs.Select(options => options.AxamlPath));
        Assert.Equal("Program.cs(3,1): error CS1002: ; expected", hosts.Runs[1].SetupError);
    }

    [Fact]
    public async Task RunAsync_BuildsTheAppBeforeTheFirstHostWhenACodeFileIsNewerThanTheOutputAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        File.SetLastWriteTimeUtc(dir.Write("src/Ui/MainViewModel.cs"), DateTime.UtcNow.AddHours(1));
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Ok);
        FakeOutputBuilder builder = new();

        _ = await Session(hosts, builder).RunAsync(view, outputDir: null, _ => { });

        AppOutput built = Assert.Single(builder.Builds);
        Assert.Equal(Path.Combine(dir.Path, "src", "Ui", "bin", "Debug", "net9.0"), built.Directory);
        Assert.Null(Assert.Single(hosts.Runs).SetupError);
    }

    [Fact]
    public async Task RunAsync_BuildsTheAppWhenACodeFileOfAReferencedProjectIsNewerThanTheOutputAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        _ = dir.Write("src/Ui/Ui.csproj", """<Project><ItemGroup><ProjectReference Include="../Core/Core.csproj" /></ItemGroup></Project>""");
        _ = dir.Write("src/Core/Core.csproj", "<Project />");
        File.SetLastWriteTimeUtc(dir.Write("src/Core/Service.cs"), DateTime.UtcNow.AddHours(1));
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Ok);
        FakeOutputBuilder builder = new();

        _ = await Session(hosts, builder).RunAsync(view, outputDir: null, _ => { });

        _ = Assert.Single(builder.Builds);
    }

    [Fact]
    public async Task RunAsync_StartsTheFirstHostWithoutABuildWhenTheOutputIsNewerThanTheCodeAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        File.SetLastWriteTimeUtc(dir.Write("src/Ui/MainViewModel.cs"), DateTime.UtcNow.AddHours(-1));
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Ok);
        FakeOutputBuilder builder = new();

        _ = await Session(hosts, builder).RunAsync(view, outputDir: null, _ => { });

        Assert.Empty(builder.Builds);
        _ = Assert.Single(hosts.Runs);
    }

    [Fact]
    public async Task RunAsync_StartsTheFirstHostWithoutABuildWhenTheOutputIsGivenAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        File.SetLastWriteTimeUtc(dir.Write("src/Ui/MainViewModel.cs"), DateTime.UtcNow.AddHours(1));
        string output = Path.Combine(dir.Path, "src", "Ui", "bin", "Debug", "net9.0");
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Ok);
        FakeOutputBuilder builder = new();

        _ = await Session(hosts, builder).RunAsync(view, output, _ => { });

        Assert.Empty(builder.Builds);
    }

    [Fact]
    public async Task RunAsync_ShowsTheBuildErrorInTheFirstHostWhenTheFirstBuildFailsAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string view = AppFixture.WriteProject(dir, "Ui");
        File.SetLastWriteTimeUtc(dir.Write("src/Ui/MainViewModel.cs"), DateTime.UtcNow.AddHours(1));
        FakeHostProcess hosts = new();
        hosts.Enqueue(ExitReason.Ok);
        FakeBuildDisplay display = new();

        _ = await Session(hosts, new FakeOutputBuilder("MainViewModel.cs(3,1): error CS1002: ; expected"), display)
            .RunAsync(view, outputDir: null, _ => { });

        HostOptions options = Assert.Single(hosts.Runs);
        Assert.Equal(view, options.AxamlPath);
        Assert.Equal("MainViewModel.cs(3,1): error CS1002: ; expected", options.SetupError);
        Assert.True(Assert.Single(display.Shown).IsDisposed);
    }

    [Fact]
    public async Task RunAsync_StartsNoHostWhenTheSetupFailsAsync()
    {
        using TempDir dir = new();
        FakeHostProcess hosts = new();

        _ = await Assert.ThrowsAsync<PreviewSetupException>(
            () => Session(hosts).RunAsync(Path.Combine(dir.Path, "Missing.axaml"), null, _ => { }));

        Assert.Empty(hosts.Runs);
    }

    private static HostSession Session(FakeHostProcess hosts) => Session(hosts, new FakeOutputBuilder());

    private static HostSession Session(FakeHostProcess hosts, FakeOutputBuilder builder) =>
        Session(hosts, builder, new FakeBuildDisplay());

    private static HostSession Session(FakeHostProcess hosts, FakeOutputBuilder builder, FakeBuildDisplay display) =>
        new(new PreviewTargetResolver(new FakePackageSource()), hosts, builder, display, TextWriter.Null, SettingsFile);
}
