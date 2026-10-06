using System.Diagnostics.CodeAnalysis;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
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
    private const double OutputMaximumWidth = 480;

    private readonly HostOptions options;
    private readonly PreviewWindowModel model;
    private readonly ContentControl host = new();
    private readonly PreviewScope scope;
    private readonly PreviewStage stage;
    private readonly ErrorPanel errorPanel = new();
    private readonly TextBlock status;
    private readonly Border noDataContextBadge;
    private readonly Border noDataContextSeparator = ChromeControls.CreateSeparator();
    private readonly TextBox filter;
    private readonly ListBox fileList;
    private readonly ThemeSwitch themeSwitch;
    private readonly PreviewSizeSwitch sizeSwitch;
    private readonly ViewModelSwitch viewModelSwitch;
    private readonly StackPanel expandSidebarGroup;
    private readonly Control sidebar;
    private readonly GridSplitter splitter = new() { Width = 4, ResizeDirection = GridResizeDirection.Columns };
    private readonly ColumnDefinition sidebarColumn = new(SidebarWidth, GridUnitType.Pixel);
    private GridLength expandedSidebarWidth = new(SidebarWidth);
    private bool isSidebarCollapsed;
    private readonly WindowPlacementTracker? placementTracker;

    public PreviewWindow(HostOptions options, ProjectSet projects, IReadOnlyList<string> notes, DesignServices services, AppStyles appStyles)
    {
        this.options = options;
        model = new PreviewWindowModel(options, projects, services, appStyles, AddWindowState, Shutdown);
        scope = new PreviewScope(model.Styles);
        host.Content = scope;
        stage = new PreviewStage(host);

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
        sizeSwitch = new PreviewSizeSwitch(stage) { Size = settings.Size };
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
            Margin = new Thickness(0, ChromeStyle.SpacingExtraSmall),
            Padding = new Thickness(ChromeStyle.SpacingMedium, 1),
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(ChromeStyle.PillCornerRadius),
            Child = noDataContextText,
        };
        ChromeStyle.SetBrush(noDataContextBadge, Border.BackgroundProperty, ChromeStyle.WarningBrushKey);
        ToolTip.SetTip(noDataContextBadge, "Select a ViewModel, set Design.DataContext, or make x:DataType creatable. See AxViewDesign.CreateServices.");
        DockPanel.SetDock(noDataContextBadge, Dock.Right);

        PathIcon searchIcon = ChromeControls.CreateIcon(ChromeIcon.Search);
        searchIcon.Margin = new Thickness(ChromeStyle.SpacingMedium, 0, ChromeStyle.SpacingSmall, 0);
        ChromeStyle.SetBrush(searchIcon, PathIcon.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        filter = new TextBox { Watermark = "Filter", Margin = new Thickness(ChromeStyle.SpacingMedium), InnerLeftContent = searchIcon };
        filter.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                model.SetFilter(filter.Text ?? string.Empty);
            }
        };

        fileList = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<FileListEntry>((entry, _) => entry is null ? new Panel() : new FileRow(entry)),
            Styles = { FileRow.CreateSelectionStyles() },
        };
        // A long folder trims. It does not scroll the list to the side.
        ScrollViewer.SetHorizontalScrollBarVisibility(fileList, ScrollBarVisibility.Disabled);
        fileList.SelectionChanged += (_, _) =>
        {
            if (fileList.SelectedItem is FileListEntry entry)
            {
                model.Select(entry);
            }
        };

        Button expandSidebarButton = CreateSidebarButton(ChromeIcon.Collapse, "Expand the sidebar", -90);
        expandSidebarButton.Click += (_, _) => SetSidebarCollapsed(false);
        expandSidebarGroup = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { expandSidebarButton, ChromeControls.CreateSeparator() },
        };
        sidebar = BuildSidebar();
        Control previewArea = BuildPreviewArea();
        TextBlock output = new()
        {
            Text = $"Output: {model.OutputDirectory}",
            MaxWidth = OutputMaximumWidth,
            Margin = new Thickness(0, ChromeStyle.SpacingSmall),
            FontSize = ChromeStyle.FontSizeNormal,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        ChromeStyle.SetBrush(output, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        ToolTip.SetTip(output, model.OutputDirectory);
        Border outputSeparator = ChromeControls.CreateSeparator();
        foreach (Control segment in new Control[] { noDataContextSeparator, output, outputSeparator })
        {
            DockPanel.SetDock(segment, Dock.Right);
        }

        Border statusBar = new()
        {
            // The first segment with a right dock is at the right edge.
            Child = new DockPanel
            {
                Margin = new Thickness(0, 0, ChromeStyle.SpacingMedium, 0),
                Children = { noDataContextBadge, noDataContextSeparator, output, outputSeparator, status },
            },
            BorderThickness = new Thickness(0, 1, 0, 0),
        };
        ChromeStyle.SetBrush(statusBar, Border.BorderBrushProperty, ChromeStyle.BorderBrushKey);
        DockPanel.SetDock(statusBar, Dock.Bottom);

        Grid body = new()
        {
            ColumnDefinitions = { sidebarColumn, new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
        };
        body.Children.Add(sidebar);
        Grid.SetColumn(splitter, 1);
        body.Children.Add(splitter);
        Grid.SetColumn(previewArea, 2);
        body.Children.Add(previewArea);

        Content = new DockPanel { Children = { statusBar, body } };
        SetSidebarCollapsed(settings.IsSidebarCollapsed);

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

    // The chevron of the collapse icon points down. The angle turns it to the side.
    private static Button CreateSidebarButton(ChromeIcon icon, string tip, double angle)
    {
        Button button = ChromeControls.CreateIconButton(icon, tip);
        if (button.Content is PathIcon pathIcon)
        {
            pathIcon.RenderTransform = new RotateTransform(angle);
        }

        return button;
    }

    // The label of a group is an icon or a text in front of its control.
    private static StackPanel ToolbarGroup(Control label, Control control) =>
        new() { Orientation = Orientation.Horizontal, Spacing = ChromeStyle.SpacingMedium, Children = { label, control } };

    private static PathIcon ToolbarIcon(ChromeIcon icon, string tip)
    {
        PathIcon label = ChromeControls.CreateIcon(icon);
        label.VerticalAlignment = VerticalAlignment.Center;
        ToolTip.SetTip(label, tip);
        ChromeStyle.SetBrush(label, PathIcon.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        return label;
    }

    private DockPanel BuildSidebar()
    {
        TextBlock projectName = new()
        {
            Text = Path.GetFileName(options.ProjectDirectory.TrimEnd(Path.DirectorySeparatorChar)),
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        ToolTip.SetTip(projectName, options.ProjectDirectory);
        Button openButton = ChromeControls.CreateIconButton(ChromeIcon.Open, "Open an AXAML file");
        openButton.Click += async (_, _) => await OpenAsync();
        Button collapseButton = CreateSidebarButton(ChromeIcon.Collapse, "Collapse the sidebar", 90);
        collapseButton.Click += (_, _) => SetSidebarCollapsed(true);
        StackPanel buttons = new() { Orientation = Orientation.Horizontal, Children = { openButton, collapseButton } };
        DockPanel.SetDock(buttons, Dock.Right);
        DockPanel header = new()
        {
            Margin = new Thickness(ChromeStyle.SpacingMedium, ChromeStyle.SpacingSmall, ChromeStyle.SpacingSmall, 0),
            Children = { buttons, projectName },
        };

        DockPanel sidebar = new();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(filter, Dock.Top);
        sidebar.Children.Add(header);
        sidebar.Children.Add(filter);
        sidebar.Children.Add(fileList);
        return sidebar;
    }

    private DockPanel BuildPreviewArea()
    {
        Grid preview = new();
        preview.Children.Add(stage);
        preview.Children.Add(errorPanel);

        Button aboutButton = ChromeControls.CreateIconButton(ChromeIcon.About, "About AxView");
        aboutButton.Click += async (_, _) => await new AboutDialog(AboutInfo.Read(typeof(AboutInfo).Assembly)).ShowDialog(this);
        DockPanel.SetDock(aboutButton, Dock.Right);
        DockPanel.SetDock(expandSidebarGroup, Dock.Left);

        TextBlock dataContextLabel = new() { Text = "DataContext", VerticalAlignment = VerticalAlignment.Center };
        ChromeStyle.SetBrush(dataContextLabel, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        Border toolbar = new()
        {
            Child = new DockPanel
            {
                Margin = new Thickness(ChromeStyle.SpacingMedium, ChromeStyle.SpacingSmall),
                Children =
                {
                    aboutButton,
                    expandSidebarGroup,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            ToolbarGroup(ToolbarIcon(ChromeIcon.Theme, "Preview theme"), themeSwitch),
                            ChromeControls.CreateSeparator(),
                            ToolbarGroup(ToolbarIcon(ChromeIcon.Size, "Preview size"), sizeSwitch),
                            ChromeControls.CreateSeparator(),
                            ToolbarGroup(dataContextLabel, viewModelSwitch),
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
                IsSidebarCollapsed = isSidebarCollapsed,
            };

    // The splitter changes the width of the sidebar. The expand gives back the last width.
    private void SetSidebarCollapsed(bool isCollapsed)
    {
        if (isCollapsed && !isSidebarCollapsed)
        {
            expandedSidebarWidth = sidebarColumn.Width;
        }

        isSidebarCollapsed = isCollapsed;
        sidebar.IsVisible = !isCollapsed;
        splitter.IsVisible = !isCollapsed;
        expandSidebarGroup.IsVisible = isCollapsed;
        sidebarColumn.Width = isCollapsed ? new GridLength(0) : expandedSidebarWidth;
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
        noDataContextSeparator.IsVisible = model.IsDataContextMissing;
        errorPanel.Show(model.Error, model.IsLastGoodRenderShown);
        status.Text = model.Status;
        viewModelSwitch.IsEnabled = model.CanSelectViewModel;
        viewModelSwitch.SelectedTypeName = model.ViewModelName;
    }
}
