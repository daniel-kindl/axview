using AxView.Resolution;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class ProjectFinderTests
{
    [Fact]
    public void FindProject_ReturnsNearestProjectAboveTheFile()
    {
        using TempDir dir = new();
        string project = dir.Write("App/App.csproj", "<Project />");
        string axaml = dir.Write("App/Views/Main/MainView.axaml");

        string found = ProjectFinder.FindProject(axaml);

        Assert.Equal(project, found);
    }

    [Fact]
    public void FindProject_ThrowsWhenNoProjectExists()
    {
        using TempDir dir = new();
        string axaml = dir.Write("Loose/View.axaml");

        _ = Assert.Throws<PreviewSetupException>(() => ProjectFinder.FindProject(axaml));
    }

    [Fact]
    public void ReadAssemblyName_UsesAssemblyNameProperty()
    {
        using TempDir dir = new();
        string project = dir.Write("App/App.csproj",
            "<Project><PropertyGroup><AssemblyName>My.Ui</AssemblyName></PropertyGroup></Project>");

        Assert.Equal("My.Ui", ProjectFinder.ReadAssemblyName(project));
    }

    [Fact]
    public void ReadAssemblyName_FallsBackToProjectFileName()
    {
        using TempDir dir = new();
        string project = dir.Write("App/My.App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        Assert.Equal("My.App", ProjectFinder.ReadAssemblyName(project));
    }
}
