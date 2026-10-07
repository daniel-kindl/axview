extern alias host;

using System.Text.Json;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class BackgroundErrorsTests
{
    private const string Message = "The calling thread cannot access this object because a different thread owns it.";

    [Fact]
    public void Report_WritesTheErrorToTheErrorStreamAndGivesRenderFailed()
    {
        using StringWriter error = new();

        ExitReason reason = BackgroundErrors.Report(Options("Main.axaml"), new InvalidOperationException(Message), error);

        Assert.Equal(ExitReason.RenderFailed, reason);
        Assert.Contains($"InvalidOperationException: {Message}", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Report_SavesTheSnapshotReportWithTheError()
    {
        using TempDir dir = new();
        string reportPath = Path.Combine(dir.Path, "report.json");
        HostOptions options = Options(Path.Combine(dir.Path, "Main.axaml")) with
        {
            SnapshotPath = Path.Combine(dir.Path, "main.png"),
            ReportPath = reportPath,
        };

        _ = BackgroundErrors.Report(options, new InvalidOperationException(Message), TextWriter.Null);

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(reportPath));
        JsonElement report = document.RootElement;
        Assert.Equal(options.AxamlPath, report.GetProperty("File").GetString());
        Assert.Equal("RenderFailed", report.GetProperty("ExitReason").GetString());
        Assert.Equal($"InvalidOperationException: {Message}", report.GetProperty("Error").GetString());
    }

    private static HostOptions Options(string axamlPath) =>
        new(axamlPath, Path.GetDirectoryName(axamlPath) ?? string.Empty, "Ui", null, "loader.dll", null, null);
}
