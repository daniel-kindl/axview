extern alias host;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class PreviewWindowTests
{
    private const string TextBlockXaml = """<TextBlock xmlns="https://github.com/avaloniaui" Text="Hello" />""";

    [AvaloniaFact]
    public void Show_ShowsTheStateOfTheModel()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        HostOptions options = new(file, view.Path, "AxView.Tests", null, "Loader.dll", null, null)
        {
            SettingsFile = Path.Combine(view.Path, "settings.json"),
        };
        ProjectSet projects = ProjectSet.Load(options);
        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, projects.CreateRenderers(DesignServices.None));
        PreviewWindow window = new(options, projects, [], DesignServices.None, appStyles);

        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Close();

        Assert.Equal($"AxView - {Path.Combine("Views", "Main.axaml")}", window.Title);
        Assert.True(File.Exists(options.SettingsFile));
    }

    [AvaloniaFact]
    public void PreviewWindow_TakesEachBrushFromTheChromeStyle()
    {
        using TempDir view = new();
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        HostOptions options = new(file, view.Path, "AxView.Tests", null, "Loader.dll", null, null);
        ProjectSet projects = ProjectSet.Load(options);
        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, projects.CreateRenderers(DesignServices.None));

        PreviewWindow window = new(options, projects, [], DesignServices.None, appStyles) { RequestedThemeVariant = ThemeVariant.Dark };

        ChromeBrushes.AssertFromChromeStyle(window, ThemeVariant.Dark);
    }

    [AvaloniaFact]
    public void Toolbar_ShowsAboutAsAnIconButtonAtTheRightEdge()
    {
        using TempDir view = new();
        PreviewWindow window = CreateWindow(view);

        window.Show();
        Dispatcher.UIThread.RunJobs();
        Button about = window.GetLogicalDescendants().OfType<Button>().Single(button => ToolTip.GetTip(button) is "About AxView");
        Point? rightEdge = about.TranslatePoint(new Point(about.Bounds.Width, 0), window);
        window.Close();

        Point edge = Assert.NotNull(rightEdge);
        Assert.InRange(edge.X, window.ClientSize.Width - ChromeStyle.SpacingLarge, window.ClientSize.Width);
        Assert.InRange(rightEdge.Value.X, window.ClientSize.Width - ChromeStyle.SpacingLarge, window.ClientSize.Width);
    }

    [AvaloniaFact]
    public void Toolbar_ShowsEachGroupWithAnIconOrALabel_AndSeparators()
    {
        using TempDir view = new();
        PreviewWindow window = CreateWindow(view);

        Panel themeGroup = GroupOf<ThemeSwitch>(window);
        Panel sizeGroup = GroupOf<PreviewSizeSwitch>(window);
        Panel dataContextGroup = GroupOf<ViewModelSwitch>(window);

        Assert.Equal("Preview theme", ToolTip.GetTip(Assert.IsType<PathIcon>(themeGroup.Children[0])));
        Assert.Equal("Preview size", ToolTip.GetTip(Assert.IsType<PathIcon>(sizeGroup.Children[0])));
        Assert.Equal("DataContext", Assert.IsType<TextBlock>(dataContextGroup.Children[0]).Text);
        Panel groups = Assert.IsType<Panel>(themeGroup.Parent, exactMatch: false);
        Assert.Equal([themeGroup, sizeGroup, dataContextGroup], groups.Children.Where(child => child is not Border));
        Assert.Equal(2, groups.Children.OfType<Border>().Count());
        _ = Assert.IsType<Border>(groups.Children[1]);
        _ = Assert.IsType<Border>(groups.Children[3]);
    }

    [AvaloniaFact]
    public void Sidebar_ShowsTheOpenButtonAsAnIconButtonInTheHeader()
    {
        using TempDir view = new();
        PreviewWindow window = CreateWindow(view);

        Button open = ButtonOf(window, "Open an AXAML file");

        _ = Assert.IsType<PathIcon>(open.Content);
        DockPanel header = open.GetLogicalAncestors().OfType<DockPanel>().First();
        TextBlock projectName = header.GetLogicalDescendants().OfType<TextBlock>().Single();
        Assert.Equal(view.Path, ToolTip.GetTip(projectName));
    }

    [AvaloniaFact]
    public void Filter_ShowsASearchIcon()
    {
        using TempDir view = new();
        PreviewWindow window = CreateWindow(view);

        TextBox filter = window.GetLogicalDescendants().OfType<TextBox>().Single();

        _ = Assert.IsType<PathIcon>(filter.InnerLeftContent);
    }

    [AvaloniaFact]
    public void CollapseSidebar_HidesTheSidebar_AndExpandShowsItAgain()
    {
        using TempDir view = new();
        PreviewWindow window = CreateWindow(view);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        TextBox filter = window.GetLogicalDescendants().OfType<TextBox>().Single();
        Button expand = ButtonOf(window, "Expand the sidebar");
        bool isExpandShownFirst = IsShown(expand);

        ButtonOf(window, "Collapse the sidebar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        bool isFilterShownCollapsed = IsShown(filter);
        bool isExpandShownCollapsed = IsShown(expand);
        expand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        bool isFilterShownExpanded = IsShown(filter);
        bool isExpandShownExpanded = IsShown(expand);
        window.Close();

        Assert.False(isExpandShownFirst);
        Assert.False(isFilterShownCollapsed);
        Assert.True(isExpandShownCollapsed);
        Assert.True(isFilterShownExpanded);
        Assert.False(isExpandShownExpanded);
    }

    [AvaloniaFact]
    public void CollapsedSidebar_StaysCollapsedInTheNextWindow()
    {
        using TempDir view = new();
        string settingsFile = Path.Combine(view.Path, "settings.json");
        PreviewWindow first = CreateWindow(view, settingsFile);
        first.Show();
        Dispatcher.UIThread.RunJobs();
        ButtonOf(first, "Collapse the sidebar").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        first.Close();

        PreviewWindow next = CreateWindow(view, settingsFile);

        Assert.True(Settings.Load(settingsFile).IsSidebarCollapsed);
        Assert.False(IsShown(next.GetLogicalDescendants().OfType<TextBox>().Single()));
    }

    [AvaloniaFact]
    public void StatusBar_ShowsTheStatus_TheOutputFolder_AndTheBadge_WithSeparators()
    {
        using TempDir view = new();
        PreviewWindow window = CreateWindow(view);

        string outputDirectory = Path.GetDirectoryName(typeof(PreviewWindowTests).Assembly.Location)!;
        TextBlock output = window.GetLogicalDescendants().OfType<TextBlock>().Single(text => text.Text == $"Output: {outputDirectory}");
        Panel statusBar = Assert.IsType<Panel>(output.Parent, exactMatch: false);
        Border badge = statusBar.Children.OfType<Border>().Single(border => border.Child is TextBlock { Text: "No DataContext" });
        TextBlock status = statusBar.Children.OfType<TextBlock>().Single(text => text != output);

        Assert.StartsWith("Rendered", status.Text, StringComparison.Ordinal);
        Assert.Equal(outputDirectory, ToolTip.GetTip(output));
        Assert.Equal(ChromeStyle.PillCornerRadius, badge.CornerRadius.TopLeft);
        Assert.Equal(2, statusBar.Children.OfType<Border>().Count(border => border.Width == 1));
    }

    private static PreviewWindow CreateWindow(TempDir view, string? settingsFile = null)
    {
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        HostOptions options = new(file, view.Path, "AxView.Tests", null, "Loader.dll", null, null) { SettingsFile = settingsFile };
        ProjectSet projects = ProjectSet.Load(options);
        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, projects.CreateRenderers(DesignServices.None));
        return new PreviewWindow(options, projects, [], DesignServices.None, appStyles);
    }

    private static Button ButtonOf(Window window, string tip) =>
        window.GetLogicalDescendants().OfType<Button>().Single(button => Equals(ToolTip.GetTip(button), tip));

    // The test app has no theme. Thus IsEffectivelyVisible does not see a hidden logical parent in a template.
    private static bool IsShown(Control control) =>
        control.IsVisible && control.GetLogicalAncestors().OfType<Control>().All(ancestor => ancestor.IsVisible);

    private static Panel GroupOf<TSwitch>(Window window)
        where TSwitch : Control
    {
        TSwitch control = window.GetLogicalDescendants().OfType<TSwitch>().Single();
        return Assert.IsType<Panel>(control.Parent, exactMatch: false);
    }
}
