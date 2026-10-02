extern alias host;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;

namespace AxView.Tests.Host;

public sealed class AppStylesTests
{
    private const string Xmlns = "xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";
    private const string TextKey = "Text";

    [AvaloniaFact]
    public void Update_ChangedResourceInclude_UpdatesTheScopeAndTheApp()
    {
        using TempDir dir = new();
        string texts = WriteProject(dir, "Before");
        AppStyles appStyles = Create(dir);
        PreviewScope scope = new(appStyles.CreateCopy());
        Application app = new();
        appStyles.AddResourcesTo(app);

        File.WriteAllText(texts, TextResources("After"));
        IncludeUpdate update = appStyles.Update();

        Assert.True(update.IsChanged);
        Assert.Null(update.Error);
        Assert.True(scope.TryFindResource(TextKey, out object? scopeText));
        Assert.Equal("After", scopeText);
        Assert.True(app.TryGetResource(TextKey, null, out object? appText));
        Assert.Equal("After", appText);
    }

    [AvaloniaFact]
    public void Update_ChangedStyleInclude_UpdatesEachCopyAndTheAppStyles()
    {
        using TempDir dir = new();
        _ = WriteProject(dir, "Before");
        AppStyles appStyles = Create(dir);
        TargetStyles first = appStyles.CreateCopy();
        TargetStyles second = appStyles.CreateCopy();
        Application app = new();
        appStyles.AddTo(app);

        File.WriteAllText(Path.Combine(dir.Path, "Styles", "Main.axaml"), TagStyles("After"));
        IncludeUpdate update = appStyles.Update();

        Assert.True(update.IsChanged);
        Assert.Equal("After", TagOf(first.Styles[0]));
        Assert.Equal("After", TagOf(second.Styles[0]));
        Assert.Equal("After", TagOf(Assert.IsType<Styles>(app.Styles[0])[0]));
    }

    [AvaloniaFact]
    public void CreateCopy_GivesTheFirstImportFirstAndThenNewCopies()
    {
        using TempDir dir = new();
        _ = WriteProject(dir, "Before");
        TargetStyles firstImport = Import(dir);
        AppStyles appStyles = new(firstImport, () => Import(dir), [Renderer(dir)]);

        TargetStyles first = appStyles.CreateCopy();
        TargetStyles second = appStyles.CreateCopy();

        Assert.Same(firstImport, first);
        Assert.NotSame(first.Styles, second.Styles);
        Assert.NotSame(first.Resources, second.Resources);
    }

    [AvaloniaFact]
    public void Update_BrokenInclude_GivesTheErrorOnce()
    {
        using TempDir dir = new();
        string texts = WriteProject(dir, "Before");
        AppStyles appStyles = Create(dir);
        _ = appStyles.CreateCopy();
        _ = appStyles.CreateCopy();

        File.WriteAllText(texts, "<ResourceDictionary");
        IncludeUpdate update = appStyles.Update();

        Assert.False(update.IsChanged);
        string error = Assert.IsType<string>(update.Error);
        Assert.Single(error.Split(Environment.NewLine));
        Assert.Contains("Texts.axaml", error);
    }

    [AvaloniaFact]
    public void ProjectDirectories_GivesTheProjectsWhenOneHasAnAppFile()
    {
        using TempDir dir = new();
        _ = WriteProject(dir, "Before");

        AppStyles appStyles = Create(dir);

        Assert.Equal([dir.Path], appStyles.ProjectDirectories);
    }

    [AvaloniaFact]
    public void Update_LibraryView_LoadsTheIncludesOfTheAppFileInTheAppProject()
    {
        using TempDir library = new();
        using TempDir appProject = new();
        _ = library.Write("Views/Main.axaml", $"""<UserControl {Xmlns} />""");
        string texts = WriteProject(appProject, "Before");
        AppStyles appStyles = new(Import(appProject), () => Import(appProject), [Renderer(library), Renderer(appProject)]);
        TargetStyles copy = appStyles.CreateCopy();

        File.WriteAllText(texts, TextResources("After"));
        IncludeUpdate update = appStyles.Update();

        Assert.Equal([library.Path, appProject.Path], appStyles.ProjectDirectories);
        Assert.True(update.IsChanged);
        Assert.True(copy.Resources.TryGetResource(TextKey, null, out object? text));
        Assert.Equal("After", text);
    }

