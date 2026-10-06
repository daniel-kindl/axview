using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class HostProtocolTests
{
    private static readonly HostOptions full = new(
        AxamlPath: "/src/Ui/Views/Main.axaml",
        ProjectDirectory: "/src/Ui",
        AssemblyName: "Ui",
        AppAssemblyName: "Desktop",
        LoaderPath: "/nuget/loader.dll",
        SnapshotPath: "/out/shot.png",
        HandOffFile: "/tmp/hand-off.json",
        SetupError: "The app is not built.",
        SettingsFile: "/tools/axview/settings.json",
        AppProjectDirectory: "/src/Desktop",
        ViewModelName: "Ui.ViewModels.MainViewModel",
        ReferencedProjects: string.Join(Path.PathSeparator, "/src/Core", "/src/Common"),
        ChromeThemePath: "/nuget/theme.dll",
        Size: new PreviewSize(PreviewSizePreset.Custom, 500, 300),
        Theme: PreviewTheme.Dark);

    [Fact]
    public void Parse_ReadsTheArgumentsThatToArgumentsWrites()
    {
        HostOptions parsed = HostOptions.Parse(full.ToArguments());

        Assert.Equal(full, parsed);
    }

    [Fact]
    public void Parse_ReadsTheArgumentsWithoutTheOptionalValues()
    {
        HostOptions options = full with
        {
            AppAssemblyName = null,
            SnapshotPath = null,
            HandOffFile = null,
            SetupError = null,
            SettingsFile = null,
            AppProjectDirectory = null,
            ViewModelName = null,
            ReferencedProjects = null,
            ChromeThemePath = null,
            Size = null,
            Theme = null,
        };

        HostOptions parsed = HostOptions.Parse(options.ToArguments());

        Assert.Equal(options, parsed);
    }

    [Fact]
    public void Parse_ReadsTheSnapshotFolder()
    {
        HostOptions options = full with { SnapshotPath = null, ViewModelName = null, SnapshotAllDirectory = "/out/shots" };

        HostOptions parsed = HostOptions.Parse(options.ToArguments());

        Assert.Equal(options, parsed);
    }

    [Fact]
    public void Mode_GivesTheHostModeOfTheOptions()
    {
        Assert.Equal(HostMode.Snapshot, full.Mode);
        Assert.Equal(HostMode.Preview, (full with { SnapshotPath = null, ViewModelName = null }).Mode);
        Assert.Equal(HostMode.SnapshotAll, (full with { SnapshotPath = null, ViewModelName = null, SnapshotAllDirectory = "/out" }).Mode);
    }

    [Fact]
    public void Parse_RejectsASnapshotWithASnapshotFolder()
    {
        string[] args = [.. full.ToArguments(), "--snapshot-all", "/out/shots"];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));

        Assert.Contains("--snapshot-all", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_RejectsAViewModelWithoutASnapshot()
    {
        string[] args = [.. (full with { SnapshotPath = null }).ToArguments()];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));

        Assert.Contains("--view-model", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_RejectsAViewModelWithASnapshotFolder()
    {
        string[] args = [.. (full with { SnapshotPath = null, SnapshotAllDirectory = "/out/shots" }).ToArguments()];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));

        Assert.Contains("--view-model", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_ReadsTheRenderFlagsWithASnapshotFolder()
    {
        HostOptions options = full with { SnapshotPath = null, ViewModelName = null, SnapshotAllDirectory = "/out/shots" };

        HostOptions parsed = HostOptions.Parse(options.ToArguments());

        Assert.Equal(options, parsed);
    }

    [Theory]
    [InlineData("--size", "Phone")]
    [InlineData("--theme", "Dark")]
    public void ValidateMode_RejectsARenderFlagWithoutASnapshot(string key, string value)
    {
        HostOptions preview = full with { SnapshotPath = null, ViewModelName = null, Size = null, Theme = null };
        string[] args = [.. preview.ToArguments(), key, value];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));

        Assert.Contains(key, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_RejectsASizeThatIsNotASize()
    {
        string[] args = [.. (full with { Size = null }).ToArguments(), "--size", "Huge"];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));

        Assert.Contains("'Huge'", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Default", (int)PreviewTheme.Default)]
    [InlineData("light", (int)PreviewTheme.Light)]
    [InlineData("DARK", (int)PreviewTheme.Dark)]
    public void ParseTheme_ReadsTheNameInAnyCase(string text, int theme) =>
        Assert.Equal((PreviewTheme)theme, HostOptions.ParseTheme(text));

    [Theory]
    [InlineData("Blue")]
    [InlineData("1")]
    [InlineData("")]
    public void ParseTheme_RejectsAnUnknownName(string text)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.ParseTheme(text));

        Assert.Contains($"'{text}'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReferencedProjectDirectories_SplitsTheOption()
    {
        Assert.Equal(["/src/Core", "/src/Common"], full.ReferencedProjectDirectories);
        Assert.Empty((full with { ReferencedProjects = null }).ReferencedProjectDirectories);
    }

    [Fact]
    public void Parse_RejectsAnUnknownKey()
    {
        string[] args = [.. full.ToArguments(), "--color", "Red"];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));

        Assert.Contains("--color", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_RejectsAKeyWithoutAValue()
    {
        string[] args = [.. full.ToArguments(), "--snapshot"];

        _ = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));
    }

    [Fact]
    public void Parse_RejectsADuplicateKey()
    {
        string[] args = [.. full.ToArguments(), "--assembly", "Other"];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));

        Assert.Contains("--assembly", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_RejectsAMissingRequiredKey()
    {
        string[] args = [.. full.ToArguments().Where((_, index) => index > 1)];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));

        Assert.Contains("--axaml", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExitReason_KeepsTheExitCodesThatScriptsSee()
    {
        Assert.Equal(
            [0, 1, 2, 3, 4],
            [
                (int)ExitReason.Ok,
                (int)ExitReason.RenderFailed,
                (int)ExitReason.BadArguments,
                (int)ExitReason.SwitchProject,
                (int)ExitReason.SetupFailed,
            ]);
    }

    [Theory]
    [InlineData((int)ExitReason.SwitchProject)]
    [InlineData((int)ExitReason.Rebuild)]
    public void Take_GivesTheHandOffThatSaveWrites(int reasonCode)
    {
        using TempDir dir = new();
        string file = Path.Combine(dir.Path, "hand-off.json");
        HandOff handOff = new((ExitReason)reasonCode, "/src/Other/Views/Main.axaml");

        int exitCode = handOff.Save(file);

        Assert.Equal(reasonCode, exitCode);
        Assert.Equal(handOff, HandOff.Take(file, exitCode));
    }

    [Fact]
    public void Take_DeletesTheFile()
    {
        using TempDir dir = new();
        string file = Path.Combine(dir.Path, "hand-off.json");
        int exitCode = new HandOff(ExitReason.Rebuild, "/src/Ui/Views/Main.axaml").Save(file);

        _ = HandOff.Take(file, exitCode);

        Assert.False(File.Exists(file));
    }

    [Fact]
    public void Take_ReturnsNullWhenTheHostWroteNoHandOff()
    {
        using TempDir dir = new();

        Assert.Null(HandOff.Take(Path.Combine(dir.Path, "hand-off.json"), (int)ExitReason.SwitchProject));
    }

    [Theory]
    [InlineData((int)ExitReason.Ok)]
    [InlineData((int)ExitReason.RenderFailed)]
    public void Take_IgnoresTheFileForAnExitCodeWithoutAHandOff(int exitCode)
    {
        using TempDir dir = new();
        string file = Path.Combine(dir.Path, "hand-off.json");
        _ = new HandOff(ExitReason.SwitchProject, "/src/Other/Views/Main.axaml").Save(file);

        Assert.Null(HandOff.Take(file, exitCode));
    }

    [Fact]
    public void Constructor_RejectsAReasonWithoutAHandOff() =>
        Assert.Throws<ArgumentException>(() => new HandOff(ExitReason.Ok, "/src/Ui/Views/Main.axaml"));
}
