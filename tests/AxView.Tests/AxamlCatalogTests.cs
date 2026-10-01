using AxView.Host;

namespace AxView.Tests;

public sealed class AxamlCatalogTests
{
    [Fact]
    public void List_ReturnsTheProjectFilesSortedByRelativePath()
    {
        using TempDir dir = new();
        _ = dir.Write("App/App.csproj");
        string view = dir.Write("App/Views/MainView.axaml");
        string app = dir.Write("App/App.axaml");
        _ = dir.Write("App/Views/MainView.axaml.cs");

        IReadOnlyList<string> files = AxamlCatalog.List(Path.Combine(dir.Path, "App"));

        Assert.Equal([app, view], files);
    }

    [Fact]
    public void List_SkipsBuildOutputAndHiddenFolders()
    {
        using TempDir dir = new();
        _ = dir.Write("App/App.csproj");
        string view = dir.Write("App/View.axaml");
        _ = dir.Write("App/bin/Debug/View.axaml");
        _ = dir.Write("App/obj/View.axaml");
        _ = dir.Write("App/.vs/View.axaml");

        Assert.Equal([view], AxamlCatalog.List(Path.Combine(dir.Path, "App")));
    }

    [Fact]
    public void List_SkipsFoldersThatBelongToAnotherProject()
    {
        using TempDir dir = new();
        _ = dir.Write("App/App.csproj");
        string view = dir.Write("App/View.axaml");
        _ = dir.Write("App/Nested/Nested.csproj");
        _ = dir.Write("App/Nested/Other.axaml");

        Assert.Equal([view], AxamlCatalog.List(Path.Combine(dir.Path, "App")));
    }
}
