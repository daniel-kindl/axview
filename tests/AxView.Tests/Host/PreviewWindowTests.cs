extern alias host;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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

    private static PreviewWindow CreateWindow(TempDir view)
    {
        string file = view.Write("Views/Main.axaml", TextBlockXaml);
        HostOptions options = new(file, view.Path, "AxView.Tests", null, "Loader.dll", null, null);
        ProjectSet projects = ProjectSet.Load(options);
        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, projects.CreateRenderers(DesignServices.None));
        return new PreviewWindow(options, projects, [], DesignServices.None, appStyles);
    }

    private static Panel GroupOf<TSwitch>(Window window)
        where TSwitch : Control
    {
        TSwitch control = window.GetLogicalDescendants().OfType<TSwitch>().Single();
        return Assert.IsType<Panel>(control.Parent, exactMatch: false);
    }
}
