extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

using host::AxView.Host;

namespace AxView.Tests;

public sealed class PreviewRendererTests
{
    private const string AvaloniaXmlns = "xmlns=\"https://github.com/avaloniaui\"";

    [AvaloniaFact]
    public void Render_ReturnsTheRootControlOfTheFile()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");

        Control preview = Renderer(dir).Render(file);

        Assert.Equal("Hello", Assert.IsType<TextBlock>(preview).Text);
    }

    [AvaloniaFact]
    public void Render_MovesTheContentOfAWindowIntoAContainer()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Main.axaml", $"""
            <Window {AvaloniaXmlns} Width="300" Height="200">
              <TextBlock Text="Inside" />
            </Window>
            """);

        Control preview = Renderer(dir).Render(file);

        Border container = Assert.IsType<Border>(preview);
        Assert.Equal(300, container.Width);
        Assert.Equal("Inside", Assert.IsType<TextBlock>(container.Child).Text);
    }

    [AvaloniaFact]
    public void Render_FindsALocalTypeInABindingPath()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Local.axaml", $"""
            <local:FixtureControl {AvaloniaXmlns} xmlns:local="using:AxView.Tests" Tag="tagged">
              <TextBlock Text="{"{"}Binding $parent[local:FixtureControl].Tag{"}"}" />
            </local:FixtureControl>
            """);

        Control preview = Renderer(dir).Render(file);

        Window window = new() { Content = preview };
        window.Show();
        TextBlock text = Assert.IsType<TextBlock>(Assert.IsType<FixtureControl>(preview).Content);
        Assert.Equal("tagged", text.Text);
    }

    [AvaloniaFact]
    public void Render_RejectsAStylesRoot()
    {
        using TempDir dir = new();
        string file = dir.Write("Styles/Buttons.axaml", $"""<Styles {AvaloniaXmlns} />""");

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => Renderer(dir).Render(file));

        Assert.Contains(nameof(Styles), ex.Message, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Render_ThrowsTheLoadErrorOfInvalidXaml()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Broken.axaml", $"""<UserControl {AvaloniaXmlns}><NoSuchControl /></UserControl>""");

        Exception ex = Assert.ThrowsAny<Exception>(() => Renderer(dir).Render(file));

        Assert.Contains("NoSuchControl", ex.Message, StringComparison.Ordinal);
    }

    // The test assembly is the local assembly of the fixture project.
    private static PreviewRenderer Renderer(TempDir dir) => new(typeof(PreviewRendererTests).Assembly, dir.Path);
}
