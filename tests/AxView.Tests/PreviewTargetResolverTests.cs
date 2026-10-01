namespace AxView.Tests;

public sealed class PreviewTargetResolverTests
{
    private const string AvaloniaDeps = """{ "libraries": { "Avalonia/11.3.22": { "type": "package" } } }""";

    [Fact]
    public void Resolve_FindsTheProjectTheAppOutputAndTheLoader()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Ui/Ui.csproj", "<Project><PropertyGroup><AssemblyName>My.Ui</AssemblyName></PropertyGroup></Project>");
        string axaml = dir.Write("src/Ui/Views/Main.axaml");
        string output = WriteAppOutput(dir, "src/Desktop/bin/Debug/net9.0", "Desktop", "My.Ui");
        FakeLoaderSource loaders = new();

        PreviewTarget target = new PreviewTargetResolver(loaders).Resolve(axaml, outputDir: null);

        Assert.Equal(axaml, target.AxamlPath);
        Assert.Equal(Path.Combine(dir.Path, "src", "Ui"), target.ProjectDirectory);
        Assert.Equal("My.Ui", target.AssemblyName);
        Assert.Equal(output, target.Output.Directory);
        Assert.Equal("Desktop", target.Output.AppName);
        Assert.Equal(["11.3.22"], loaders.RequestedVersions);
        Assert.Equal(FakeLoaderSource.LoaderPath, target.LoaderPath);
    }

    [Fact]
    public void Resolve_UsesTheGivenOutputDirectory()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Ui/Ui.csproj", "<Project />");
        string axaml = dir.Write("src/Ui/Views/Main.axaml");
        string given = WriteAppOutput(dir, "src/A/bin/Debug/net9.0", "A", "Ui");
        string recent = WriteAppOutput(dir, "src/B/bin/Debug/net9.0", "B", "Ui");
        File.SetLastWriteTimeUtc(Path.Combine(given, "Ui.dll"), DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(Path.Combine(recent, "Ui.dll"), DateTime.UtcNow);

        PreviewTarget target = new PreviewTargetResolver(new FakeLoaderSource()).Resolve(axaml, given);

        Assert.Equal(given, target.Output.Directory);
    }

    [Fact]
    public void Resolve_RejectsAMissingFile()
    {
        using TempDir dir = new();
        string missing = Path.Combine(dir.Path, "Views", "Main.axaml");

        PreviewSetupException ex = Assert.Throws<PreviewSetupException>(
            () => new PreviewTargetResolver(new FakeLoaderSource()).Resolve(missing, outputDir: null));

        Assert.Contains(missing, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_RejectsAnOutputWithoutAvalonia()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        _ = dir.Write("src/Ui/Ui.csproj", "<Project />");
        string axaml = dir.Write("src/Ui/Views/Main.axaml");
        _ = WriteAppOutput(dir, "src/Desktop/bin/Debug/net9.0", "Desktop", "Ui", deps: "{}");
        FakeLoaderSource loaders = new();

        _ = Assert.Throws<PreviewSetupException>(() => new PreviewTargetResolver(loaders).Resolve(axaml, outputDir: null));
        Assert.Empty(loaders.RequestedVersions);
    }

    private static string WriteAppOutput(
        TempDir dir, string relativeDir, string appName, string assemblyName, string deps = AvaloniaDeps)
    {
        _ = dir.Write($"{relativeDir}/{assemblyName}.dll");
        _ = dir.Write($"{relativeDir}/Avalonia.Desktop.dll");
        _ = dir.Write($"{relativeDir}/{appName}.dll");
        _ = dir.Write($"{relativeDir}/{appName}.deps.json", deps);
        _ = dir.Write($"{relativeDir}/{appName}.runtimeconfig.json", "{}");
        return Path.Combine(dir.Path, relativeDir.Replace('/', Path.DirectorySeparatorChar));
    }
}
