namespace AxView.Tests;

public sealed class RecentFilesTests
{
    [Fact]
    public void Add_PutsTheFileFirst()
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "recent.json"));

        recent.Add("a.axaml");
        recent.Add("b.axaml");

        Assert.Equal([Path.GetFullPath("b.axaml"), Path.GetFullPath("a.axaml")], recent.Files);
    }

    [Fact]
    public void Add_MovesAKnownFileToTheTopWithoutADuplicate()
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "recent.json"));

        recent.Add("a.axaml");
        recent.Add("b.axaml");
        recent.Add("a.axaml");

        Assert.Equal([Path.GetFullPath("a.axaml"), Path.GetFullPath("b.axaml")], recent.Files);
    }

    [Fact]
    public void Add_KeepsOnlyTheNewestFiles()
    {
        using TempDir dir = new();
        RecentFiles recent = new(Path.Combine(dir.Path, "recent.json"));

        for (int i = 0; i < RecentFiles.Capacity + 5; i++)
        {
            recent.Add($"{i}.axaml");
        }

        Assert.Equal(RecentFiles.Capacity, recent.Files.Count);
        Assert.Equal(Path.GetFullPath($"{RecentFiles.Capacity + 4}.axaml"), recent.Files[0]);
    }

    [Fact]
    public void Files_SurviveANewInstance()
    {
        using TempDir dir = new();
        string store = Path.Combine(dir.Path, "sub", "recent.json");
        new RecentFiles(store).Add("a.axaml");

        RecentFiles reloaded = new(store);

        Assert.Equal([Path.GetFullPath("a.axaml")], reloaded.Files);
    }

    [Fact]
    public void Files_AreEmptyWhenTheStoreIsCorrupt()
    {
        using TempDir dir = new();
        string store = dir.Write("recent.json", "{ not json");

        Assert.Empty(new RecentFiles(store).Files);
    }
}
