using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class SettingsTests
{
    [Fact]
    public void Load_ReadsTheSettingsThatSaveWrites()
    {
        using TempDir dir = new();
        string file = Path.Combine(dir.Path, "sub", "settings.json");
        Settings settings = new()
        {
            RecentFiles = ["/src/Ui/Views/Main.axaml"],
            Window = new WindowPlacement(WindowMode.Maximized, 10, 20, 1000, 700),
            Theme = PreviewTheme.Dark,
            Size = new PreviewSize(PreviewSizePreset.Custom, 500, 300),
            ViewModels = new Dictionary<string, string> { ["/src/Ui/Views/Main.axaml"] = "Ui.ViewModels.MainViewModel" },
        };

        settings.Save(file);
        Settings loaded = Settings.Load(file);

        Assert.Equal(settings.RecentFiles, loaded.RecentFiles);
        Assert.Equal(settings.Window, loaded.Window);
        Assert.Equal(PreviewTheme.Dark, loaded.Theme);
        Assert.Equal(settings.Size, loaded.Size);
        Assert.Equal(settings.ViewModels, loaded.ViewModels);
    }

    [Fact]
    public void Save_WritesTheEnumValuesAsNames()
    {
        using TempDir dir = new();
        string file = Path.Combine(dir.Path, "settings.json");

        (Settings.Default with { Theme = PreviewTheme.Light }).Save(file);

        Assert.Contains("\"Light\"", File.ReadAllText(file), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_GivesTheDefaultsWhenTheFileIsMissing()
    {
        using TempDir dir = new();

        Settings loaded = Settings.Load(Path.Combine(dir.Path, "settings.json"));

        AssertDefault(loaded);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{ \"Theme\": \"Purple\" }")]
    public void Load_GivesTheDefaultsWhenTheFileIsNotValid(string json)
    {
        using TempDir dir = new();

        Settings loaded = Settings.Load(dir.Write("settings.json", json));

        AssertDefault(loaded);
    }

    [Fact]
    public void Load_GivesTheDefaultForEachMissingValue()
    {
        using TempDir dir = new();

        Settings loaded = Settings.Load(dir.Write("settings.json", "{ \"Theme\": \"Dark\", \"RecentFiles\": null, \"Size\": null, \"ViewModels\": null }"));

        Assert.Equal(PreviewTheme.Dark, loaded.Theme);
        Assert.Empty(loaded.RecentFiles);
        Assert.Null(loaded.Window);
        Assert.Equal(PreviewSize.Fit, loaded.Size);
        Assert.Empty(loaded.ViewModels);
    }

    private static void AssertDefault(Settings settings)
    {
        Assert.Empty(settings.RecentFiles);
        Assert.Null(settings.Window);
        Assert.Equal(PreviewTheme.Default, settings.Theme);
        Assert.Equal(PreviewSize.Fit, settings.Size);
        Assert.Empty(settings.ViewModels);
    }
}
