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
    public void FindAppProject_ReturnsTheProjectAboveTheOutputThatBuildsTheApp()
    {
        using TempDir dir = new();
        _ = dir.Write("Ui/Ui.csproj", "<Project />");
        string app = dir.Write("Desktop/Desktop.csproj", "<Project />");
        string outputDir = AppFixture.WriteAppOutput(dir, "Desktop/bin/Debug/net9.0", "Desktop", "Ui");
        AppOutput output = AppOutput.FromRuntimeConfig(Path.Combine(outputDir, "Desktop.runtimeconfig.json"));

        Assert.Equal(app, ProjectFinder.FindAppProject(output));
    }

    [Fact]
    public void FindAppProject_ThrowsWhenNoProjectAboveTheOutputBuildsTheApp()
    {
        using TempDir dir = new();
        _ = dir.Write("Other/Other.csproj", "<Project />");
        string outputDir = AppFixture.WriteAppOutput(dir, "Other/bin/Debug/net9.0", "Desktop", "Ui");
        AppOutput output = AppOutput.FromRuntimeConfig(Path.Combine(outputDir, "Desktop.runtimeconfig.json"));

        _ = Assert.Throws<PreviewSetupException>(() => ProjectFinder.FindAppProject(output));
    }
}
