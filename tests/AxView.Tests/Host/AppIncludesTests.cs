extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class AppIncludesTests
{
    private const string Xmlns = "xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";
    private const string AssemblyUri = "avares://AxView.Tests";
    private const string LibraryUri = "avares://Avalonia.Controls";
    private const string BrokenXaml = "<Styles";

    [AvaloniaFact]
    public void Update_ChangedStyleInclude_ReplacesTheStyles()
    {
        using TempDir dir = new();
        string app = WriteApp(dir, styles: $"""<StyleInclude Source="{AssemblyUri}/Styles/Main.axaml" />""");
        string main = dir.Write("Styles/Main.axaml", TagStyles("Before"));
        Styles styles = [LoadStyles(dir, main)];
        AppIncludes includes = new(Renderer(dir), app, styles, new ResourceDictionary());

        File.WriteAllText(main, TagStyles("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.Null(update.Error);
        Assert.Equal("After", TagOf(styles[0]));
    }

    [AvaloniaFact]
    public void Update_NoChange_KeepsTheCompiledStyles()
    {
        using TempDir dir = new();
        string app = WriteApp(dir, styles: $"""<StyleInclude Source="/Styles/Main.axaml" />""");
        string main = dir.Write("Styles/Main.axaml", TagStyles("Before"));
        IStyle compiled = LoadStyles(dir, main);
        Styles styles = [compiled];
        AppIncludes includes = new(Renderer(dir), app, styles, new ResourceDictionary());

        IncludeUpdate update = includes.Update();

        Assert.False(update.IsChanged);
        Assert.Same(compiled, styles[0]);
    }

    [AvaloniaFact]
    public void Update_ChangedResourceInclude_ReplacesTheMergedDictionary()
    {
        using TempDir dir = new();
        string app = WriteApp(dir, resources: $"""
            <ResourceDictionary>
              <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="{AssemblyUri}/Styles/Texts.axaml" />
              </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
            """);
        string texts = dir.Write("Styles/Texts.axaml", TextResources("Before"));
        ResourceDictionary resources = new() { MergedDictionaries = { LoadResources(dir, texts) } };
        AppIncludes includes = new(Renderer(dir), app, [], resources);

        File.WriteAllText(texts, TextResources("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.True(resources.TryGetResource("Text", null, out object? text));
        Assert.Equal("After", text);
    }

    [AvaloniaFact]
    public void Update_ChangedThemeInclude_ReplacesTheThemeDictionary()
    {
        using TempDir dir = new();
        string app = WriteApp(dir, resources: $"""
            <ResourceDictionary>
              <ResourceDictionary.ThemeDictionaries>
                <ResourceInclude x:Key="Dark" Source="{AssemblyUri}/Styles/Dark.axaml" />
              </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
            """);
        string dark = dir.Write("Styles/Dark.axaml", TextResources("Before"));
        ResourceDictionary resources = new() { ThemeDictionaries = { [ThemeVariant.Dark] = LoadResources(dir, dark) } };
        AppIncludes includes = new(Renderer(dir), app, [], resources);

        File.WriteAllText(dark, TextResources("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.True(resources.TryGetResource("Text", ThemeVariant.Dark, out object? text));
        Assert.Equal("After", text);
    }

    [AvaloniaFact]
    public void Update_ChangedNestedInclude_ReplacesTheStylesInsideTheParent()
    {
        using TempDir dir = new();
        string app = WriteApp(dir, styles: $"""<StyleInclude Source="{AssemblyUri}/Styles/Main.axaml" />""");
        _ = dir.Write("Styles/Main.axaml", $"""
            <Styles {Xmlns}>
              <StyleInclude Source="Buttons.axaml" />
            </Styles>
            """);
        string buttons = dir.Write("Styles/Buttons.axaml", TagStyles("Before"));
        Styles main = [LoadStyles(dir, buttons)];
        Styles styles = [main];
        AppIncludes includes = new(Renderer(dir), app, styles, new ResourceDictionary());

        File.WriteAllText(buttons, TagStyles("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.Same(main, styles[0]);
        Assert.Equal("After", TagOf(main[0]));
    }

    [AvaloniaFact]
    public void Update_BrokenInclude_KeepsTheOldStylesAndGivesTheError()
    {
        using TempDir dir = new();
        string app = WriteApp(dir, styles: $"""<StyleInclude Source="{AssemblyUri}/Styles/Main.axaml" />""");
        string main = dir.Write("Styles/Main.axaml", TagStyles("Before"));
        Styles styles = [LoadStyles(dir, main)];
        AppIncludes includes = new(Renderer(dir), app, styles, new ResourceDictionary());

        File.WriteAllText(main, BrokenXaml);
        IncludeUpdate update = includes.Update();

        Assert.False(update.IsChanged);
        Assert.Contains("Main.axaml", update.Error);
        Assert.Equal("Before", TagOf(styles[0]));
    }

    [AvaloniaFact]
    public void Update_FileChangedAgain_ReplacesTheLoadedStyles()
    {
        using TempDir dir = new();
        string app = WriteApp(dir, styles: $"""<StyleInclude Source="{AssemblyUri}/Styles/Main.axaml" />""");
        string main = dir.Write("Styles/Main.axaml", TagStyles("Before"));
        Styles styles = [LoadStyles(dir, main)];
        AppIncludes includes = new(Renderer(dir), app, styles, new ResourceDictionary());
        File.WriteAllText(main, TagStyles("First"));
        _ = includes.Update();

        File.WriteAllText(main, TagStyles("Second"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.Equal("Second", TagOf(styles[0]));
    }

    [AvaloniaFact]
    public void Update_AppDoesNotAgreeWithTheRuntimeStyles_ChangesNothing()
    {
        using TempDir dir = new();
        string app = WriteApp(dir, styles: $"""<StyleInclude Source="{AssemblyUri}/Styles/Main.axaml" />""");
        string main = dir.Write("Styles/Main.axaml", TagStyles("Before"));
        Styles styles = [];
        AppIncludes includes = new(Renderer(dir), app, styles, new ResourceDictionary());

        File.WriteAllText(main, TagStyles("After"));
        IncludeUpdate update = includes.Update();

        Assert.False(update.IsChanged);
        Assert.Empty(styles);
    }

    [Fact]
    public void FindAppFile_GivesTheFileWithTheApplicationRoot()
    {
        using TempDir dir = new();
        _ = dir.Write("Views/Main.axaml", $"""<UserControl {Xmlns} />""");
        string app = WriteApp(dir);

        Assert.Equal(app, AppIncludes.FindAppFile(dir.Path));
    }

    [Fact]
    public void FindAppFile_NoApplication_GivesNull()
    {
        using TempDir dir = new();
        _ = dir.Write("Views/Main.axaml", $"""<UserControl {Xmlns} />""");

        Assert.Null(AppIncludes.FindAppFile(dir.Path));
    }

    [AvaloniaFact]
    public void Update_IncludeInAnotherAssembly_LoadsTheFileOfThatProject()
    {
        using TempDir appDir = new();
        using TempDir libraryDir = new();
        string app = WriteApp(appDir, styles: $"""<StyleInclude Source="{LibraryUri}/Styles/Main.axaml" />""");
        string main = libraryDir.Write("Styles/Main.axaml", TagStyles("Before"));
        Styles styles = [Assert.IsAssignableFrom<IStyle>(LibraryRenderer(libraryDir).Load(main, File.ReadAllText(main)))];
        AppIncludes includes = new(Renderer(appDir), app, styles, new ResourceDictionary(), [LibraryRenderer(libraryDir)]);

        File.WriteAllText(main, TagStyles("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.Null(update.Error);
        Assert.Equal("After", TagOf(styles[0]));
    }

    [AvaloniaFact]
    public void Update_IncludeOfAFileInAnotherAssembly_ResolvesItsIncludesInThatAssembly()
    {
        using TempDir appDir = new();
        using TempDir libraryDir = new();
        string app = WriteApp(appDir, styles: $"""<StyleInclude Source="{LibraryUri}/Styles/Main.axaml" />""");
        string main = libraryDir.Write("Styles/Main.axaml", $"""
            <Styles {Xmlns}>
              <StyleInclude Source="/Styles/Inner.axaml" />
            </Styles>
            """);
        string inner = libraryDir.Write("Styles/Inner.axaml", TagStyles("Before"));
        IStyle compiledInner = Assert.IsAssignableFrom<IStyle>(LibraryRenderer(libraryDir).Load(inner, File.ReadAllText(inner)));
        Styles styles = [new Styles { compiledInner }];
        AppIncludes includes = new(Renderer(appDir), app, styles, new ResourceDictionary(), [LibraryRenderer(libraryDir)]);

        File.WriteAllText(inner, TagStyles("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.Null(update.Error);
        Assert.Equal("After", TagOf(Assert.IsType<Styles>(styles[0])[0]));
    }

    [AvaloniaFact]
    public void Update_ChangedStyleIncludeOfAView_ReplacesTheStylesOfTheView()
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
        UserControl root = new() { Styles = { LoadStyles(dir, main) } };
        AppIncludes includes = new(Renderer(dir), view, root);

        File.WriteAllText(main, TagStyles("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.Null(update.Error);
        Assert.Equal("After", TagOf(root.Styles[0]));
    }

    [AvaloniaFact]
    public void Update_ChangedResourceIncludeOfAView_ReplacesTheMergedDictionaryOfTheView()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", $"""
            <UserControl {Xmlns}>
              <UserControl.Resources>
                <ResourceDictionary>
                  <ResourceDictionary.MergedDictionaries>
                    <ResourceInclude Source="/Styles/Texts.axaml" />
                  </ResourceDictionary.MergedDictionaries>
                </ResourceDictionary>
              </UserControl.Resources>
            </UserControl>
            """);
        string texts = dir.Write("Styles/Texts.axaml", TextResources("Before"));
        UserControl root = new() { Resources = { MergedDictionaries = { LoadResources(dir, texts) } } };
        AppIncludes includes = new(Renderer(dir), view, root);

        File.WriteAllText(texts, TextResources("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.True(root.TryFindResource("Text", out object? text));
        Assert.Equal("After", text);
    }

    [AvaloniaFact]
    public void Update_ChangedResourceIncludeOfAStyleRoot_ReplacesTheMergedDictionaryOfTheStyle()
    {
        using TempDir dir = new();
        string file = dir.Write("Styles/Button.axaml", $"""
            <Style {Xmlns} Selector="Button">
              <Style.Resources>
                <ResourceDictionary>
                  <ResourceDictionary.MergedDictionaries>
                    <ResourceInclude Source="Texts.axaml" />
                  </ResourceDictionary.MergedDictionaries>
                </ResourceDictionary>
              </Style.Resources>
            </Style>
            """);
        string texts = dir.Write("Styles/Texts.axaml", TextResources("Before"));
        Style root = new() { Resources = { MergedDictionaries = { LoadResources(dir, texts) } } };
        AppIncludes includes = new(Renderer(dir), file, root);

        File.WriteAllText(texts, TextResources("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.True(root.TryGetResource("Text", null, out object? text));
        Assert.Equal("After", text);
    }

    [AvaloniaFact]
    public void Update_ChangedIncludeOfAStylesRoot_ReplacesTheStylesOfTheRoot()
    {
        using TempDir dir = new();
        string file = dir.Write("Styles/All.axaml", $"""
            <Styles {Xmlns}>
              <StyleInclude Source="Main.axaml" />
            </Styles>
            """);
        string main = dir.Write("Styles/Main.axaml", TagStyles("Before"));
        Styles root = [LoadStyles(dir, main)];
        AppIncludes includes = new(Renderer(dir), file, root);

        File.WriteAllText(main, TagStyles("After"));
        IncludeUpdate update = includes.Update();

        Assert.True(update.IsChanged);
        Assert.Equal("After", TagOf(root[0]));
    }

    private static string WriteApp(TempDir dir, string styles = "", string resources = "") => dir.Write("App.axaml", $"""
        <Application {Xmlns} x:Class="AxView.Tests.App">
          <Application.Resources>{resources}</Application.Resources>
          <Application.Styles>{styles}</Application.Styles>
        </Application>
        """);

    private static string TagStyles(string tag) => $"""
        <Styles {Xmlns}>
          <Style Selector="TextBlock">
            <Setter Property="Tag" Value="{tag}" />
          </Style>
        </Styles>
        """;

    private static string TextResources(string text) => $"""
        <ResourceDictionary {Xmlns}>
          <x:String x:Key="Text">{text}</x:String>
        </ResourceDictionary>
        """;

    private static object? TagOf(IStyle styles) =>
        Assert.IsType<Setter>(Assert.IsType<Style>(Assert.IsType<Styles>(styles)[0]).Setters[0]).Value;

    // The compiled XAML of the target app gives the same objects as the runtime loader.
    private static IStyle LoadStyles(TempDir dir, string path) =>
        Assert.IsAssignableFrom<IStyle>(Renderer(dir).Load(path, File.ReadAllText(path)));

    private static ResourceDictionary LoadResources(TempDir dir, string path) =>
        Assert.IsType<ResourceDictionary>(Renderer(dir).Load(path, File.ReadAllText(path)));

    private static PreviewRenderer Renderer(TempDir dir) => new(typeof(AppIncludesTests).Assembly, dir.Path, DesignServices.None);

    // Any other loaded assembly stands for the library project.
    private static PreviewRenderer LibraryRenderer(TempDir dir) =>
        new(typeof(Button).Assembly, dir.Path, DesignServices.None);
}