    [AvaloniaFact]
    public void Update_BothProjectsHaveAnAppFile_UsesTheAppFileOfTheFirstProject()
    {
        using TempDir first = new();
        using TempDir second = new();
        string texts = WriteProject(first, "Before");
        _ = second.Write("App.axaml", $"""<Application {Xmlns} x:Class="AxView.Tests.App" />""");
        AppStyles appStyles = new(Import(first), () => Import(first), [Renderer(first), Renderer(second)]);
        _ = appStyles.CreateCopy();

        File.WriteAllText(texts, TextResources("After"));

        Assert.True(appStyles.Update().IsChanged);
    }

    [AvaloniaFact]
    public void ProjectDirectories_NoAppFile_GivesTheProjects()
    {
        using TempDir dir = new();
        _ = dir.Write("Views/Main.axaml", $"""<UserControl {Xmlns} />""");

        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, [Renderer(dir)]);

        Assert.Equal([dir.Path], appStyles.ProjectDirectories);
        Assert.False(appStyles.Update().IsChanged);
    }

    [AvaloniaFact]
    public void CreateIncludes_IncludeChangedBeforeTheFirstUse_LoadsTheIncludeFromDisk()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", $"""
            <UserControl {Xmlns}>
              <UserControl.Styles>
                <StyleInclude Source="/Styles/Main.axaml" />
              </UserControl.Styles>
            </UserControl>
            """);
        string main = dir.Write("Styles/Main.axaml", TagStyles("Before"));
        AppStyles appStyles = new(TargetStyles.None, () => TargetStyles.None, [Renderer(dir)]);
        IStyle compiled = Assert.IsAssignableFrom<IStyle>(Renderer(dir).Load(main, File.ReadAllText(main)));
        UserControl root = new() { Styles = { compiled } };
        File.WriteAllText(main, TagStyles("After"));

        _ = appStyles.CreateIncludes(view, root);

        Assert.Equal("After", TagOf(root.Styles[0]));
    }

    [AvaloniaFact]
    public void Update_ChangedAppFile_LoadsTheAppFileIntoEachCopy()
    {
        using TempDir dir = new();
        string appPath = dir.Write("App.axaml", InlineApp("Before"));
        AppStyles appStyles = CreateWithAppType(dir, appPath);
        PreviewScope scope = new(appStyles.CreateCopy());
        TargetStyles second = appStyles.CreateCopy();

        File.WriteAllText(appPath, InlineApp("After"));
        IncludeUpdate update = appStyles.Update();

        Assert.True(update.IsChanged);
        Assert.Null(update.Error);
        Assert.True(scope.TryFindResource(TextKey, out object? scopeText));
        Assert.Equal("After", scopeText);
        Assert.Equal("After", TagOf(second.Styles[0]));
    }

    [AvaloniaFact]
    public void Update_BrokenAppFile_KeepsTheOldStylesAndGivesTheError()
    {
        using TempDir dir = new();
        string appPath = dir.Write("App.axaml", InlineApp("Before"));
        AppStyles appStyles = CreateWithAppType(dir, appPath);
        TargetStyles copy = appStyles.CreateCopy();

        File.WriteAllText(appPath, InlineApp("After").Replace("</Application>", "<Unknown /></Application>"));
        IncludeUpdate update = appStyles.Update();

        Assert.False(update.IsChanged);
        Assert.Contains("App.axaml", update.Error);
        Assert.Equal("Before", TagOf(copy.Styles[0]));
    }

    [AvaloniaFact]
    public void CreateCopy_AfterAnIncludeChange_GivesTheIncludeFromDisk()
    {
        using TempDir dir = new();
        string texts = WriteProject(dir, "Before");
        TargetStyles build = Import(dir);
        AppStyles appStyles = new(build, () => ImportText(dir, TextResources("Before")), [Renderer(dir)]);
        _ = appStyles.CreateCopy();
        File.WriteAllText(texts, TextResources("After"));
        _ = appStyles.Update();

        TargetStyles copy = appStyles.CreateCopy();

        Assert.True(copy.Resources.TryGetResource(TextKey, null, out object? text));
        Assert.Equal("After", text);
    }

    [AvaloniaFact]
    public void CreateCopy_AfterAnAppFileChange_GivesTheChangedAppFile()
    {
        using TempDir dir = new();
        string appPath = dir.Write("App.axaml", InlineApp("Before"));
        AppStyles appStyles = CreateWithAppType(dir, appPath);
        _ = appStyles.CreateCopy();
        File.WriteAllText(appPath, InlineApp("After"));
        _ = appStyles.Update();

        TargetStyles copy = appStyles.CreateCopy();

        Assert.Equal("After", TagOf(copy.Styles[0]));
    }

    // App.axaml with an inline style and an inline resource.
    private static string InlineApp(string text) => $"""
        <Application {Xmlns}>
          <Application.Resources>
            <x:String x:Key="{TextKey}">{text}</x:String>
          </Application.Resources>
          <Application.Styles>
            <Styles>
              <Style Selector="TextBlock">
                <Setter Property="Tag" Value="{text}" />
              </Style>
            </Styles>
          </Application.Styles>
        </Application>
        """;

    // The first import loads the text of the build, as the compiled App.axaml does.
    private static AppStyles CreateWithAppType(TempDir dir, string appPath)
    {
        PreviewRenderer renderer = Renderer(dir);
        Type appType = typeof(StartupFixtureApp);
        string buildText = File.ReadAllText(appPath);
        return new(
            TargetAppImporter.ImportXaml(appType, renderer, appPath, buildText),
            () => TargetAppImporter.ImportXaml(appType, renderer, appPath, buildText),
            [renderer],
            appType);
    }

    // Writes App.axaml with one style include and one resource include. Returns the resource include file.
    private static string WriteProject(TempDir dir, string text)
    {
        _ = dir.Write("App.axaml", $"""
            <Application {Xmlns} x:Class="AxView.Tests.App">
              <Application.Resources>
                <ResourceDictionary>
                  <ResourceDictionary.MergedDictionaries>
                    <ResourceInclude Source="/Styles/Texts.axaml" />
                  </ResourceDictionary.MergedDictionaries>
                </ResourceDictionary>
              </Application.Resources>
              <Application.Styles>
                <StyleInclude Source="/Styles/Main.axaml" />
              </Application.Styles>
            </Application>
            """);
        _ = dir.Write("Styles/Main.axaml", TagStyles(text));
        return dir.Write("Styles/Texts.axaml", TextResources(text));
    }

    private static AppStyles Create(TempDir dir) => new(Import(dir), () => Import(dir), [Renderer(dir)]);

    // The compiled XAML of the target app gives the same objects as the runtime loader.
    private static TargetStyles Import(TempDir dir) =>
        ImportText(dir, File.ReadAllText(Path.Combine(dir.Path, "Styles", "Texts.axaml")));

    // Imports the resource include with the given text of the build.
    private static TargetStyles ImportText(TempDir dir, string textsBuild)
    {
        PreviewRenderer renderer = Renderer(dir);
        string main = Path.Combine(dir.Path, "Styles", "Main.axaml");
        string texts = Path.Combine(dir.Path, "Styles", "Texts.axaml");
        IStyle style = Assert.IsAssignableFrom<IStyle>(renderer.Load(main, File.ReadAllText(main)));
        ResourceDictionary textResources = Assert.IsType<ResourceDictionary>(renderer.Load(texts, textsBuild));
        return new TargetStyles([style], new ResourceDictionary { MergedDictionaries = { textResources } }, [], null);
    }

    private static string TagStyles(string tag) => $"""
        <Styles {Xmlns}>
          <Style Selector="TextBlock">
            <Setter Property="Tag" Value="{tag}" />
          </Style>
        </Styles>
        """;

    private static string TextResources(string text) => $"""
        <ResourceDictionary {Xmlns}>
          <x:String x:Key="{TextKey}">{text}</x:String>
        </ResourceDictionary>
        """;

    private static object? TagOf(IStyle styles) =>
        Assert.IsType<Setter>(Assert.IsType<Style>(Assert.IsType<Styles>(styles)[0]).Setters[0]).Value;

    private static PreviewRenderer Renderer(TempDir dir) => new(typeof(AppStylesTests).Assembly, dir.Path, DesignServices.None);
}
