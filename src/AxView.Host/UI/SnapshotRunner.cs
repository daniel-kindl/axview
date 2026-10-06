using System.Globalization;
using System.Text;

using AxView.Host.Catalog;
using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Runs the snapshot modes of the host. Writes PNG files without user interaction, and gives the exit reason.</summary>
/// <remarks>
/// <para>
/// <see cref="HostMode.Snapshot"/> writes one PNG of the file of the options, with the ViewModel of the options.
/// An unknown ViewModel gives <see cref="ExitReason.BadArguments"/> and no PNG.
/// </para>
/// <para>
/// <see cref="HostMode.SnapshotAll"/> writes one PNG for each AXAML file of the project and for the control gallery,
/// and a summary file. The PNG of <c>Views/Main.axaml</c> is <c>Views/Main.png</c> in the output folder.
/// The PNG of the <see cref="ControlGallery"/> is <c>axview-gallery.png</c> in the output folder.
/// The summary file <c>summary.txt</c> has one line per file, and a last line for the gallery, with tab-separated fields:
/// <code>
/// ok    Views/Main.axaml
/// FAIL  Views/Broken.axaml    The error text, on one line
/// ok    (control gallery)
/// </code>
/// A failed file does not stop the run.
/// </para>
/// Each PNG has the size and the theme of the options, if they are set. The control gallery ignores the size.
/// Each PNG waits for the settle time after the window shows. The errors go to the standard error stream.
/// </remarks>
/// <param name="projects">The projects of the preview.</param>
/// <param name="services">The design services that create the ViewModels.</param>
/// <param name="createTarget">Gives new styles of the target app. A window owns its styles, so each PNG needs new ones.</param>
/// <param name="settleDelay">The time that layout, bindings, and image loads get before each PNG.</param>
internal sealed class SnapshotRunner(ProjectSet projects, DesignServices services, Func<TargetStyles> createTarget, TimeSpan settleDelay)
{
    private const string SummaryFileName = "summary.txt";
    private const string GalleryFileName = "axview-gallery.png";
    private const string GalleryName = "(control gallery)";

    private readonly PreviewRenderer renderer = projects.CreateViewRenderer(services);

    /// <summary>Runs the snapshot mode of the options.</summary>
    /// <param name="options">The options of the host. Their mode is <see cref="HostMode.Snapshot"/> or <see cref="HostMode.SnapshotAll"/>.</param>
    /// <returns><see cref="ExitReason.Ok"/> when all snapshots are good.</returns>
    /// <exception cref="ArgumentException">The options select no snapshot mode.</exception>
    public Task<ExitReason> RunAsync(HostOptions options) =>
        options switch
        {
            { SnapshotAllDirectory: string directory } => SaveProjectAsync(options, directory),
            { SnapshotPath: string png } => SaveFileAsync(options, png),
            _ => throw new ArgumentException("The options select no snapshot mode.", nameof(options)),
        };

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

    private async Task<ExitReason> SaveFileAsync(HostOptions options, string png)
    {
        string? viewModelName = options.ViewModelName;
        Type? viewModelType = null;
        if (viewModelName is not null)
        {
            viewModelType = ViewModelCatalog.Find(projects.ViewAssembly, services, viewModelName);
            if (viewModelType is null)
            {
                await Console.Error.WriteLineAsync($"axview: error: No ViewModel '{viewModelName}' that the design services can create.");
                return ExitReason.BadArguments;
            }
        }

        SnapshotWindow window = new(renderer, options.AxamlPath, createTarget(), viewModelType);
        string? error = await SaveAsync(window, png, options);
        if (error is null)
        {
            return ExitReason.Ok;
        }

        await Console.Error.WriteLineAsync($"axview: error: {error}");
        return ExitReason.RenderFailed;
    }

    private async Task<ExitReason> SaveProjectAsync(HostOptions options, string outputDirectory)
    {
        string projectDirectory = options.ProjectDirectory;
        _ = Directory.CreateDirectory(outputDirectory);
        StringBuilder summary = new();
        bool isGood = true;
        foreach (string file in AxamlCatalog.List(projectDirectory))
        {
            string relative = Path.GetRelativePath(projectDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
            string png = Path.Combine(outputDirectory, Path.ChangeExtension(relative, ".png"));
            string? error = await SaveAsync(new SnapshotWindow(renderer, file, createTarget()), png, options);
            isGood &= AddSummaryLine(summary, relative, error);
        }

        TargetStyles galleryTarget = createTarget();
        SnapshotWindow galleryWindow = new(ControlGallery.Create(galleryTarget.Styles, galleryTarget.Resources), galleryTarget);
        string? galleryError = await SaveAsync(galleryWindow, Path.Combine(outputDirectory, GalleryFileName), options);
        isGood &= AddSummaryLine(summary, GalleryName, galleryError);

        string summaryText = summary.ToString();
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, SummaryFileName), summaryText);
        await Console.Error.WriteAsync(summaryText);
        return isGood ? ExitReason.Ok : ExitReason.RenderFailed;
    }

    // Gives the error of the snapshot, or null for a good snapshot.
    private async Task<string?> SaveAsync(SnapshotWindow window, string png, HostOptions options)
    {
        window.PreviewSize = options.Size ?? PreviewSize.Fit;
        window.ShowTheme(options.Theme ?? PreviewTheme.Default);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(png))!);
        try
        {
            window.Show();
            await Task.Delay(settleDelay);
            window.Save(png);
            return window.Error;
        }
        catch (Exception ex)
        {
            // Layout runs user code. It can throw any exception. A snapshot-all run goes on with the next file.
            return ExceptionText.Describe(ex);
        }
        finally
        {
            window.Close();
        }
    }
}
