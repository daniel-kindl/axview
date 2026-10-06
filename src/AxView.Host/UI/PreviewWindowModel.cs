using Avalonia.Controls;

using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>The headless state of the preview window: the file list, the shown item, the status, and the settings.</summary>
/// <remarks>
/// The window only shows the state. It calls the methods of the model for each user action.
/// <list type="bullet">
/// <item><description>The file list has the gallery entry first, then the files of the project that agree with the filter.</description></item>
/// <item><description>The shown item is a file of the project or the control gallery.</description></item>
/// <item><description>The error of the shown file shows until the next load.</description></item>
/// <item><description>A message, for example the setup error, shows until the next load or the next shown item.</description></item>
/// <item><description>The file stays live behind the gallery. Each change of the preview makes the gallery again.</description></item>
/// </list>
/// A rebuild or a project switch saves the settings, then stops the host with the exit reason of the hand-off.
/// </remarks>
internal sealed class PreviewWindowModel : IDisposable
{
    private const string GalleryTitle = "Control gallery";
    private const string GalleryDetail = "The controls in each state, with the app styles";
    private const string TitlePrefix = "AxView - ";

    private static readonly FileListEntry galleryEntry = new(GalleryTitle, GalleryDetail, null, FileKind.Gallery);
    private static readonly StringComparison pathComparison =
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private readonly HostOptions options;
    private readonly ProjectPreview preview;
    private readonly Func<Settings, Settings> addWindowState;
    private readonly Action<int> shutdown;
    private readonly string outputDirectory;

    // A filter change lists the entries again. The cache keeps the files from a read for each key press.
    private readonly Dictionary<string, FileKind> fileKinds = new(StringComparer.FromComparison(pathComparison));
    private string filter = string.Empty;
    private bool isGalleryShown;

    // A message that is not an error of the shown item. For example, the setup error.
    private string? message;

    /// <summary>Shows the first file of the options and lists the files of the project.</summary>
    /// <param name="options">The options of the host.</param>
    /// <param name="projects">The projects of the preview.</param>
    /// <param name="services">The design services that create the ViewModels.</param>
    /// <param name="appStyles">The app styles. The model gets a live copy for the shown item.</param>
    /// <param name="addWindowState">Adds the state of the window, for example its placement, to the settings before a save.</param>
    /// <param name="shutdown">Stops the host with an exit code.</param>
    public PreviewWindowModel(
        HostOptions options,
        ProjectSet projects,
        DesignServices services,
        AppStyles appStyles,
        Func<Settings, Settings> addWindowState,
        Action<int> shutdown)
    {
        this.options = options;
        this.addWindowState = addWindowState;
        this.shutdown = shutdown;
        outputDirectory = Path.GetDirectoryName(projects.ViewAssembly.Location) ?? string.Empty;
        Styles = appStyles.CreateCopy();
        Settings = options.SettingsFile is null ? Settings.Default : Settings.Load(options.SettingsFile);
        preview = new ProjectPreview(options, projects, services, appStyles, Settings.ViewModels);
        preview.Changed += (_, _) => ShowItem(null);
        preview.Failed += (_, error) => ShowItem(error);
        preview.StopRequested += (_, handOff) => Stop(handOff);
        preview.RefreshCatalog();
        fileKinds.Clear();
        UpdateEntries();
        preview.Show(options.AxamlPath);
        if (options.SetupError is not null)
        {
            ShowItem(options.SetupError);
        }
    }

    /// <summary>Occurs after a change of the state.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the live copy of the app styles for the shown item. The preview scope of the window must own it.</summary>
    public TargetStyles Styles { get; }

    /// <summary>Gets the settings at the start. The window takes its placement, theme, and preview size from them.</summary>
    public Settings Settings { get; }

    /// <summary>Gets the ViewModel types that the user can select.</summary>
    public IReadOnlyList<Type> ViewModelTypes => preview.ViewModelTypes;

    /// <summary>Gets the title of the window. It names the shown item.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the entries of the file list that agree with the filter. A new list replaces the list on each change.</summary>
    public IReadOnlyList<FileListEntry> Entries { get; private set; } = [];

    /// <summary>Gets the entry of the shown item. Null when the filter hides it.</summary>
    public FileListEntry? SelectedEntry =>
        isGalleryShown
            ? Entries.FirstOrDefault(e => e.IsGallery)
            : Entries.FirstOrDefault(e => string.Equals(e.RelativePath, RelativePath(preview.FilePath), pathComparison));

    /// <summary>Gets the control of the shown item: the last good preview of the file, or the gallery.</summary>
    public Control? Content { get; private set; }

    /// <summary>Gets the errors and the message to show. Null when there is none.</summary>
    public string? Error { get; private set; }

    /// <summary>Gets a value that tells if the preview shows the last good render of the file, because the last load failed.</summary>
    public bool IsLastGoodRenderShown { get; private set; }

    /// <summary>Gets the status line. It tells the time of the last load or error, the shown item, and the app output.</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>Gets a value that tells if the shown file is a control without a DataContext.</summary>
    public bool IsDataContextMissing { get; private set; }

    /// <summary>Gets a value that tells if the user can select a ViewModel. Only a file has a ViewModel.</summary>
    public bool CanSelectViewModel => !isGalleryShown && (ViewModelTypes.Count > 0);

    /// <summary>Gets the full name of the selected ViewModel type of the shown file. Null for the DataContext from the XAML.</summary>
    public string? ViewModelName => ViewModelTypes.FirstOrDefault(t => t.FullName == preview.ViewModelName)?.FullName;

