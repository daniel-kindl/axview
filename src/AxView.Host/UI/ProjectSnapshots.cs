using System.Globalization;
using System.Text;

using AxView.Host.Catalog;
using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Renders all AXAML files of a project and the control gallery. Writes one PNG for each, and a summary file.</summary>
/// <remarks>
/// The PNG of <c>Views/Main.axaml</c> is <c>Views/Main.png</c> in the output folder.
/// The PNG of the <see cref="ControlGallery"/> is <c>axview-gallery.png</c> in the output folder.
/// The summary file <c>summary.txt</c> has one line per file, and a last line for the gallery, with tab-separated fields:
/// <code>
/// ok    Views/Main.axaml
/// FAIL  Views/Broken.axaml    The error text, on one line
/// ok    (control gallery)
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
    private const string GalleryFileName = "axview-gallery.png";
    private const string GalleryName = "(control gallery)";

    /// <summary>Renders each file in turn, then the control gallery.</summary>
    /// <returns><see cref="ExitReason.RenderFailed"/> when any snapshot failed, else <see cref="ExitReason.Ok"/>.</returns>
    public async Task<ExitReason> RunAsync()
    {
        _ = Directory.CreateDirectory(outputDirectory);
        StringBuilder summary = new();
        bool isGood = true;
        foreach (string file in AxamlCatalog.List(projectDirectory))
        {
            string relative = Path.GetRelativePath(projectDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
            string png = Path.Combine(outputDirectory, Path.ChangeExtension(relative, ".png"));
            string? error = await SnapshotAsync(new SnapshotWindow(renderer, file, createTarget()), png);
            isGood &= AddSummaryLine(summary, relative, error);
        }

        TargetStyles galleryTarget = createTarget();
        SnapshotWindow galleryWindow = new(ControlGallery.Create(galleryTarget.Styles, galleryTarget.Resources), galleryTarget);
        string? galleryError = await SnapshotAsync(galleryWindow, Path.Combine(outputDirectory, GalleryFileName));
        isGood &= AddSummaryLine(summary, GalleryName, galleryError);

        string summaryText = summary.ToString();
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, SummaryFileName), summaryText);
        await Console.Error.WriteAsync(summaryText);
        return isGood ? ExitReason.Ok : ExitReason.RenderFailed;
    }

    // Gives true for a good snapshot.
    private static bool AddSummaryLine(StringBuilder summary, string name, string? error)
    {
        if (error is null)
        {
            _ = summary.AppendLine(CultureInfo.InvariantCulture, $"ok\t{name}");
            return true;
        }

        _ = summary.AppendLine(CultureInfo.InvariantCulture, $"FAIL\t{name}\t{error.ReplaceLineEndings(" ")}");
        return false;
    }

    private async Task<string?> SnapshotAsync(SnapshotWindow window, string png)
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(png)!);
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
