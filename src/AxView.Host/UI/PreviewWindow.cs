using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using AxView.Host.Catalog;
using AxView.Host.Rendering;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>
/// Lists the project's AXAML files and shows the selected one. Reloads it on save.
/// Keeps the last good render when a load fails.
/// </summary>
internal sealed class PreviewWindow : Window
{
    private const double SidebarWidth = 280;

    private static readonly FontFamily monospace = new("Cascadia Mono, Consolas, Menlo, DejaVu Sans Mono, monospace");
    private static readonly StringComparison pathComparison =
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private readonly HostOptions options;
    private readonly PreviewRenderer renderer;
    private readonly ContentControl host = new();
    private readonly Border errorPanel;
    private readonly SelectableTextBlock errorText;
    private readonly TextBlock status;
    private readonly Border noDataContextBadge;
    private readonly TextBox filter;
    private readonly ListBox fileList;
    private readonly Action? unsubscribeErrors;
    private IReadOnlyList<string> catalog = [];
    private string currentPath;
    private FileChangeWatcher? watcher;
    private object? lastGood;
    private bool isSyncingSelection;

    public PreviewWindow(HostOptions options, IReadOnlyList<string> notes)
    {
        this.options = options;
        currentPath = Path.GetFullPath(options.AxamlPath);
        renderer = new PreviewRenderer(Assembly.Load(options.AssemblyName), options.ProjectDirectory);

        Width = 1280;
        Height = 800;

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
        ToolTip.SetTip(noDataContextBadge, "Set Design.DataContext, or give x:DataType a parameterless constructor.");
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

        unsubscribeErrors = DispatcherErrors.Subscribe(OnUnhandledException);
        RefreshCatalog();
        Show(currentPath);
        if (options.SetupError is not null)
        {
            ShowError($"Cannot open the file: {options.SetupError}");
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (options.SnapshotPath is not null)
        {
            // Give layout, bindings, and image loads time to settle.
            _ = DispatcherTimer.RunOnce(SaveSnapshotAndExit, TimeSpan.FromMilliseconds(500));
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        watcher?.Dispose();
        unsubscribeErrors?.Invoke();
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
                Children = { new ThemeSwitch(this) },
            },
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = Brushes.Gray,
        };
        DockPanel.SetDock(toolbar, Dock.Top);
        return new DockPanel { Children = { toolbar, preview } };
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
        fileList.SelectedItem = items.FirstOrDefault(p => string.Equals(p, RelativePath(currentPath), pathComparison));
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
            ShowError(new InvalidOperationException($"'{path}' belongs to another project. Start axview with that file."));
            return;
        }

        new HandOff(Path.GetFullPath(path)).Save(options.HandOffFile);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown((int)ExitReason.SwitchProject);
        }
    }

    private void Show(string path)
    {
        currentPath = Path.GetFullPath(path);
        Title = $"AxView - {RelativePath(currentPath)}";
        watcher?.Dispose();
        watcher = new FileChangeWatcher(currentPath, Reload);
        ShowCatalog();
        host.Content = null;
        lastGood = null;
        Reload();
    }

    private void Reload()
    {
        try
        {
            Control view = renderer.Render(currentPath);
            host.Content = view;
            HideError();
            noDataContextBadge.IsVisible = view.DataContext is null;
            lastGood = view;
            status.Text = $"Rendered {DateTime.Now:HH:mm:ss}   {currentPath}";
            Console.WriteLine($"axview: rendered {RelativePath(currentPath)}");
        }
        catch (Exception ex)
        {
            // User XAML and its code can throw any exception. Show it and keep the last good render.
            ShowError(ex);
        }
    }

    // A load can succeed and then fail in layout or binding. Go back to the last good render.
    private void OnUnhandledException(Exception ex)
    {
        if (!ReferenceEquals(host.Content, lastGood))
        {
            host.Content = lastGood;
        }

        ShowError(ex);
    }

    private void SaveSnapshotAndExit()
    {
        PixelSize size = PixelSize.FromSize(ClientSize, RenderScaling);
        using (RenderTargetBitmap bitmap = new(size, new Vector(96 * RenderScaling, 96 * RenderScaling)))
        {
            bitmap.Render(this);
            bitmap.Save(options.SnapshotPath!);
        }

        ExitReason reason = errorPanel.IsVisible ? ExitReason.RenderFailed : ExitReason.Ok;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown((int)reason);
        }
    }

    private void ShowError(Exception ex)
    {
        Exception shown = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
        ShowError($"{shown.GetType().Name}: {shown.Message}");
    }

    private void ShowError(string message)
    {
        errorText.Text = message;
        errorPanel.IsVisible = true;
        Console.Error.WriteLine($"axview: error: {errorText.Text}");
        status.Text = lastGood is null
            ? $"Error {DateTime.Now:HH:mm:ss}   {currentPath}"
            : $"Error {DateTime.Now:HH:mm:ss}. Showing the last good render.   {currentPath}";
    }

    private void HideError() => errorPanel.IsVisible = false;

    private string RelativePath(string path) =>
        Path.GetRelativePath(options.ProjectDirectory, path);
}