    /// <summary>Shows only the entries whose path or title contains the text. The case of the letters is ignored.</summary>
    /// <param name="text">The filter text.</param>
    public void SetFilter(string text)
    {
        filter = text;
        UpdateEntries();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Lists the files of the project again. The user can add files while the window is open.</summary>
    public void RefreshCatalog()
    {
        preview.RefreshCatalog();
        fileKinds.Clear();
        UpdateEntries();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Shows the item of an entry of the file list. The entry of the shown item does nothing.</summary>
    /// <param name="entry">An entry of <see cref="Entries"/>.</param>
    public void Select(FileListEntry entry)
    {
        if (entry == SelectedEntry)
        {
            return;
        }

        if (entry.RelativePath is null)
        {
            isGalleryShown = true;
            ShowItem(null);
            return;
        }

        isGalleryShown = false;
        preview.Show(Path.Combine(options.ProjectDirectory, entry.RelativePath));
    }

    /// <summary>Shows a file that the user selected. A file of another project stops the host for a project switch.</summary>
    /// <param name="path">An AXAML file.</param>
    public void Open(string path)
    {
        isGalleryShown = false;
        preview.Open(path);
    }

    /// <summary>Selects the ViewModel of the shown file, loads the file again, and saves the choice.</summary>
    /// <remarks>Save the choice now. A hand-off stops the host without a close of the window.</remarks>
    /// <param name="type">The ViewModel type, or null to use the DataContext from the XAML.</param>
    public void SelectViewModel(Type? type)
    {
        if (type?.FullName == ViewModelName)
        {
            return;
        }

        preview.SelectViewModel(type);
        SaveSettings();
    }

    /// <summary>Saves the state of the window and the ViewModel of each file in the settings file of the options.</summary>
    /// <remarks>The settings file must not stop the host. A failed write only loses the changed values.</remarks>
    public void SaveSettings()
    {
        if (options.SettingsFile is not string settingsFile)
        {
            return;
        }

        try
        {
            Settings changed = addWindowState(Settings.Load(settingsFile)) with
            {
                ViewModels = new Dictionary<string, string>(preview.ViewModels),
            };
            changed.Save(settingsFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"axview: cannot save the settings: {ex.Message}");
        }
    }

    /// <summary>Stops the watches and the live preview.</summary>
    public void Dispose() => preview.Dispose();

    private static string? Join(params string?[] errors)
    {
        List<string> found = [.. errors.OfType<string>()];
        return found.Count == 0 ? null : string.Join(Environment.NewLine, found);
    }

    private FileListEntry FileEntry(string path)
    {
        string relativePath = RelativePath(path);
        string folder = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '/') ?? string.Empty;
        return new FileListEntry(Path.GetFileNameWithoutExtension(relativePath), folder.Length == 0 ? "/" : folder, relativePath, KindOf(path));
    }

    private void UpdateEntries()
    {
        List<FileListEntry> entries = [.. preview.Catalog
            .Where(p => RelativePath(p).Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Select(FileEntry)];
        if (GalleryTitle.Contains(filter, StringComparison.OrdinalIgnoreCase))
        {
            entries.Insert(0, galleryEntry);
        }

        Entries = entries;
    }

    // Shows the shown item again with a new message. Each load and each shown item removes the old message.
    private void ShowItem(string? newMessage)
    {
        message = newMessage;
        if (isGalleryShown)
        {
            ShowGallery();
        }
        else
        {
            ShowFile();
        }

        if (Error is not null)
        {
            Console.Error.WriteLine($"axview: error: {Error}");
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    // The gallery shows the scope styles. An include reload can add or remove a style class or a keyed theme.
    private void ShowGallery()
    {
        Title = TitlePrefix + GalleryTitle;
        Content = ControlGallery.Create([Styles.Styles], Styles.Resources);
        IsDataContextMissing = false;
        Error = Join(message, preview.StylesError);
        IsLastGoodRenderShown = false;
        Status = Error is null ? $"{GalleryTitle}   Output: {outputDirectory}" : $"Error {DateTime.Now:HH:mm:ss}   {GalleryTitle}";
    }

    private void ShowFile()
    {
        Title = TitlePrefix + RelativePath(preview.FilePath);
        Content = preview.View;
        IsDataContextMissing = preview.View is { DataContext: null } && !preview.IsStylePreview;
        Error = Join(message, preview.Error, preview.StylesError);
        IsLastGoodRenderShown = (Error is not null) && (preview.View is not null);
        if (Error is null)
        {
            Status = $"Rendered {DateTime.Now:HH:mm:ss}   {preview.FilePath}   Output: {outputDirectory}";
            Console.WriteLine($"axview: rendered {RelativePath(preview.FilePath)}");
            return;
        }

        Status = IsLastGoodRenderShown
            ? $"Error {DateTime.Now:HH:mm:ss}. Showing the last good render.   {preview.FilePath}"
            : $"Error {DateTime.Now:HH:mm:ss}   {preview.FilePath}";
    }

    // The shutdown does not close the window. Thus save the settings first, so the next host opens at the same place.
    private void Stop(HandOff handOff)
    {
        SaveSettings();
        shutdown((int)handOff.Reason);
    }

    private FileKind KindOf(string path)
    {
        if (!fileKinds.TryGetValue(path, out FileKind kind))
        {
            kind = FileKindReader.Read(path);
            fileKinds[path] = kind;
        }

        return kind;
    }

    private string RelativePath(string path) => Path.GetRelativePath(options.ProjectDirectory, path);
}
