extern alias host;

using System.Text.Json;

using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class SnapshotRunnerTests
{
    private const string AvaloniaXmlns = "xmlns=\"https://github.com/avaloniaui\"";

    [AvaloniaFact]
    public async Task RunAsync_Snapshot_WritesThePngOfTheFileAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string png = Path.Combine(dir.Path, "hello.png");

        ExitReason reason = await Runner(dir).RunAsync(Options(dir, file) with { SnapshotPath = png });

        Assert.Equal(ExitReason.Ok, reason);
        Assert.True(File.Exists(png));
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotWithASize_WritesAPngOfThatSizeAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string png = Path.Combine(dir.Path, "hello.png");
        HostOptions options =
            Options(dir, file) with { SnapshotPath = png, Size = PreviewSize.Parse("500x300"), Theme = PreviewTheme.Dark };

        ExitReason reason = await Runner(dir).RunAsync(options);

        Assert.Equal(ExitReason.Ok, reason);
        using Bitmap bitmap = new(png);
        Assert.Equal(new PixelSize(500, 300), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotAllWithASize_WritesPngsOfThatSizeAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string output = Path.Combine(dir.Path, "out");
        HostOptions options = Options(dir, file) with { SnapshotAllDirectory = output, Size = PreviewSize.Parse("500x300") };

        _ = await Runner(dir).RunAsync(options);

        using Bitmap bitmap = new(Path.Combine(output, "Views", "Hello.png"));
        Assert.Equal(new PixelSize(500, 300), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotWithATree_WritesTheTreeOfTheViewAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string tree = Path.Combine(dir.Path, "out", "tree.json");
        HostOptions options = Options(dir, file) with { SnapshotPath = Path.Combine(dir.Path, "hello.png"), TreePath = tree };

        ExitReason reason = await Runner(dir).RunAsync(options);

        Assert.Equal(ExitReason.Ok, reason);
        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(tree));
        Assert.Equal("TextBlock", document.RootElement.GetProperty("Type").GetString());
        Assert.Equal("Hello", document.RootElement.GetProperty("Text").GetString());
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotOfABrokenFileWithATree_WritesNoTreeAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Broken.axaml", "<TextBlock");
        string tree = Path.Combine(dir.Path, "tree.json");
        HostOptions options = Options(dir, file) with { SnapshotPath = Path.Combine(dir.Path, "broken.png"), TreePath = tree };

        ExitReason reason = await Runner(dir).RunAsync(options);

        Assert.Equal(ExitReason.RenderFailed, reason);
        Assert.False(File.Exists(tree));
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotWithAReport_WritesTheExitReasonAndTheLoadTimeAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");

        JsonElement report = await SaveReportAsync(dir, file, ExitReason.Ok);

        Assert.Equal(file, report.GetProperty("File").GetString());
        Assert.Equal("Ok", report.GetProperty("ExitReason").GetString());
        Assert.True(report.GetProperty("LoadMilliseconds").GetDouble() >= 0);
        Assert.False(report.TryGetProperty("Error", out _));
        Assert.Empty(report.GetProperty("Log").EnumerateArray());
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotOfABrokenFileWithAReport_WritesTheErrorAndItsLineAsync()
    {
        using TempDir dir = new();
        string file = dir.Write(
            "Views/Broken.axaml",
            $"""
            <TextBlock {AvaloniaXmlns}>
              <Broken
            """);

        JsonElement report = await SaveReportAsync(dir, file, ExitReason.RenderFailed);

        Assert.Equal("RenderFailed", report.GetProperty("ExitReason").GetString());
        Assert.False(string.IsNullOrEmpty(report.GetProperty("Error").GetString()));
        Assert.Equal(2, report.GetProperty("Line").GetInt32());
        Assert.True(report.GetProperty("Column").GetInt32() > 0);
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotWithABindingError_WritesTheBindingErrorToTheReportAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $$"""<TextBlock {{AvaloniaXmlns}} Text="{Binding Missing}" />""");

        JsonElement report = await SaveReportAsync(dir, file, ExitReason.Ok, typeof(FixtureViewModel).FullName);

        JsonElement entry = Assert.Single(
            report.GetProperty("Log").EnumerateArray(), item => item.GetProperty("Area").GetString() == "Binding");
        Assert.Contains("Missing", entry.GetProperty("Message").GetString(), StringComparison.Ordinal);
        Assert.Equal("TextBlock", entry.GetProperty("Source").GetString());
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotWithAnUnknownViewModelAndAReport_WritesBadArgumentsAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");

        JsonElement report = await SaveReportAsync(dir, file, ExitReason.BadArguments, "Unknown.ViewModel");

        Assert.Equal("BadArguments", report.GetProperty("ExitReason").GetString());
        Assert.Contains("Unknown.ViewModel", report.GetProperty("Error").GetString(), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotOfABrokenFile_GivesRenderFailedAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Broken.axaml", "<TextBlock");
        string png = Path.Combine(dir.Path, "broken.png");

        ExitReason reason = await Runner(dir).RunAsync(Options(dir, file) with { SnapshotPath = png });

        Assert.Equal(ExitReason.RenderFailed, reason);
        Assert.True(File.Exists(png));
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotWithAViewModel_GivesOkAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string png = Path.Combine(dir.Path, "hello.png");
        HostOptions options = Options(dir, file) with { SnapshotPath = png, ViewModelName = typeof(FixtureViewModel).FullName };

        ExitReason reason = await Runner(dir).RunAsync(options);

        Assert.Equal(ExitReason.Ok, reason);
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotWithAnUnknownViewModel_GivesBadArgumentsAndNoPngAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string png = Path.Combine(dir.Path, "hello.png");
        HostOptions options = Options(dir, file) with { SnapshotPath = png, ViewModelName = "Unknown.ViewModel" };

        ExitReason reason = await Runner(dir).RunAsync(options);

        Assert.Equal(ExitReason.BadArguments, reason);
        Assert.False(File.Exists(png));
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotAll_WritesASummaryLinePerFileAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        _ = dir.Write("Views/Sub/World.axaml", $"""<TextBlock {AvaloniaXmlns} Text="World" />""");
        string output = Path.Combine(dir.Path, "out");

        ExitReason reason = await Runner(dir).RunAsync(Options(dir, file) with { SnapshotAllDirectory = output });

        Assert.Equal(ExitReason.Ok, reason);
        Assert.Equal(
            ["ok\tViews/Hello.axaml", "ok\tViews/Sub/World.axaml", "ok\t(control gallery)"],
            await File.ReadAllLinesAsync(Path.Combine(output, "summary.txt")));
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotAllWithABrokenFile_FailsButStillRendersTheOtherFilesAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Broken.axaml", "<TextBlock");
        _ = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string output = Path.Combine(dir.Path, "out");

        ExitReason reason = await Runner(dir).RunAsync(Options(dir, file) with { SnapshotAllDirectory = output });

        Assert.Equal(ExitReason.RenderFailed, reason);
        string[] summary = await File.ReadAllLinesAsync(Path.Combine(output, "summary.txt"));
        Assert.StartsWith("FAIL\tViews/Broken.axaml\t", summary[0], StringComparison.Ordinal);
        Assert.Equal("ok\tViews/Hello.axaml", summary[1]);
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotAllWithAFileThatThrowsInLayout_FailsButStillRendersTheOtherFilesAsync()
    {
        using TempDir dir = new();
        string file = dir.Write(
            "Views/Throws.axaml",
            $"""<local:FixtureThrowingControl {AvaloniaXmlns} xmlns:local="using:AxView.Tests.Fixtures" />""");
        _ = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string output = Path.Combine(dir.Path, "out");

        ExitReason reason = await Runner(dir).RunAsync(Options(dir, file) with { SnapshotAllDirectory = output });

        Assert.Equal(ExitReason.RenderFailed, reason);
        string[] summary = await File.ReadAllLinesAsync(Path.Combine(output, "summary.txt"));
        Assert.Contains("Layout failed.", summary[1], StringComparison.Ordinal);
        Assert.StartsWith("FAIL\tViews/Throws.axaml\t", summary[1], StringComparison.Ordinal);
        Assert.Equal("ok\tViews/Hello.axaml", summary[0]);
    }

    [AvaloniaFact]
    public async Task RunAsync_SnapshotAll_WritesTheControlGalleryAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string output = Path.Combine(dir.Path, "out");

        _ = await Runner(dir).RunAsync(Options(dir, file) with { SnapshotAllDirectory = output });

        Assert.True(File.Exists(Path.Combine(output, "axview-gallery.png")));
    }

    [AvaloniaFact]
    public async Task RunAsync_Preview_ThrowsAsync()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");

        _ = await Assert.ThrowsAsync<ArgumentException>(() => Runner(dir).RunAsync(Options(dir, file)));
    }

    private static async Task<JsonElement> SaveReportAsync(TempDir dir, string file, ExitReason expected, string? viewModelName = null)
    {
        string reportPath = Path.Combine(dir.Path, "out", "report.json");
        HostOptions options = Options(dir, file) with
        {
            SnapshotPath = Path.Combine(dir.Path, "shot.png"),
            ViewModelName = viewModelName,
            ReportPath = reportPath,
        };

        ExitReason reason = await Runner(dir).RunAsync(options);

        Assert.Equal(expected, reason);
        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        return document.RootElement.Clone();
    }

    private static SnapshotRunner Runner(TempDir dir) =>
        new(ProjectSet.Load(Options(dir, string.Empty)), DesignServices.None, () => TargetStyles.None, TimeSpan.Zero);

    private static HostOptions Options(TempDir dir, string file) =>
        new(file, dir.Path, "AxView.Tests", null, "Loader.dll", null, null);
}
