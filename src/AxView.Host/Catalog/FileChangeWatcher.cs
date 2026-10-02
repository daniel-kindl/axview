using Avalonia.Threading;

namespace AxView.Host.Catalog;

/// <summary>Raises one change on the UI thread after a burst of file system events for the watched files.</summary>
/// <remarks>
/// Editors often save by writing a temporary file and renaming it, so the watcher also accepts renames.
/// A deleted code file also changes the build, so the watcher also accepts deletes.
/// </remarks>
internal sealed class FileChangeWatcher : IDisposable
{
    private static readonly StringComparison pathComparison =
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private readonly Func<string, bool> isWatched;
    private readonly FileSystemWatcher watcher;
    private readonly DispatcherTimer debounce;

    private FileChangeWatcher(string directory, bool includeSubdirectories, Func<string, bool> isWatched, Action changed)
    {
        this.isWatched = isWatched;
        debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        debounce.Tick += (_, _) =>
        {
            debounce.Stop();
            changed();
        };

        watcher = new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime,
        };
        watcher.Changed += OnEvent;
        watcher.Created += OnEvent;
        watcher.Deleted += OnEvent;
        watcher.Renamed += OnEvent;
        watcher.EnableRaisingEvents = true;
    }

    /// <summary>Watches one file.</summary>
    /// <param name="path">The file to watch.</param>
    /// <param name="changed">Called on the UI thread after the file changes.</param>
    public static FileChangeWatcher ForFile(string path, Action changed)
    {
        string fullPath = Path.GetFullPath(path);
        return new FileChangeWatcher(
            Path.GetDirectoryName(fullPath)!,
            includeSubdirectories: false,
            changedPath => string.Equals(changedPath, fullPath, pathComparison),
            changed);
    }

    /// <summary>Watches the C# files of a project. See <see cref="ProjectSources.IsCodeFile"/>.</summary>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <param name="changed">Called on the UI thread after one or more code files change.</param>
    public static FileChangeWatcher ForCodeFiles(string projectDirectory, Action changed)
    {
        string fullDirectory = Path.GetFullPath(projectDirectory);
        return new FileChangeWatcher(
            fullDirectory,
            includeSubdirectories: true,
            changedPath => ProjectSources.IsCodeFile(fullDirectory, changedPath),
            changed);
    }

    /// <summary>Watches the AXAML files of a project. See <see cref="ProjectSources.IsAxamlFile"/>.</summary>
    /// <param name="projectDirectory">The folder of the project.</param>
    /// <param name="changed">Called on the UI thread after one or more AXAML files change.</param>
    public static FileChangeWatcher ForAxamlFiles(string projectDirectory, Action changed)
    {
        string fullDirectory = Path.GetFullPath(projectDirectory);
        return new FileChangeWatcher(
            fullDirectory,
            includeSubdirectories: true,
            changedPath => ProjectSources.IsAxamlFile(fullDirectory, changedPath),
            changed);
    }

    public void Dispose()
    {
        watcher.Dispose();
        debounce.Stop();
    }

    private void OnEvent(object sender, FileSystemEventArgs e)
    {
        if (isWatched(Path.GetFullPath(e.FullPath)))
        {
            Dispatcher.UIThread.Post(() =>
            {
                debounce.Stop();
                debounce.Start();
            });
        }
    }
}
