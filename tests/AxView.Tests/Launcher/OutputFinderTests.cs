using AxView.Resolution;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class OutputFinderTests
{
    [Fact]
    public void Find_ReturnsTheExecutableOutputThatContainsTheAssembly()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string project = dir.Write("src/Ui/Ui.csproj", "<Project />");
        _ = dir.Write("src/Ui/bin/Debug/net9.0/Ui.dll");
        _ = dir.Write("src/Ui/bin/Debug/net9.0/Ui.deps.json", "{}");
        string expected = WriteExecutableOutput(dir, "src/Desktop/bin/Debug/net9.0", "Desktop", "Ui");

        AppOutput output = OutputFinder.Find(project, "Ui");

        Assert.Equal(expected, output.Directory);
        Assert.Equal(Path.Combine(expected, "Desktop.deps.json"), output.DepsFile);
        Assert.Equal(Path.Combine(expected, "Desktop.runtimeconfig.json"), output.RuntimeConfig);
    }

    [Fact]
    public void Find_PrefersTheMostRecentlyBuiltOutput()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string project = dir.Write("src/Ui/Ui.csproj", "<Project />");
        string old = WriteExecutableOutput(dir, "src/A/bin/Debug/net9.0", "A", "Ui");
        string recent = WriteExecutableOutput(dir, "src/B/bin/Debug/net9.0", "B", "Ui");
        File.SetLastWriteTimeUtc(Path.Combine(old, "Ui.dll"), DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(Path.Combine(recent, "Ui.dll"), DateTime.UtcNow);

        AppOutput output = OutputFinder.Find(project, "Ui");

        Assert.Equal(recent, output.Directory);
    }

    [Fact]
    public void Find_IgnoresOutputsWithoutAvaloniaDesktop()
    {
        using TempDir dir = new();
        _ = dir.Write("Repo.sln");
        string project = dir.Write("src/Ui/Ui.csproj", "<Project />");
        _ = dir.Write("src/Web/bin/Debug/net9.0/Ui.dll");
        _ = dir.Write("src/Web/bin/Debug/net9.0/Web.runtimeconfig.json", "{}");
        _ = dir.Write("src/Web/bin/Debug/net9.0/Web.deps.json", "{}");

        _ = Assert.Throws<PreviewSetupException>(() => OutputFinder.Find(project, "Ui"));
    }

    [Fact]
    public void FromDirectory_UsesTheGivenOutput()
    {
        using TempDir dir = new();
        string output = WriteExecutableOutput(dir, "out", "Desktop", "Ui");

        AppOutput found = OutputFinder.FromDirectory(output, "Ui");

        Assert.Equal(output, found.Directory);
    }

    private static string WriteExecutableOutput(TempDir dir, string relativeDir, string exeName, string assemblyName)
    {
        _ = dir.Write($"{relativeDir}/{assemblyName}.dll");
        _ = dir.Write($"{relativeDir}/Avalonia.Desktop.dll");
        _ = dir.Write($"{relativeDir}/{exeName}.dll");
        _ = dir.Write($"{relativeDir}/{exeName}.deps.json", "{}");
        _ = dir.Write($"{relativeDir}/{exeName}.runtimeconfig.json", "{}");
        return Path.Combine(dir.Path, relativeDir.Replace('/', Path.DirectorySeparatorChar));
    }
}
