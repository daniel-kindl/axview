extern alias host;

using host::AxView.Host.Catalog;

namespace AxView.Tests.Host;

public sealed class ProjectSourcesTests
{
    private static readonly string project = Path.Combine(Path.GetTempPath(), "App");

    [Theory]
    [InlineData("MainViewModel.cs")]
    [InlineData("Views/MainView.axaml.cs")]
    public void IsCodeFile_AcceptsACodeFileOfTheProject(string relativePath) =>
        Assert.True(ProjectSources.IsCodeFile(project, Path.Combine(project, relativePath)));

    [Theory]
    [InlineData("Views/MainView.axaml")]
    [InlineData("obj/Debug/net9.0/App.AssemblyInfo.cs")]
    [InlineData("bin/Debug/Generated.cs")]
    [InlineData(".vs/Temp.cs")]
    [InlineData("node_modules/Tool.cs")]
    public void IsCodeFile_RejectsOtherFilesAndBuildOutput(string relativePath) =>
        Assert.False(ProjectSources.IsCodeFile(project, Path.Combine(project, relativePath)));
}
