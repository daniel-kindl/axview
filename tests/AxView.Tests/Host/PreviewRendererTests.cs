extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class PreviewRendererTests
{
    private const string AvaloniaXmlns = "xmlns=\"https://github.com/avaloniaui\"";
    private const string XamlXmlns = "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";

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
            <local:FixtureControl {AvaloniaXmlns} xmlns:local="using:AxView.Tests.Fixtures" Tag="tagged">
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
    public void Render_CreatesTheCodeBehindTypeAndFillsItFromTheXaml()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/WithCode.axaml", $"""
            <UserControl {AvaloniaXmlns} {XamlXmlns} x:Class="AxView.Tests.Fixtures.FixtureCodeBehind">
              <TextBlock Text="From XAML" />
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file);

        FixtureCodeBehind view = Assert.IsType<FixtureCodeBehind>(preview);
        Assert.Equal(FixtureCodeBehind.ConstructedTag, view.Tag);
        Assert.Equal("From XAML", Assert.IsType<TextBlock>(view.Content).Text);
    }

    [AvaloniaFact]
    public void Render_UsesDesignModeWhenTheCodeBehindTypeHasNoParameterlessConstructor()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/NoDefault.axaml", $"""
            <UserControl {AvaloniaXmlns} {XamlXmlns} x:Class="AxView.Tests.Fixtures.FixtureNoDefaultConstructor">
              <TextBlock Text="From XAML" />
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file);

        Assert.IsNotType<FixtureNoDefaultConstructor>(preview);
        Assert.Equal("From XAML", Assert.IsType<TextBlock>(Assert.IsAssignableFrom<UserControl>(preview).Content).Text);
    }

    [AvaloniaFact]
    public void Render_AppliesTheDesignDataContext()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Designed.axaml", $"""
            <UserControl {AvaloniaXmlns} xmlns:vm="using:AxView.Tests.Fixtures">
              <Design.DataContext>
                <vm:FixtureViewModel />
              </Design.DataContext>
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file);

        _ = Assert.IsType<FixtureViewModel>(preview.DataContext);
    }

    [AvaloniaFact]
    public void Render_AppliesTheDesignDataContextOfAWindow()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/DesignedWindow.axaml", $"""
            <Window {AvaloniaXmlns} xmlns:vm="using:AxView.Tests.Fixtures">
              <Design.DataContext>
                <vm:FixtureViewModel />
              </Design.DataContext>
              <TextBlock Text="Inside" />
            </Window>
            """);

        Control preview = Renderer(dir).Render(file);

        _ = Assert.IsType<FixtureViewModel>(preview.DataContext);
    }

    [AvaloniaFact]
    public void Render_CreatesTheDataTypeWhenNoDesignDataContextIsSet()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Typed.axaml", $"""
            <UserControl {AvaloniaXmlns} {XamlXmlns} xmlns:vm="using:AxView.Tests.Fixtures" x:DataType="vm:FixtureViewModel">
              <TextBlock Text="{"{"}Binding Greeting{"}"}" />
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file);

        _ = Assert.IsType<FixtureViewModel>(preview.DataContext);
    }

    [AvaloniaFact]
    public void Render_CreatesTheDataTypeWhenTheDesignDataContextHasNoParameterlessConstructor()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/DependentDesign.axaml", $"""
            <UserControl {AvaloniaXmlns} {XamlXmlns} xmlns:vm="using:AxView.Tests.Fixtures" x:DataType="vm:FixtureViewModel">
              <Design.DataContext>
                <vm:FixtureDependentViewModel />
              </Design.DataContext>
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file);

        _ = Assert.IsType<FixtureViewModel>(preview.DataContext);
    }

    [AvaloniaFact]
    public void Render_LeavesNoDataContextWhenTheDataTypeHasNoParameterlessConstructor()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Dependent.axaml", $"""
            <UserControl {AvaloniaXmlns} {XamlXmlns} xmlns:vm="using:AxView.Tests.Fixtures"
                         x:DataType="vm:FixtureDependentViewModel" />
            """);

        Control preview = Renderer(dir).Render(file);

        Assert.Null(preview.DataContext);
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
