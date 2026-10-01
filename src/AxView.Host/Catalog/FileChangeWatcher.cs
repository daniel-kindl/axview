using Avalonia.Threading;

namespace AxView.Host.Catalog;

/// <summary>Raises one change on the UI thread after a burst of file system events for one file.</summary>
/// <remarks>Editors often save by writing a temporary file and renaming it, so the watcher also accepts renames.</remarks>
internal sealed class FileChangeWatcher : IDisposable
{
    private static readonly StringComparison pathComparison =
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private readonly string path;
    private readonly FileSystemWatcher watcher;
    private readonly DispatcherTimer debounce;

    public FileChangeWatcher(string path, Action changed)
    {
        this.path = Path.GetFullPath(path);
        debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        debounce.Tick += (_, _) =>
        {
            debounce.Stop();
            changed();
        };

        watcher = new FileSystemWatcher(Path.GetDirectoryName(this.path)!)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime,
        };
        watcher.Changed += OnEvent;
        watcher.Created += OnEvent;
        watcher.Renamed += OnEvent;
        watcher.EnableRaisingEvents = true;
    }

    public void Dispose()
    {
        watcher.Dispose();
        debounce.Stop();
    }

    private void OnEvent(object sender, FileSystemEventArgs e)
    {
        if (string.Equals(Path.GetFullPath(e.FullPath), path, pathComparison))
        {
            Dispatcher.UIThread.Post(() =>
            {
                debounce.Stop();
                debounce.Start();
            });
        }
    }
}
