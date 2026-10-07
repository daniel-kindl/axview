

using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

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

    [Fact]
    public void IsAxamlFile_AcceptsAnAxamlFileOfTheProject() =>
        Assert.True(ProjectSources.IsAxamlFile(project, Path.Combine(project, "Styles", "Main.axaml")));

    [Theory]
    [InlineData("Views/MainView.axaml.cs")]
    [InlineData("obj/Debug/net9.0/Avalonia/Main.axaml")]
    public void IsAxamlFile_RejectsOtherFilesAndBuildOutput(string relativePath) =>
        Assert.False(ProjectSources.IsAxamlFile(project, Path.Combine(project, relativePath)));

    [Fact]
    public void GetLastCodeWriteTime_GivesTheTimeOfTheNewestCodeFileOutsideTheBuildOutput()
    {
        using TempDir dir = new();
        DateTime newest = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(dir.Write("App/Old.cs"), newest.AddHours(-2));
        File.SetLastWriteTimeUtc(dir.Write("App/ViewModels/New.cs"), newest);
        File.SetLastWriteTimeUtc(dir.Write("App/Views/Main.axaml"), newest.AddHours(1));
        File.SetLastWriteTimeUtc(dir.Write("App/obj/Debug/App.AssemblyInfo.cs"), newest.AddHours(1));

        DateTime time = ProjectSources.GetLastCodeWriteTime(Path.Combine(dir.Path, "App"));

        Assert.Equal(newest, time);
    }

    [Fact]
    public void GetLastCodeWriteTime_GivesTheMinimumTimeForAProjectWithoutCodeFiles()
    {
        using TempDir dir = new();
        _ = dir.Write("App/Views/Main.axaml");

        Assert.Equal(DateTime.MinValue, ProjectSources.GetLastCodeWriteTime(Path.Combine(dir.Path, "App")));
    }
}
