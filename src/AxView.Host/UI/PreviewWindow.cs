using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using AxView.Host.Catalog;
using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>
/// Lists the project's AXAML files and shows the selected one. Reloads it on save.
/// Keeps the last good render when a load fails.
/// </summary>
/// <remarks>
/// When a code file of the project changes, the host stops. The launcher then builds the app and starts a new host.
/// When an include file of the app styles or resources changes, the window loads it from disk and renders the preview again.
/// </remarks>
internal sealed class PreviewWindow : Window
{
    private const double SidebarWidth = 280;

    private static readonly FontFamily monospace = new("Cascadia Mono, Consolas, Menlo, DejaVu Sans Mono, monospace");
    private static readonly StringComparison pathComparison =
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private readonly HostOptions options;
    private readonly LivePreview livePreview;
    private readonly ContentControl host = new();
    private readonly PreviewScope scope;
    private readonly Border errorPanel;
    private readonly SelectableTextBlock errorText;
    private readonly TextBlock status;
    private readonly Border noDataContextBadge;
    private readonly TextBox filter;
    private readonly ListBox fileList;
    private readonly ThemeSwitch themeSwitch;
    private readonly PreviewSizeSwitch sizeSwitch;
    private readonly ViewModelSwitch viewModelSwitch;
    private readonly Dictionary<string, string> viewModels;
    private readonly WindowPlacementTracker? placementTracker;
    private readonly FileChangeWatcher? codeWatcher;
    private readonly FileChangeWatcher? appCodeWatcher;
    private readonly AppIncludes? appIncludes;
    private readonly FileChangeWatcher? axamlWatcher;
    private IReadOnlyList<string> catalog = [];
    private bool isSyncingSelection;

