using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>
/// Lists the project's AXAML files and shows the selected one. Reloads it on save.
/// Keeps the last good render when a load fails.
/// </summary>
/// <remarks>
/// The <see cref="ProjectPreview"/> owns the preview logic. The window keeps the layout, the settings file, and the shutdown.
/// On a stop request of the preview, the window writes the hand-off file and stops the host.
/// </remarks>
internal sealed class PreviewWindow : Window
{
    private const double SidebarWidth = 280;

    private static readonly FontFamily monospace = new("Cascadia Mono, Consolas, Menlo, DejaVu Sans Mono, monospace");
    private static readonly StringComparison pathComparison =
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private readonly HostOptions options;
    private readonly ProjectSet projects;
    private readonly ProjectPreview preview;
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
    private readonly WindowPlacementTracker? placementTracker;
    private bool isSyncingSelection;

    public PreviewWindow(HostOptions options, ProjectSet projects, IReadOnlyList<string> notes, DesignServices services, AppStyles appStyles)
    {
        this.options = options;
        this.projects = projects;
        scope = new PreviewScope(appStyles.CreateCopy());
        host.Content = scope;

        Icon = AppIcon.Get();
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
        preview = new ProjectPreview(options, projects, services, appStyles, settings.ViewModels);
        preview.Changed += (_, _) => ShowPreview();
        preview.Failed += (_, message) => ShowError(message);
        preview.StopRequested += (_, request) => StopWithHandOff(request);
        viewModelSwitch = new ViewModelSwitch(preview.ViewModelTypes);
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
        preview.Dispose();
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

        Button aboutButton = new() { Content = "About" };
        aboutButton.Click += async (_, _) => await new AboutDialog(AboutInfo.Read(typeof(AboutInfo).Assembly)).ShowDialog(this);
        DockPanel.SetDock(aboutButton, Dock.Right);

        Border toolbar = new()
        {
            Child = new DockPanel
            {
                Margin = new Thickness(8, 4),
                Children =
                {
                    aboutButton,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            themeSwitch,
                            sizeSwitch,
                            new TextBlock { Text = "DataContext", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center },
                            viewModelSwitch,
                        },
                    },
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

        preview.SelectViewModel(viewModelSwitch.SelectedType);
        UpdateSettings(settings => settings with { ViewModels = new Dictionary<string, string>(preview.ViewModels) });
    }

    private void RefreshCatalog()
    {
        preview.RefreshCatalog();
        ShowCatalog();
    }

    private void ShowCatalog()
    {
        string text = filter.Text ?? string.Empty;
        List<string> items = [.. preview.Catalog
            .Select(RelativePath)
            .Where(p => p.Contains(text, StringComparison.OrdinalIgnoreCase))];

        isSyncingSelection = true;
        fileList.ItemsSource = items;
        string shown = RelativePath(preview.FilePath);
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
        if (path is not null)
        {
            preview.Open(path);
            ShowShownFile();
        }
    }

    // The shutdown does not close the window. Thus save the settings first, so the next host opens at the same place.
    // The preview gives a stop request only when the options have a hand-off file.
    private void StopWithHandOff(StopRequest request)
    {
        SaveSettings();
        new HandOff(request.NextFile).Save(options.HandOffFile!);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown((int)request.Reason);
        }
    }

    private void Show(string path)
    {
        preview.Show(path);
        ShowShownFile();
    }

    // Show the title, the ViewModel, and the list selection of the shown file.
    private void ShowShownFile()
    {
        Title = $"AxView - {RelativePath(preview.FilePath)}";
        isSyncingSelection = true;
        viewModelSwitch.SelectedTypeName = preview.ViewModelName;
        isSyncingSelection = false;
        ShowCatalog();
    }

    private void ShowPreview()
    {
        scope.Content = preview.View;
        noDataContextBadge.IsVisible = preview.View is { DataContext: null } && !preview.IsStylePreview;
        if (preview.Error is not null)
        {
            ShowError(preview.Error);
            return;
        }

        HideError();
        status.Text = $"Rendered {DateTime.Now:HH:mm:ss}   {preview.FilePath}   Output: {OutputDirectory()}";
        Console.WriteLine($"axview: rendered {RelativePath(preview.FilePath)}");
    }

    private void ShowError(string message)
    {
        errorText.Text = message;
        errorPanel.IsVisible = true;
        Console.Error.WriteLine($"axview: error: {errorText.Text}");
        status.Text = preview.View is null
            ? $"Error {DateTime.Now:HH:mm:ss}   {preview.FilePath}"
            : $"Error {DateTime.Now:HH:mm:ss}. Showing the last good render.   {preview.FilePath}";
    }

    // The app output folder shows which build the preview uses, for example Debug or Release.
    private string OutputDirectory() =>
        Path.GetDirectoryName(projects.ViewAssembly.Location) ?? string.Empty;

    private void HideError() => errorPanel.IsVisible = false;

    private string RelativePath(string path) =>
        Path.GetRelativePath(options.ProjectDirectory, path);
}
