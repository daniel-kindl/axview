using AxView.Resolution;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class PreviewTargetResolverTests
{
    [Fact]
    public void Resolve_FindsTheProjectTheAppOutputAndTheLoader()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Ui/Ui.csproj", "<Project><PropertyGroup><AssemblyName>My.Ui</AssemblyName></PropertyGroup></Project>");
        string axaml = dir.Write("src/Ui/Views/Main.axaml");
        string output = AppFixture.WriteAppOutput(dir, "src/Desktop/bin/Debug/net9.0", "Desktop", "My.Ui");
        FakePackageSource loaders = new();

        PreviewTarget target = new PreviewTargetResolver(loaders).Resolve(axaml, outputDir: null);

        Assert.Equal(axaml, target.AxamlPath);
        Assert.Equal(Path.Combine(dir.Path, "src", "Ui"), target.ProjectDirectory);
        Assert.Equal("My.Ui", target.AssemblyName);
        Assert.Equal(output, target.Output.Directory);
        Assert.Equal("Desktop", target.Output.AppName);
        Assert.Equal(["Avalonia.Markup.Xaml.Loader/11.3.22", "Avalonia.Themes.Simple/11.3.22"], loaders.Requests);
        Assert.Equal(FakePackageSource.LoaderPath, target.LoaderPath);
    }

    [Fact]
    public void Resolve_AppWithoutATheme_GivesTheSimpleThemeOfTheAppVersionToTheHost()
    {
        using TempDir dir = new();
        string axaml = AppFixture.WriteProject(dir, "Desktop");

        PreviewTarget target = new PreviewTargetResolver(new FakePackageSource()).Resolve(axaml, outputDir: null);

        Assert.Equal(FakePackageSource.SimpleThemePath, target.ChromeThemePath);
        Assert.Equal(FakePackageSource.SimpleThemePath, target.ToHostOptions(snapshotPath: null, handOffFile: null).ChromeThemePath);
    }

    [Theory]
    [InlineData("Avalonia.Themes.Fluent")]
    [InlineData("Avalonia.Themes.Simple")]
    public void Resolve_AppWithATheme_GivesNoChromeTheme(string themePackage)
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Desktop/Desktop.csproj", "<Project />");
        string axaml = dir.Write("src/Desktop/Views/Main.axaml");
        string deps = $$"""{ "libraries": { "Avalonia/11.3.22": {}, "{{themePackage}}/11.3.22": {} } }""";
        _ = AppFixture.WriteAppOutput(dir, "src/Desktop/bin/Debug/net9.0", "Desktop", "Desktop", deps);
        FakePackageSource packages = new();

        PreviewTarget target = new PreviewTargetResolver(packages).Resolve(axaml, outputDir: null);

        Assert.Null(target.ChromeThemePath);
        Assert.Equal(["Avalonia.Markup.Xaml.Loader/11.3.22"], packages.Requests);
    }

    [Fact]
    public void Resolve_LibraryView_FindsTheFolderOfTheAppProject()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Ui/Ui.csproj", "<Project />");
        string axaml = dir.Write("src/Ui/Views/Main.axaml");
        _ = dir.Write("src/Desktop/Desktop.csproj", "<Project />");
        _ = AppFixture.WriteAppOutput(dir, "src/Desktop/bin/Debug/net9.0", "Desktop", "Ui");

        PreviewTarget target = new PreviewTargetResolver(new FakePackageSource()).Resolve(axaml, outputDir: null);

        string appProject = Path.Combine(dir.Path, "src", "Desktop");
        Assert.Equal(appProject, target.AppProjectDirectory);
        Assert.Equal(appProject, target.ToHostOptions(snapshotPath: null, handOffFile: null).AppProjectDirectory);
    }

    [Fact]
    public void Resolve_GivesTheReferencedProjectsOfTheViewProjectAndTheAppProject()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Ui/Ui.csproj", "<Project><ItemGroup><ProjectReference Include=\"../Core/Core.csproj\" /></ItemGroup></Project>");
        string axaml = dir.Write("src/Ui/Views/Main.axaml");
        _ = dir.Write("src/Core/Core.csproj", "<Project />");
        _ = dir.Write("src/Desktop/Desktop.csproj", "<Project><ItemGroup><ProjectReference Include=\"../Theme/Theme.csproj\" /></ItemGroup></Project>");
        _ = dir.Write("src/Theme/Theme.csproj", "<Project />");
        _ = AppFixture.WriteAppOutput(dir, "src/Desktop/bin/Debug/net9.0", "Desktop", "Ui");

        PreviewTarget target = new PreviewTargetResolver(new FakePackageSource()).Resolve(axaml, outputDir: null);

        string[] expected = [Path.Combine(dir.Path, "src", "Core"), Path.Combine(dir.Path, "src", "Theme")];
        Assert.Equal(expected, target.ReferencedProjectDirectories);
        Assert.Equal(expected, target.ToHostOptions(snapshotPath: null, handOffFile: null).ReferencedProjectDirectories);
    }

    [Fact]
    public void Resolve_AppView_GivesNoAppProject()
    {
        using TempDir dir = new();
        string axaml = AppFixture.WriteProject(dir, "Desktop");

        PreviewTarget target = new PreviewTargetResolver(new FakePackageSource()).Resolve(axaml, outputDir: null);

        Assert.Null(target.AppProjectDirectory);
    }

    [Fact]
    public void Resolve_LibraryViewWithoutAnAppProject_GivesNoAppProject()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Ui/Ui.csproj", "<Project />");
        string axaml = dir.Write("src/Ui/Views/Main.axaml");
        _ = AppFixture.WriteAppOutput(dir, "src/Desktop/bin/Debug/net9.0", "Desktop", "Ui");

        PreviewTarget target = new PreviewTargetResolver(new FakePackageSource()).Resolve(axaml, outputDir: null);

        Assert.Null(target.AppProjectDirectory);
    }

    [Fact]
    public void Resolve_UsesTheGivenOutputDirectory()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Ui/Ui.csproj", "<Project />");
        string axaml = dir.Write("src/Ui/Views/Main.axaml");
        string given = AppFixture.WriteAppOutput(dir, "src/A/bin/Debug/net9.0", "A", "Ui");
        string recent = AppFixture.WriteAppOutput(dir, "src/B/bin/Debug/net9.0", "B", "Ui");
        File.SetLastWriteTimeUtc(Path.Combine(given, "Ui.dll"), DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(Path.Combine(recent, "Ui.dll"), DateTime.UtcNow);

        PreviewTarget target = new PreviewTargetResolver(new FakePackageSource()).Resolve(axaml, given);

        Assert.Equal(given, target.Output.Directory);
    }

    [Fact]
    public void Resolve_RejectsAMissingFile()
    {
        using TempDir dir = new();
        string missing = Path.Combine(dir.Path, "Views", "Main.axaml");

        PreviewSetupException ex = Assert.Throws<PreviewSetupException>(
            () => new PreviewTargetResolver(new FakePackageSource()).Resolve(missing, outputDir: null));

        Assert.Contains(missing, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_RejectsAnOutputWithoutAvalonia()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Ui/Ui.csproj", "<Project />");
        string axaml = dir.Write("src/Ui/Views/Main.axaml");
        _ = AppFixture.WriteAppOutput(dir, "src/Desktop/bin/Debug/net9.0", "Desktop", "Ui", deps: "{}");
        FakePackageSource loaders = new();

        _ = Assert.Throws<PreviewSetupException>(() => new PreviewTargetResolver(loaders).Resolve(axaml, outputDir: null));
        Assert.Empty(loaders.Requests);
    }
}