    public PreviewWindow(HostOptions options, IReadOnlyList<string> notes, DesignServices services, TargetStyles target)
    {
        this.options = options;
        scope = new PreviewScope(target);
        host.Content = scope;
        Assembly localAssembly = Assembly.Load(options.AssemblyName);
        PreviewRenderer renderer = new(localAssembly, options.ProjectDirectory, services);
        livePreview = new LivePreview(renderer);
        livePreview.Changed += (_, _) => ShowPreview();

        Width = 1280;
        Height = 800;
        Settings settings = options.SettingsFile is null ? Settings.Default : Settings.Load(options.SettingsFile);
        if (settings.Window is not null)
        {
            WindowPlacementTracker.Apply(this, settings.Window);
        }

        // Without a settings file, the window has no placement to keep.
        placementTracker = options.SettingsFile is null ? null : new WindowPlacementTracker(this);
        themeSwitch = new ThemeSwitch(scope) { SelectedItem = settings.Theme };
        sizeSwitch = new PreviewSizeSwitch(host) { Size = settings.Size };
        viewModels = new Dictionary<string, string>(settings.ViewModels, StringComparer.FromComparison(pathComparison));
        viewModelSwitch = new ViewModelSwitch(ViewModelCatalog.List(localAssembly, services));
        viewModelSwitch.SelectionChanged += (_, _) => OnViewModelSelected();

        status = new TextBlock { Margin = new Thickness(8, 4), FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis };
        ToolTip.SetTip(status, string.Join(Environment.NewLine, notes));
        noDataContextBadge = new Border
        {
            IsVisible = false,
            Margin = new Thickness(8, 2),
            Padding = new Thickness(6, 1),
            CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(Color.FromRgb(0xB0, 0x6A, 0x00)),
            Child = new TextBlock { Text = "No DataContext", FontSize = 11, Foreground = Brushes.White },
        };
        ToolTip.SetTip(noDataContextBadge, "Select a ViewModel, set Design.DataContext, or make x:DataType creatable. See AxViewDesign.CreateServices.");
        DockPanel.SetDock(noDataContextBadge, Dock.Right);

        errorText = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White,
            FontFamily = monospace,
            FontSize = 12,
        };
        errorPanel = new Border
        {
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Bottom,
            MaxHeight = 320,
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Color.FromArgb(0xE6, 0x8B, 0x1A, 0x1A)),
            Child = new ScrollViewer { Content = errorText },
        };

        filter = new TextBox { Watermark = "Filter", Margin = new Thickness(8) };
        filter.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                ShowCatalog();
            }
        };

        fileList = new ListBox { ItemTemplate = new FuncDataTemplate<string>((relative, _) => FileItem(relative)) };
        fileList.SelectionChanged += (_, _) => OnFileSelected();

        Button openButton = new() { Content = "Open...", Margin = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Stretch };
        openButton.Click += async (_, _) => await OpenAsync();

        Control sidebar = BuildSidebar(openButton);
        Control stage = BuildStage();
        Border statusBar = new()
        {
            Child = new DockPanel { Children = { noDataContextBadge, status } },
            BorderThickness = new Thickness(0, 1, 0, 0),
            BorderBrush = Brushes.Gray,
        };
        DockPanel.SetDock(statusBar, Dock.Bottom);

        Grid body = new() { ColumnDefinitions = new ColumnDefinitions($"{SidebarWidth},Auto,*") };
        body.Children.Add(sidebar);
        GridSplitter splitter = new() { Width = 4, ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(splitter, 1);
        body.Children.Add(splitter);
        Grid.SetColumn(stage, 2);
        body.Children.Add(stage);

        Content = new DockPanel { Children = { statusBar, body } };

        // The user can add files while the window is open.
        Activated += (_, _) => RefreshCatalog();

        // Only the launcher can build the app and start a new host.
        if (options.HandOffFile is not null)
        {
            codeWatcher = FileChangeWatcher.ForCodeFiles(
                options.ProjectDirectory,
                () => StopWithHandOff(livePreview.FilePath, ExitReason.Rebuild));

            // A library view also needs a rebuild when the code of the app changes.
            if (options.AppProjectDirectory is not null)
            {
                appCodeWatcher = FileChangeWatcher.ForCodeFiles(
                    options.AppProjectDirectory,
                    () => StopWithHandOff(livePreview.FilePath, ExitReason.Rebuild));
            }
        }

        // The app file of a library view is in another project. Its includes do not load again.
        string? appFile = AppIncludes.FindAppFile(options.ProjectDirectory);
        if (appFile is not null)
        {
            appIncludes = new AppIncludes(renderer, appFile, scope.Styles, scope.Resources);
            axamlWatcher = FileChangeWatcher.ForAxamlFiles(options.ProjectDirectory, UpdateIncludes);
        }

        Show(options.AxamlPath);
        RefreshCatalog();
        if (options.SetupError is not null)
        {
            ShowError(options.SetupError);
        }
    }

    // Save before the window closes. A closed window has no position.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (!e.Cancel)
        {
            SaveSettings();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        codeWatcher?.Dispose();
        appCodeWatcher?.Dispose();
        axamlWatcher?.Dispose();
        livePreview.Dispose();
        base.OnClosed(e);
    }

    private DockPanel BuildSidebar(Button openButton)
    {
        TextBlock projectName = new()
        {
            Text = Path.GetFileName(options.ProjectDirectory.TrimEnd(Path.DirectorySeparatorChar)),
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(8, 8, 8, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        ToolTip.SetTip(projectName, options.ProjectDirectory);

        DockPanel sidebar = new();
        DockPanel.SetDock(projectName, Dock.Top);
        DockPanel.SetDock(filter, Dock.Top);
        DockPanel.SetDock(openButton, Dock.Bottom);
        sidebar.Children.Add(projectName);
        sidebar.Children.Add(filter);
        sidebar.Children.Add(openButton);
        sidebar.Children.Add(fileList);
        return sidebar;
    }

    // The file name first, so a long folder path does not hide it.
    private static StackPanel FileItem(string? relative)
    {
        string path = relative ?? string.Empty;
        string folder = Path.GetDirectoryName(path)?.Replace(Path.DirectorySeparatorChar, '/') ?? string.Empty;
        StackPanel item = new() { Spacing = 1 };
        item.Children.Add(new TextBlock { Text = Path.GetFileNameWithoutExtension(path), TextTrimming = TextTrimming.CharacterEllipsis });
        item.Children.Add(new TextBlock
        {
            Text = folder.Length == 0 ? "/" : folder,
            FontSize = 11,
            Opacity = 0.6,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        ToolTip.SetTip(item, path);
        return item;
    }

    private DockPanel BuildStage()
    {
        Grid preview = new();
        preview.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = host,
        });
        preview.Children.Add(errorPanel);

        Border toolbar = new()
        {
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(8, 4),
                Spacing = 8,
                Children =
                {
                    themeSwitch,
                    sizeSwitch,
                    new TextBlock { Text = "DataContext", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center },
                    viewModelSwitch,
                },
            },
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = Brushes.Gray,
        };
        DockPanel.SetDock(toolbar, Dock.Top);
        return new DockPanel { Children = { toolbar, preview } };
    }

    private void SaveSettings()
    {
        if (placementTracker is not null)
        {
            UpdateSettings(settings => settings with
            {
                Window = placementTracker.GetPlacement(),
                Theme = themeSwitch.SelectedItem is PreviewTheme theme ? theme : PreviewTheme.Default,
                Size = sizeSwitch.Size,
            });
        }
    }

    // The settings file must not stop the host. A failed write only loses the changed values.
    private void UpdateSettings(Func<Settings, Settings> change)
    {
        if (options.SettingsFile is null)
        {
            return;
        }

        try
        {
            change(Settings.Load(options.SettingsFile)).Save(options.SettingsFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"axview: cannot save the settings: {ex.Message}");
        }
    }

    // Save the choice now. A hand-off stops the host without a close of the window.
    private void OnViewModelSelected()
    {
        if (isSyncingSelection)
        {
            return;
        }

        string? typeName = viewModelSwitch.SelectedTypeName;
        if (typeName is null)
        {
            _ = viewModels.Remove(livePreview.FilePath);
        }
        else
        {
            viewModels[livePreview.FilePath] = typeName;
        }

        UpdateSettings(settings => settings with { ViewModels = new Dictionary<string, string>(viewModels) });
        livePreview.Show(livePreview.FilePath, viewModelSwitch.SelectedType);
    }

    // A StaticResource does not follow a change of the app resources. Thus render the preview again.
    private void UpdateIncludes()
    {
        IncludeUpdate update = appIncludes!.Update();
        if (update.IsChanged)
        {
            livePreview.Reload();
        }

        if (update.Error is not null)
        {
            ShowError(update.Error);
        }
    }

    private void RefreshCatalog()
    {
        catalog = AxamlCatalog.List(options.ProjectDirectory);
        ShowCatalog();
    }

    private void ShowCatalog()
    {
        string text = filter.Text ?? string.Empty;
        List<string> items = [.. catalog
            .Select(RelativePath)
            .Where(p => p.Contains(text, StringComparison.OrdinalIgnoreCase))];

        isSyncingSelection = true;
        fileList.ItemsSource = items;
        string shown = RelativePath(livePreview.FilePath);
        fileList.SelectedItem = items.FirstOrDefault(p => string.Equals(p, shown, pathComparison));
        isSyncingSelection = false;
    }

    private void OnFileSelected()
    {
        if (!isSyncingSelection && fileList.SelectedItem is string relative)
        {
            Show(Path.Combine(options.ProjectDirectory, relative));
        }
    }

    private async Task OpenAsync()
    {
        IStorageFolder? start = await StorageProvider.TryGetFolderFromPathAsync(options.ProjectDirectory);
        IReadOnlyList<IStorageFile> picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open AXAML file",
            AllowMultiple = false,
            SuggestedStartLocation = start,
            FileTypeFilter = [new FilePickerFileType("AXAML") { Patterns = ["*.axaml"] }],
        });
        string? path = picked.Count == 1 ? picked[0].TryGetLocalPath() : null;
        if (path is null)
        {
            return;
        }

        if (catalog.Any(f => string.Equals(f, Path.GetFullPath(path), pathComparison)))
        {
            Show(path);
            return;
        }

        SwitchProject(path);
    }

    // A file of another project needs that project's app runtime. Only the launcher can start it.
    private void SwitchProject(string path)
    {
        if (options.HandOffFile is null)
        {
            ShowError($"'{path}' belongs to another project. Start axview with that file.");
            return;
        }

        StopWithHandOff(path, ExitReason.SwitchProject);
    }

    // The shutdown does not close the window. Thus save the settings first, so the next host opens at the same place.
    private void StopWithHandOff(string nextFile, ExitReason reason)
    {
        SaveSettings();
        new HandOff(Path.GetFullPath(nextFile)).Save(options.HandOffFile!);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown((int)reason);
        }
    }

    private void Show(string path)
    {
        Title = $"AxView - {RelativePath(path)}";
        string fullPath = Path.GetFullPath(path);
        isSyncingSelection = true;
        viewModelSwitch.SelectedTypeName = viewModels.GetValueOrDefault(fullPath);
        isSyncingSelection = false;
        livePreview.Show(fullPath, viewModelSwitch.SelectedType);
        ShowCatalog();
    }

    private void ShowPreview()
    {
        scope.Content = livePreview.View;
        noDataContextBadge.IsVisible = livePreview.View is { DataContext: null } && !livePreview.IsStylePreview;
        if (livePreview.Error is not null)
        {
            ShowError(livePreview.Error);
            return;
        }

        HideError();
        status.Text = $"Rendered {DateTime.Now:HH:mm:ss}   {livePreview.FilePath}";
        Console.WriteLine($"axview: rendered {RelativePath(livePreview.FilePath)}");
    }

    private void ShowError(string message)
    {
        errorText.Text = message;
        errorPanel.IsVisible = true;
        Console.Error.WriteLine($"axview: error: {errorText.Text}");
        status.Text = livePreview.View is null
            ? $"Error {DateTime.Now:HH:mm:ss}   {livePreview.FilePath}"
            : $"Error {DateTime.Now:HH:mm:ss}. Showing the last good render.   {livePreview.FilePath}";
    }

    private void HideError() => errorPanel.IsVisible = false;

    private string RelativePath(string path) =>
        Path.GetRelativePath(options.ProjectDirectory, path);
}
