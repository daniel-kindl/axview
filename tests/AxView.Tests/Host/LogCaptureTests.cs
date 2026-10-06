extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class LogCaptureTests
{
    [AvaloniaFact]
    public void Start_CatchesAWarningWithTheFormattedMessage()
    {
        TextBlock source = new() { Name = "lblTitle" };
        using LogCapture capture = LogCapture.Start();

        Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(source, "No property {Property} on {Type}.", "Missing", "MainViewModel");

        LogEntry entry = Assert.Single(capture.Entries);
        Assert.Equal("Warning", entry.Level);
        Assert.Equal(LogArea.Binding, entry.Area);
        Assert.Equal("TextBlock#lblTitle", entry.Source);
        Assert.Equal("No property Missing on MainViewModel.", entry.Message);
    }

    [AvaloniaFact]
    public void Start_GivesTheTypeAsTheSourceOfAnElementWithoutAName()
    {
        using LogCapture capture = LogCapture.Start();

        Logger.TryGet(LogEventLevel.Error, LogArea.Layout)?.Log(new Border(), "Failed.");

        Assert.Equal("Border", Assert.Single(capture.Entries).Source);
    }

    [AvaloniaFact]
    public void Start_IgnoresInformation()
    {
        using LogCapture capture = LogCapture.Start();

        Logger.TryGet(LogEventLevel.Information, LogArea.Binding)?.Log(null, "Some information.");

        Assert.Empty(capture.Entries);
    }

    [AvaloniaFact]
    public void Dispose_PutsBackThePreviousSink()
    {
        ILogSink? previous = Logger.Sink;

        LogCapture.Start().Dispose();

        Assert.Same(previous, Logger.Sink);
    }
}
