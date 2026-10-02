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
        SnapshotAllDirectory: "/out/shots",
        ViewModelName: "Ui.ViewModels.MainViewModel");

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
            SnapshotAllDirectory = null,
            ViewModelName = null,
        };

        HostOptions parsed = HostOptions.Parse(options.ToArguments());

        Assert.Equal(options, parsed);
    }

    [Fact]
    public void Parse_RejectsAnUnknownKey()
    {
        string[] args = [.. full.ToArguments(), "--theme", "Dark"];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => HostOptions.Parse(args));

        Assert.Contains("--theme", ex.Message, StringComparison.Ordinal);
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

    [Fact]
    public void Load_ReadsTheHandOffThatSaveWrites()
    {
        using TempDir dir = new();
        string file = Path.Combine(dir.Path, "hand-off.json");
        HandOff handOff = new("/src/Other/Views/Main.axaml");

        handOff.Save(file);

        Assert.Equal(handOff, HandOff.Load(file));
    }

    [Fact]
    public void Load_ReturnsNullWhenTheHostWroteNoHandOff()
    {
        using TempDir dir = new();

        Assert.Null(HandOff.Load(Path.Combine(dir.Path, "hand-off.json")));
    }
}
