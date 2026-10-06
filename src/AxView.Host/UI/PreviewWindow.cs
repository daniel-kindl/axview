using System.Diagnostics.CodeAnalysis;

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
/// The <see cref="PreviewWindowModel"/> owns the state. The window keeps the layout, and shows the model after each change.
/// </remarks>
[SuppressMessage("Design", "CA1001", Justification = "OnClosed disposes the model. A window has no Dispose.")]
internal sealed class PreviewWindow : Window
{
    private const double SidebarWidth = 280;

    private static readonly FontFamily monospace = new("Cascadia Mono, Consolas, Menlo, DejaVu Sans Mono, monospace");

    private readonly HostOptions options;
    private readonly PreviewWindowModel model;
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

    public PreviewWindow(HostOptions options, ProjectSet projects, IReadOnlyList<string> notes, DesignServices services, AppStyles appStyles)
    {
        this.options = options;
        model = new PreviewWindowModel(options, projects, services, appStyles, AddWindowState, Shutdown);
        scope = new PreviewScope(model.Styles);
        host.Content = scope;

        Icon = AppIcon.Get();
        Width = 1280;
        Height = 800;
        Resources.MergedDictionaries.Add(ChromeStyle.CreateResources());
        Settings settings = model.Settings;
        if (settings.Window is not null)
        {
            WindowPlacementTracker.Apply(this, settings.Window);
        }

        // Without a settings file, the window has no placement to keep.
        placementTracker = options.SettingsFile is null ? null : new WindowPlacementTracker(this);
        themeSwitch = new ThemeSwitch(scope) { SelectedItem = settings.Theme };
        sizeSwitch = new PreviewSizeSwitch(host) { Size = settings.Size };
        viewModelSwitch = new ViewModelSwitch(model.ViewModelTypes);
        viewModelSwitch.SelectionChanged += (_, _) => model.SelectViewModel(viewModelSwitch.SelectedType);

        status = new TextBlock
        {
            Margin = new Thickness(ChromeStyle.SpacingMedium, ChromeStyle.SpacingSmall),
            FontSize = ChromeStyle.FontSizeNormal,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        ToolTip.SetTip(status, string.Join(Environment.NewLine, notes));
        TextBlock noDataContextText = new() { Text = "No DataContext", FontSize = ChromeStyle.FontSizeSmall };
        ChromeStyle.SetBrush(noDataContextText, TextBlock.ForegroundProperty, ChromeStyle.StatusForegroundBrushKey);
        noDataContextBadge = new Border
        {
            IsVisible = false,
            Margin = new Thickness(ChromeStyle.SpacingMedium, ChromeStyle.SpacingExtraSmall),
            Padding = new Thickness(6, 1),
            CornerRadius = new CornerRadius(ChromeStyle.CornerRadius),
            Child = noDataContextText,
        };
        ChromeStyle.SetBrush(noDataContextBadge, Border.BackgroundProperty, ChromeStyle.WarningBrushKey);
        ToolTip.SetTip(noDataContextBadge, "Select a ViewModel, set Design.DataContext, or make x:DataType creatable. See AxViewDesign.CreateServices.");
        DockPanel.SetDock(noDataContextBadge, Dock.Right);

        errorText = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontFamily = monospace,
            FontSize = ChromeStyle.FontSizeNormal,
        };
        ChromeStyle.SetBrush(errorText, TextBlock.ForegroundProperty, ChromeStyle.StatusForegroundBrushKey);
        errorPanel = new Border
        {
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Bottom,
            MaxHeight = 320,
            Padding = new Thickness(ChromeStyle.SpacingLarge),
            Child = new ScrollViewer { Content = errorText },
        };
        ChromeStyle.SetBrush(errorPanel, Border.BackgroundProperty, ChromeStyle.ErrorBrushKey);

        filter = new TextBox { Watermark = "Filter", Margin = new Thickness(8) };
        filter.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                model.SetFilter(filter.Text ?? string.Empty);
            }
        };

        fileList = new ListBox { ItemTemplate = new FuncDataTemplate<FileListEntry>((entry, _) => FileItem(entry)) };
        fileList.SelectionChanged += (_, _) =>
        {
            if (fileList.SelectedItem is FileListEntry entry)
            {
                model.Select(entry);
            }
        };

        Button openButton = new() { Content = "Open...", Margin = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Stretch };
        openButton.Click += async (_, _) => await OpenAsync();

        Control sidebar = BuildSidebar(openButton);
        Control stage = BuildStage();
        Border statusBar = new()
        {
            Child = new DockPanel { Children = { noDataContextBadge, status } },
            BorderThickness = new Thickness(0, 1, 0, 0),
        };
        ChromeStyle.SetBrush(statusBar, Border.BorderBrushProperty, ChromeStyle.BorderBrushKey);
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
        Activated += (_, _) => model.RefreshCatalog();
        model.Changed += (_, _) => ShowModel();
        ShowModel();
    }

    // Save before the window closes. A closed window has no position.
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (!e.Cancel)
        {
            model.SaveSettings();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        model.Dispose();
        base.OnClosed(e);
    }

    private static void Shutdown(int exitCode)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown(exitCode);
        }
    }

    // The title first, so a long folder path does not hide the file name.
    private static StackPanel FileItem(FileListEntry? entry)
    {
        StackPanel item = new() { Spacing = 1 };
        if (entry is null)
        {
            return item;
        }

        item.Children.Add(new TextBlock { Text = entry.Title, TextTrimming = TextTrimming.CharacterEllipsis });
        item.Children.Add(new TextBlock { Text = entry.Detail, FontSize = 11, Opacity = 0.6, TextTrimming = TextTrimming.CharacterEllipsis });
        if (entry.RelativePath is not null)
        {
            ToolTip.SetTip(item, entry.RelativePath);
        }

        return item;
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
        };
        ChromeStyle.SetBrush(toolbar, Border.BorderBrushProperty, ChromeStyle.BorderBrushKey);
        DockPanel.SetDock(toolbar, Dock.Top);
        return new DockPanel { Children = { toolbar, preview } };
    }

    // The model saves the settings. The window gives its placement, its theme, and its preview size.
    private Settings AddWindowState(Settings settings) =>
        placementTracker is null
            ? settings
            : settings with
            {
                Window = placementTracker.GetPlacement(),
                Theme = themeSwitch.SelectedItem is PreviewTheme theme ? theme : PreviewTheme.Default,
                Size = sizeSwitch.Size,
            };

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
            model.Open(path);
        }
    }

    // A change of a selection calls the model again. The model ignores the selection of the shown item.
    private void ShowModel()
    {
        Title = model.Title;
        if (!ReferenceEquals(fileList.ItemsSource, model.Entries))
        {
            fileList.ItemsSource = model.Entries;
        }

        fileList.SelectedItem = model.SelectedEntry;
        scope.Content = model.Content;
        noDataContextBadge.IsVisible = model.IsDataContextMissing;
        errorText.Text = model.Error;
        errorPanel.IsVisible = model.Error is not null;
        status.Text = model.Status;
        viewModelSwitch.IsEnabled = model.CanSelectViewModel;
        viewModelSwitch.SelectedTypeName = model.ViewModelName;
    }
}
