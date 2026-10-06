extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class ChangedIncludesTests
{
    private const string Xmlns = "xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";
    private const string LibraryUri = "avares://Avalonia.Controls";
    private const string BrokenXaml = "<ResourceDictionary";

    [AvaloniaFact]
    public void Render_ChangedResourceInclude_GivesTheChangedValueToAStaticResource()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", TextView("/Styles/Texts.axaml"));
        string texts = dir.Write("Styles/Texts.axaml", TextResources("After"));
        ChangedIncludes includes = new([Renderer(dir)], Build(texts, TextResources("Before")));

        Control control = Renderer(dir).Render(view, includes: includes).Control;

        Assert.Equal("After", TextOf(control));
        Assert.Null(includes.Error);
    }

    [AvaloniaFact]
    public void Render_ChangedStyleIncludeOfAChildControl_GivesTheChangedStylesToTheChild()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", $"""
            <UserControl {Xmlns}>
              <Border>
                <Border.Styles>
                  <StyleInclude Source="../Styles/Main.axaml" />
                </Border.Styles>
              </Border>
            </UserControl>
            """);
        string main = dir.Write("Styles/Main.axaml", TagStyles("After"));
        ChangedIncludes includes = new([Renderer(dir)], Build(main, TagStyles("Before")));

        Control control = Renderer(dir).Render(view, includes: includes).Control;

        Border border = Assert.IsType<Border>(Assert.IsType<UserControl>(control).Content);
        Assert.Equal("After", TagOf(border.Styles[0]));
    }

    [AvaloniaFact]
    public void Render_ChangedIncludeInAnotherAssembly_LoadsTheFileOfThatProject()
    {
        using TempDir dir = new();
        using TempDir libraryDir = new();
        string view = dir.Write("Views/Main.axaml", TextView($"{LibraryUri}/Styles/Texts.axaml"));
        string texts = libraryDir.Write("Styles/Texts.axaml", TextResources("After"));
        ChangedIncludes includes = new([Renderer(dir), LibraryRenderer(libraryDir)], Build(texts, TextResources("Before")));

        Control control = Renderer(dir).Render(view, includes: includes).Control;

        Assert.Equal("After", TextOf(control));
    }

    [AvaloniaFact]
    public void Render_ChangedNestedInclude_LoadsTheChangedFileInsideTheParentFile()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", TextView("/Styles/All.axaml"));
        string all = dir.Write("Styles/All.axaml", $"""
            <ResourceDictionary {Xmlns}>
              <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="Texts.axaml" />
              </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
            """);
        string texts = dir.Write("Styles/Texts.axaml", TextResources("After"));
        ChangedIncludes includes = new([Renderer(dir)], Build(texts, TextResources("Before"), all, File.ReadAllText(all)));

        Control control = Renderer(dir).Render(view, includes: includes).Control;

        Assert.Equal("After", TextOf(control));
    }

    [AvaloniaFact]
    public void FindChanged_NoChange_GivesNoDocuments()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", TextView("/Styles/Texts.axaml"));
        string texts = dir.Write("Styles/Texts.axaml", TextResources("Before"));
        ChangedIncludes includes = new([Renderer(dir)], Build(texts, TextResources("Before")));

        IReadOnlyList<RuntimeXamlLoaderDocument> documents = includes.FindChanged(view, File.ReadAllText(view));

        Assert.Empty(documents);
        Assert.Null(includes.Error);
    }

    [AvaloniaFact]
    public void FindChanged_BrokenInclude_GivesTheErrorAndNoDocument()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", TextView("/Styles/Texts.axaml"));
        string texts = dir.Write("Styles/Texts.axaml", BrokenXaml);
        ChangedIncludes includes = new([Renderer(dir)], Build(texts, TextResources("Before")));

        IReadOnlyList<RuntimeXamlLoaderDocument> documents = includes.FindChanged(view, File.ReadAllText(view));

        Assert.Empty(documents);
        Assert.Contains("Texts.axaml", includes.Error, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void FindChanged_IncludeThatDoesNotLoad_GivesTheErrorAndNoDocument()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", TextView("/Styles/Texts.axaml"));
        string texts = dir.Write("Styles/Texts.axaml", $"""<ResourceDictionary {Xmlns}><NoSuchType x:Key="Text" /></ResourceDictionary>""");
        ChangedIncludes includes = new([Renderer(dir)], Build(texts, TextResources("Before")));

        IReadOnlyList<RuntimeXamlLoaderDocument> documents = includes.FindChanged(view, File.ReadAllText(view));

        Assert.Empty(documents);
        Assert.Contains("Texts.axaml", includes.Error, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void FindChanged_FirstReadOfAFile_KeepsItsTextAsTheTextOfTheBuild()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", TextView("/Styles/Texts.axaml"));
        _ = dir.Write("Styles/Texts.axaml", TextResources("Before"));
        ChangedIncludes includes = new([Renderer(dir)], []);

        IReadOnlyList<RuntimeXamlLoaderDocument> documents = includes.FindChanged(view, File.ReadAllText(view));

        Assert.Empty(documents);
    }

    [AvaloniaFact]
    public void IsOutdated_IncludeChangedAfterTheFind_GivesTrue()
    {
        using TempDir dir = new();
        string view = dir.Write("Views/Main.axaml", TextView("/Styles/Texts.axaml"));
        string texts = dir.Write("Styles/Texts.axaml", TextResources("Before"));
        ChangedIncludes includes = new([Renderer(dir)], []);
        _ = includes.FindChanged(view, File.ReadAllText(view));
        bool isOutdatedBefore = includes.IsOutdated();

        File.WriteAllText(texts, TextResources("After"));

        Assert.False(isOutdatedBefore);
        Assert.True(includes.IsOutdated());
    }

    private static string TextView(string source) => $"""
        <UserControl {Xmlns}>
          <UserControl.Resources>
            <ResourceDictionary>
              <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="{source}" />
              </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
          </UserControl.Resources>
          <TextBlock Text="{"{"}StaticResource Text{"}"}" />
        </UserControl>
        """;

    private static string TextResources(string text) => $"""
        <ResourceDictionary {Xmlns}>
          <x:String x:Key="Text">{text}</x:String>
        </ResourceDictionary>
        """;

    private static string TagStyles(string tag) => $"""
        <Styles {Xmlns}>
          <Style Selector="TextBlock">
            <Setter Property="Tag" Value="{tag}" />
          </Style>
        </Styles>
        """;

    // The text of each file at the start of the host. The tests change the files before the render.
    private static Dictionary<string, string> Build(params string[] pathsAndTexts)
    {
        Dictionary<string, string> texts = new(StringComparer.Ordinal);
        for (int i = 0; i < pathsAndTexts.Length; i += 2)
        {
            texts[Path.GetFullPath(pathsAndTexts[i])] = pathsAndTexts[i + 1];
        }

        return texts;
    }

    private static string? TextOf(Control control) =>
        Assert.IsType<TextBlock>(Assert.IsType<UserControl>(control).Content).Text;

    private static object? TagOf(IStyle styles) =>
        Assert.IsType<Setter>(Assert.IsType<Style>(Assert.IsType<Styles>(styles)[0]).Setters[0]).Value;

    private static PreviewRenderer Renderer(TempDir dir) => new(typeof(ChangedIncludesTests).Assembly, dir.Path, DesignServices.None);

    // Any other loaded assembly stands for the library project.
    private static PreviewRenderer LibraryRenderer(TempDir dir) => new(typeof(Button).Assembly, dir.Path, DesignServices.None);
}
