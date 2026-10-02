using System.Text;

using AxView.Host.Catalog;
using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Renders all AXAML files of a project. Writes one PNG per file and a summary file.</summary>
/// <remarks>
/// The PNG of <c>Views/Main.axaml</c> is <c>Views/Main.png</c> in the output folder.
/// The summary file <c>summary.txt</c> has one line per file, with tab-separated fields:
/// <code>
/// ok    Views/Main.axaml
/// FAIL  Views/Broken.axaml    The error text, on one line
/// </code>
/// A failed file does not stop the run.
/// </remarks>
/// <param name="renderer">The renderer of the project.</param>
/// <param name="createTarget">Gives new styles of the target app. A window owns its styles, so each file needs new ones.</param>
/// <param name="projectDirectory">The folder of the project. The run renders its AXAML files.</param>
/// <param name="outputDirectory">The folder for the PNG files and the summary.</param>
/// <param name="settleDelay">The time that layout, bindings, and image loads get before each PNG.</param>
internal sealed class ProjectSnapshots(
    PreviewRenderer renderer,
    Func<TargetStyles> createTarget,
    string projectDirectory,
    string outputDirectory,
    TimeSpan settleDelay)
{
    private const string SummaryFileName = "summary.txt";

    /// <summary>Renders each file in turn.</summary>
    /// <returns><see cref="ExitReason.RenderFailed"/> when any file failed, else <see cref="ExitReason.Ok"/>.</returns>
    public async Task<ExitReason> RunAsync()
    {
        _ = Directory.CreateDirectory(outputDirectory);
        StringBuilder summary = new();
        ExitReason result = ExitReason.Ok;
        foreach (string file in AxamlCatalog.List(projectDirectory))
        {
            string relative = Path.GetRelativePath(projectDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
            string? error = await SnapshotAsync(file, relative);
            if (error is null)
            {
                _ = summary.AppendLine($"ok\t{relative}");
                continue;
            }

            result = ExitReason.RenderFailed;
            _ = summary.AppendLine($"FAIL\t{relative}\t{error.ReplaceLineEndings(" ")}");
        }

        string summaryText = summary.ToString();
        File.WriteAllText(Path.Combine(outputDirectory, SummaryFileName), summaryText);
        Console.Error.Write(summaryText);
        return result;
    }

    private async Task<string?> SnapshotAsync(string file, string relative)
    {
        string png = Path.Combine(outputDirectory, Path.ChangeExtension(relative, ".png"));
        _ = Directory.CreateDirectory(Path.GetDirectoryName(png)!);
        SnapshotWindow window = new(renderer, file, createTarget());
        try
        {
            window.Show();
            await Task.Delay(settleDelay);
            _ = window.Save(png);
            return window.Error;
        }
        catch (Exception ex)
        {
            // Layout runs user code. It can throw any exception. The run goes on with the next file.
            return ExceptionText.Describe(ex);
        }
        finally
        {
            window.Close();
        }
    }
}
