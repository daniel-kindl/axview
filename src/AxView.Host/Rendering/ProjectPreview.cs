using Avalonia.Controls;

using AxView.Host.Catalog;
using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host.Rendering;

/// <summary>The live preview of the AXAML files of one project, without a window.</summary>
/// <remarks>
/// It owns these parts:
/// <list type="bullet">
/// <item><description>The catalog of the project files.</description></item>
/// <item><description>The live preview of the shown file, with its ViewModel. Each file keeps its own ViewModel.</description></item>
/// <item><description>The include reload of the app styles.</description></item>
/// <item><description>The rebuild watch of the C# files.</description></item>
/// </list>
/// Only the launcher can build the app or open another project. Thus a rebuild or a project switch gives a
/// <see cref="StopRequested"/> event. The owner writes the hand-off file and stops the host.
/// Without a hand-off file in the options, there is no rebuild watch, and a file of another project gives an error.
/// </remarks>
internal sealed class ProjectPreview : IDisposable
{
    private static readonly StringComparison pathComparison =
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private readonly HostOptions options;
    private readonly AppStyles appStyles;
    private readonly LivePreview livePreview;
    private readonly Dictionary<string, string> viewModels;
    private readonly List<FileChangeWatcher> watchers = [];

    /// <summary>Starts the watches of the code files and of the include files. Call <see cref="Show"/> to show a file.</summary>
    /// <param name="options">The options of the host.</param>
    /// <param name="projects">The projects of the preview.</param>
    /// <param name="services">The design services that create the ViewModels.</param>
    /// <param name="appStyles">The app styles. The preview reloads the shown file after an include of them changes.</param>
    /// <param name="viewModels">The full name of the selected ViewModel type of each file, by full path.</param>
    public ProjectPreview(
        HostOptions options, ProjectSet projects, DesignServices services, AppStyles appStyles, IReadOnlyDictionary<string, string> viewModels)
    {
        this.options = options;
        this.appStyles = appStyles;
        this.viewModels = new Dictionary<string, string>(viewModels, StringComparer.FromComparison(pathComparison));
        ViewModelTypes = ViewModelCatalog.List(projects.ViewAssembly, services);
        livePreview = new LivePreview(projects.CreateViewRenderer(services));
        livePreview.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

        if (options.HandOffFile is not null)
        {
            // A library view also needs a rebuild when the code of the app or of a referenced project changes.
            foreach (string directory in projects.CodeDirectories)
            {
                watchers.Add(FileChangeWatcher.ForCodeFiles(directory, () => RequestStop(ExitReason.Rebuild, FilePath)));
            }
        }

        // The app file of a library view can be in the app project.
        foreach (string directory in appStyles.ProjectDirectories)
        {
            watchers.Add(FileChangeWatcher.ForAxamlFiles(directory, UpdateIncludes));
        }
    }

    /// <summary>Occurs after a load of the shown file or a dispatcher error. See <see cref="LivePreview.Changed"/>.</summary>
    public event EventHandler? Changed;

    /// <summary>Occurs on an error that is not an error of the shown file. For example, a broken include.</summary>
    public event EventHandler<string>? Failed;

    /// <summary>Occurs when the launcher must build the app or open another project.</summary>
    public event EventHandler<StopRequest>? StopRequested;

    /// <summary>Gets the full path of the shown file. Empty before the first <see cref="Show"/>.</summary>
    public string FilePath => livePreview.FilePath;

    /// <summary>Gets the last good preview of the shown file. Null when no load of the file succeeded.</summary>
    public Control? View => livePreview.View;

    /// <summary>Gets a value that tells if <see cref="View"/> is the preview of a style or resource root.</summary>
    public bool IsStylePreview => livePreview.IsStylePreview;

    /// <summary>Gets the message of the most recent error of the shown file. Null after a good load.</summary>
    public string? Error => livePreview.Error;

    /// <summary>Gets the AXAML files of the project, with full paths. Empty before the first <see cref="RefreshCatalog"/>.</summary>
    public IReadOnlyList<string> Catalog { get; private set; } = [];

    /// <summary>Gets the ViewModel types that the user can select.</summary>
    public IReadOnlyList<Type> ViewModelTypes { get; }

    /// <summary>Gets the full name of the selected ViewModel type of the shown file. Null for the DataContext from the XAML.</summary>
    public string? ViewModelName => viewModels.GetValueOrDefault(FilePath);

    /// <summary>Gets the full name of the selected ViewModel type of each file, by full path.</summary>
    public IReadOnlyDictionary<string, string> ViewModels => viewModels;

    /// <summary>Lists the AXAML files of the project again. The user can add files while the preview runs.</summary>
    public void RefreshCatalog() => Catalog = AxamlCatalog.List(options.ProjectDirectory);

    /// <summary>Shows a file of the project with its selected ViewModel.</summary>
    /// <param name="path">An AXAML file of the project.</param>
    public void Show(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string? typeName = viewModels.GetValueOrDefault(fullPath);
        livePreview.Show(fullPath, ViewModelTypes.FirstOrDefault(t => t.FullName == typeName));
    }

    /// <summary>Shows a file of the project. A file of another project gives a <see cref="StopRequested"/> event.</summary>
    /// <param name="path">An AXAML file that the user selected.</param>
    public void Open(string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (Catalog.Any(f => string.Equals(f, fullPath, pathComparison)))
        {
            Show(fullPath);
            return;
        }

        // A file of another project needs that project's app runtime. Only the launcher can start it.
        if (options.HandOffFile is null)
        {
            Failed?.Invoke(this, $"'{path}' belongs to another project. Start axview with that file.");
            return;
        }

        RequestStop(ExitReason.SwitchProject, fullPath);
    }

    /// <summary>Selects the ViewModel of the shown file and loads the file again.</summary>
    /// <param name="type">The ViewModel type, or null to use the DataContext from the XAML.</param>
    public void SelectViewModel(Type? type)
    {
        if (type?.FullName is string typeName)
        {
            viewModels[FilePath] = typeName;
        }
        else
        {
            _ = viewModels.Remove(FilePath);
        }

        livePreview.Show(FilePath, type);
    }

    /// <summary>Stops the watches and the live preview.</summary>
    public void Dispose()
    {
        foreach (FileChangeWatcher watcher in watchers)
        {
            watcher.Dispose();
        }

        livePreview.Dispose();
    }

    // A StaticResource does not follow a change of the app resources. Thus render the preview again.
    private void UpdateIncludes()
    {
        IncludeUpdate update = appStyles.Update();
        if (update.IsChanged)
        {
            livePreview.Reload();
        }

        if (update.Error is not null)
        {
            Failed?.Invoke(this, update.Error);
        }
    }

    private void RequestStop(ExitReason reason, string nextFile) =>
        StopRequested?.Invoke(this, new StopRequest(reason, Path.GetFullPath(nextFile)));
}
