using AxView.Protocol;

namespace AxView.Host.Rendering;

/// <summary>Reports an exception that escapes a background thread.</summary>
/// <remarks>
/// .NET always stops the process after such an exception. Thus the host cannot show it in the error panel.
/// User code can cause it, for example a ReactiveUI command that raises CanExecuteChanged on the thread pool.
/// </remarks>
internal static class BackgroundErrors
{
    /// <summary>Writes the error, and saves the snapshot report if the options have a report path.</summary>
    /// <param name="options">The options of the host.</param>
    /// <param name="ex">The exception of the background thread.</param>
    /// <param name="error">Receives the error line.</param>
    /// <returns>The exit reason for the host.</returns>
    public static ExitReason Report(HostOptions options, Exception ex, TextWriter error)
    {
        string text = ExceptionText.Describe(ex);
        error.WriteLine($"axview: error on a background thread: {text}");
        if (options.ReportPath is string reportPath)
        {
            SnapshotReport.Create(options.AxamlPath, ExitReason.RenderFailed, TimeSpan.Zero, text, null, []).Save(reportPath);
        }

        return ExitReason.RenderFailed;
    }
}
