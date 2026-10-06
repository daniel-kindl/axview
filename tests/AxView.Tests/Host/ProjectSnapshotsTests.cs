extern alias host;

using Avalonia.Headless.XUnit;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class ProjectSnapshotsTests
{
    private const string AvaloniaXmlns = "xmlns=\"https://github.com/avaloniaui\"";

    [AvaloniaFact]
    public async Task RunAsync_WritesASummaryLinePerFileAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        _ = dir.Write("Views/Sub/World.axaml", $"""<TextBlock {AvaloniaXmlns} Text="World" />""");
        string output = Path.Combine(dir.Path, "out");

        ExitReason reason = await Snapshots(dir, output).RunAsync();

        Assert.Equal(ExitReason.Ok, reason);
        Assert.Equal(
            ["ok\tViews/Hello.axaml", "ok\tViews/Sub/World.axaml", "ok\t(control gallery)"],
            File.ReadAllLines(Path.Combine(output, "summary.txt")));
    }

    [AvaloniaFact]
    public async Task RunAsync_BrokenFile_FailsButStillRendersTheOtherFilesAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Views/Broken.axaml", "<TextBlock");
        _ = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string output = Path.Combine(dir.Path, "out");

        ExitReason reason = await Snapshots(dir, output).RunAsync();

        Assert.Equal(ExitReason.RenderFailed, reason);
        string[] summary = File.ReadAllLines(Path.Combine(output, "summary.txt"));
        Assert.StartsWith("FAIL\tViews/Broken.axaml\t", summary[0], StringComparison.Ordinal);
        Assert.Equal("ok\tViews/Hello.axaml", summary[1]);
    }

    [AvaloniaFact]
    public async Task RunAsync_FileThatThrowsInLayout_FailsButStillRendersTheOtherFilesAsync()
    {
        using TempDir dir = new();
        _ = dir.Write(
            "Views/Throws.axaml",
            $"""<local:FixtureThrowingControl {AvaloniaXmlns} xmlns:local="using:AxView.Tests.Fixtures" />""");
        _ = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string output = Path.Combine(dir.Path, "out");

        ExitReason reason = await Snapshots(dir, output).RunAsync();

        Assert.Equal(ExitReason.RenderFailed, reason);
        string[] summary = File.ReadAllLines(Path.Combine(output, "summary.txt"));
        Assert.Contains("Layout failed.", summary[1], StringComparison.Ordinal);
        Assert.StartsWith("FAIL	Views/Throws.axaml	", summary[1], StringComparison.Ordinal);
        Assert.Equal("ok	Views/Hello.axaml", summary[0]);
    }

    [AvaloniaFact]
    public async Task RunAsync_WritesTheControlGalleryAsync()
    {
        using TempDir dir = new();
        _ = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string output = Path.Combine(dir.Path, "out");

        _ = await Snapshots(dir, output).RunAsync();

        Assert.True(File.Exists(Path.Combine(output, "axview-gallery.png")));
    }

    private static ProjectSnapshots Snapshots(TempDir dir, string output) => new(
        new PreviewRenderer(typeof(ProjectSnapshotsTests).Assembly, dir.Path, DesignServices.None),
        () => TargetStyles.None,
        dir.Path,
        output,
        TimeSpan.Zero);
}
