using AxView.Hosting;
using AxView.Resolution;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class OutputShadowTests
{
    [Fact]
    public void Create_CopiesTheOutputAndGivesTheCopy()
    {
        using TempDir dir = new();
        AppOutput source = WriteOutput(dir);
        _ = dir.Write("out/runtimes/win-x64/native/libSkiaSharp.dll", "native");
        string shadowRoot = Path.Combine(dir.Path, "shadow");

        using OutputShadow shadow = OutputShadow.Create(source, shadowRoot);

        Assert.StartsWith(shadowRoot, shadow.Output.Directory, StringComparison.Ordinal);
        Assert.Equal("Desktop", shadow.Output.AppName);
        Assert.True(File.Exists(shadow.Output.DepsFile));
        Assert.Equal("ui", File.ReadAllText(Path.Combine(shadow.Output.Directory, "Ui.dll")));
        Assert.Equal("native", File.ReadAllText(Path.Combine(shadow.Output.Directory, "runtimes", "win-x64", "native", "libSkiaSharp.dll")));
    }

    [Fact]
    public void Create_CopiesOnlyTheChangedFilesIntoAFreeShadow()
    {
        using TempDir dir = new();
        AppOutput source = WriteOutput(dir);
        string shadowRoot = Path.Combine(dir.Path, "shadow");
        string shadowDirectory;
        using (OutputShadow first = OutputShadow.Create(source, shadowRoot))
        {
            shadowDirectory = first.Output.Directory;
        }

        // A marker of the same size and time in the copy shows that the second sync did not copy the file.
        string unchanged = Path.Combine(shadowDirectory, "Avalonia.Desktop.dll");
        DateTime unchangedTime = File.GetLastWriteTimeUtc(unchanged);
        File.WriteAllText(unchanged, "DESKTOP");
        File.SetLastWriteTimeUtc(unchanged, unchangedTime);
        string changed = Path.Combine(source.Directory, "Ui.dll");
        File.WriteAllText(changed, "ui version 2");
        File.SetLastWriteTimeUtc(changed, DateTime.UtcNow.AddMinutes(1));

        using OutputShadow second = OutputShadow.Create(source, shadowRoot);

        Assert.StartsWith(shadowRoot, shadowDirectory, StringComparison.Ordinal);
        Assert.Equal(shadowDirectory, second.Output.Directory);
        Assert.Equal("ui version 2", File.ReadAllText(Path.Combine(shadowDirectory, "Ui.dll")));
        Assert.Equal("DESKTOP", File.ReadAllText(unchanged));
    }

    [Fact]
    public void Create_GivesAnotherShadowWhileTheFirstIsInUse()
    {
        using TempDir dir = new();
        AppOutput source = WriteOutput(dir);
        string shadowRoot = Path.Combine(dir.Path, "shadow");

        using OutputShadow first = OutputShadow.Create(source, shadowRoot);
        using OutputShadow second = OutputShadow.Create(source, shadowRoot);

        Assert.NotEqual(first.Output.Directory, second.Output.Directory);
        Assert.True(File.Exists(second.Output.RuntimeConfig));
    }

    private static AppOutput WriteOutput(TempDir dir)
    {
        _ = dir.Write("out/Ui.dll", "ui");
        _ = dir.Write("out/Avalonia.Desktop.dll", "desktop");
        _ = dir.Write("out/Desktop.deps.json", "{}");
        return AppOutput.FromRuntimeConfig(dir.Write("out/Desktop.runtimeconfig.json", "{}"));
    }
}
