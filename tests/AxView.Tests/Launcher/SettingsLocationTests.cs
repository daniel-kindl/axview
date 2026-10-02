using AxView.Picker;
using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class SettingsLocationTests
{
    [Fact]
    public void Find_UsesTheAppFolderWhenItIsWritable()
    {
        using TempDir dir = new();
        string app = Path.Combine(dir.Path, "app");
        _ = Directory.CreateDirectory(app);

        string file = SettingsLocation.Find(app, Path.Combine(dir.Path, "user"));

        Assert.Equal(Path.Combine(app, SettingsLocation.FileName), file);
        Assert.Equal([], Directory.GetFiles(app));
    }

    [Fact]
    public void Find_UsesTheUserFolderWhenTheAppFolderIsNotWritable()
    {
        using TempDir dir = new();
        string user = Path.Combine(dir.Path, "user");

        string file = SettingsLocation.Find(Path.Combine(dir.Path, "missing"), user);

        Assert.Equal(Path.Combine(user, SettingsLocation.FileName), file);
    }

    [Fact]
    public void ImportRecentFiles_CopiesTheOldListIntoANewSettingsFile()
    {
        using TempDir dir = new();
        string legacy = dir.Write("recent.json", "[\"/src/a.axaml\",\"/src/b.axaml\"]");
        string settings = Path.Combine(dir.Path, "settings.json");

        SettingsLocation.ImportRecentFiles(legacy, settings);

        Assert.Equal(["/src/a.axaml", "/src/b.axaml"], Settings.Load(settings).RecentFiles);
    }

    [Fact]
    public void ImportRecentFiles_KeepsAnExistingSettingsFile()
    {
        using TempDir dir = new();
        string legacy = dir.Write("recent.json", "[\"/src/a.axaml\"]");
        string settings = Path.Combine(dir.Path, "settings.json");
        (Settings.Default with { RecentFiles = ["/src/new.axaml"] }).Save(settings);

        SettingsLocation.ImportRecentFiles(legacy, settings);

        Assert.Equal(["/src/new.axaml"], Settings.Load(settings).RecentFiles);
    }

    [Fact]
    public void ImportRecentFiles_IgnoresACorruptOldList()
    {
        using TempDir dir = new();
        string legacy = dir.Write("recent.json", "{ not json");
        string settings = Path.Combine(dir.Path, "settings.json");

        SettingsLocation.ImportRecentFiles(legacy, settings);

        Assert.False(File.Exists(settings));
    }
}
