using System.Reflection;

using Avalonia.Controls;

using AxView.Host.Catalog;

namespace AxView.Host.Rendering;

/// <summary>Shows one AXAML file of the project. Loads it again each time the file changes on disk.</summary>
/// <remarks>
/// A failed load keeps the last good preview and gives the error.
/// An exception that escapes the dispatcher after a load, for example in layout or in a binding, also gives the error.
/// Each change of <see cref="View"/> or <see cref="Error"/> raises <see cref="Changed"/> on the UI thread.
/// </remarks>
internal sealed class LivePreview : IDisposable
{
    private readonly PreviewRenderer renderer;
    private readonly Action? unsubscribeErrors;
    private FileChangeWatcher? watcher;
    private Type? viewModelType;

    /// <summary>Starts to catch the dispatcher errors. Call <see cref="Show"/> to show a file.</summary>
    /// <param name="renderer">The renderer of the project.</param>
    public LivePreview(PreviewRenderer renderer)
    {
        this.renderer = renderer;
        unsubscribeErrors = DispatcherErrors.Subscribe(OnUnhandledException);
    }

    /// <summary>Occurs after a load or a dispatcher error.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the full path of the shown file. Empty before the first <see cref="Show"/>.</summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>Gets the last good preview of the shown file. Null when no load of the file succeeded.</summary>
    public Control? View { get; private set; }

    /// <summary>Gets the message of the most recent error. Null after a good load.</summary>
    public string? Error { get; private set; }

    /// <summary>Loads the file and watches it. Stops the watch of the previous file.</summary>
    /// <param name="path">An AXAML file of the project.</param>
    /// <param name="viewModelType">The type of the root DataContext, or null to use the DataContext from the XAML.</param>
    public void Show(string path, Type? viewModelType = null)
    {
        string fullPath = Path.GetFullPath(path);
        FilePath = fullPath;
        this.viewModelType = viewModelType;
        watcher?.Dispose();
        watcher = FileChangeWatcher.ForFile(fullPath, () => Load(fullPath));
        View = null;
        Load(fullPath);
    }

    /// <summary>Stops the file watch and the dispatcher error handler.</summary>
    public void Dispose()
    {
        watcher?.Dispose();
        watcher = null;
        unsubscribeErrors?.Invoke();
    }

    private static string Describe(Exception ex)
    {
        Exception shown = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
        return $"{shown.GetType().Name}: {shown.Message}";
    }

    private void Load(string path)
    {
        try
        {
            View = renderer.Render(path, viewModelType);
            Error = null;
        }
        catch (Exception ex)
        {
            // User XAML and its code can throw any exception. Keep the last good preview.
            Error = Describe(ex);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnUnhandledException(Exception ex)
    {
        Error = Describe(ex);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
