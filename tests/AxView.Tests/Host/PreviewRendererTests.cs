extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class PreviewRendererTests
{
    private const string AvaloniaXmlns = "xmlns=\"https://github.com/avaloniaui\"";
    private const string XamlXmlns = "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";
    private const string DesignXmlns =
        "xmlns:d=\"http://schemas.microsoft.com/expression/blend/2008\" " +
        "xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" mc:Ignorable=\"d\"";

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
    public void Render_SelectedViewModel_ReplacesTheDesignDataContext()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Designed.axaml", $"""
            <UserControl {AvaloniaXmlns} xmlns:vm="using:AxView.Tests.Fixtures">
              <Design.DataContext>
                <vm:FixtureViewModel />
              </Design.DataContext>
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file, typeof(FixtureOtherViewModel));

        _ = Assert.IsType<FixtureOtherViewModel>(preview.DataContext);
    }

    [AvaloniaFact]
    public void Render_SelectedViewModel_SetsTheDataContextOfAWindow()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Main.axaml", $"""<Window {AvaloniaXmlns}><TextBlock /></Window>""");

        Control preview = Renderer(dir).Render(file, typeof(FixtureOtherViewModel));

        _ = Assert.IsType<FixtureOtherViewModel>(preview.DataContext);
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
    public void Render_AppliesTheDesignDataContextWithContent()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/DesignedWithContent.axaml", $"""
            <UserControl {AvaloniaXmlns} xmlns:vm="using:AxView.Tests.Fixtures">
              <Design.DataContext>
                <vm:FixtureViewModel>
                  <vm:FixtureViewModel.Name>Sample</vm:FixtureViewModel.Name>
                </vm:FixtureViewModel>
              </Design.DataContext>
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file);

        Assert.Equal("Sample", Assert.IsType<FixtureViewModel>(preview.DataContext).Name);
    }

    [AvaloniaFact]
    public void Render_CreatesTheDataTypeWhenTheDesignDataContextWithContentHasNoParameterlessConstructor()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/DependentWithContent.axaml", $"""
            <UserControl {AvaloniaXmlns} {XamlXmlns} xmlns:vm="using:AxView.Tests.Fixtures" x:DataType="vm:FixtureViewModel">
              <Design.DataContext>
                <vm:FixtureDependentViewModel>
                  <vm:FixtureDependentViewModel.Greeting>Sample</vm:FixtureDependentViewModel.Greeting>
                </vm:FixtureDependentViewModel>
              </Design.DataContext>
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file);

        _ = Assert.IsType<FixtureViewModel>(preview.DataContext);
    }

    [AvaloniaTheory]
    [InlineData("{d:DesignInstance Type=vm:FixtureViewModel, IsDesignTimeCreatable=True}")]
    [InlineData("{d:DesignInstance vm:FixtureViewModel}")]
    [InlineData("{d:DesignInstance Type={x:Type vm:FixtureViewModel}}")]
    [InlineData("{d:DesignInstance Type={x:Type TypeName=vm:FixtureViewModel}, IsDesignTimeCreatable=True}")]
    public void Render_CreatesTheTypeOfADesignInstance(string designInstance)
    {
        using TempDir dir = new();
        string file = dir.Write("Views/DesignInstance.axaml", $"""
            <UserControl {AvaloniaXmlns} {XamlXmlns} {DesignXmlns} xmlns:vm="using:AxView.Tests.Fixtures"
                         d:DataContext="{designInstance}" />
            """);

        Control preview = Renderer(dir).Render(file);

        _ = Assert.IsType<FixtureViewModel>(preview.DataContext);
    }

    [AvaloniaFact]
    public void Render_CreatesTheDesignInstanceOfAChildElement()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/ChildDesignInstance.axaml", $"""
            <UserControl {AvaloniaXmlns} {DesignXmlns} xmlns:vm="using:AxView.Tests.Fixtures">
              <TextBlock d:DataContext="{"{"}d:DesignInstance vm:FixtureViewModel{"}"}" Text="{"{"}Binding Greeting{"}"}" />
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file);

        TextBlock child = Assert.IsType<TextBlock>(Assert.IsType<UserControl>(preview).Content);
        _ = Assert.IsType<FixtureViewModel>(child.DataContext);
    }

    [AvaloniaFact]
    public void Render_LetsAChildInheritTheDataContextWhenItsDesignInstanceHasNoParameterlessConstructor()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/DependentChildDesignInstance.axaml", $"""
            <UserControl {AvaloniaXmlns} {XamlXmlns} {DesignXmlns} xmlns:vm="using:AxView.Tests.Fixtures"
                         x:DataType="vm:FixtureViewModel">
              <TextBlock d:DataContext="{"{"}d:DesignInstance vm:FixtureDependentViewModel{"}"}" />
            </UserControl>
            """);

        Control preview = Renderer(dir).Render(file);

        TextBlock child = Assert.IsType<TextBlock>(Assert.IsType<UserControl>(preview).Content);
        _ = Assert.IsType<FixtureViewModel>(child.DataContext);
    }

    [AvaloniaFact]
    public void Render_CreatesTheDataTypeWhenTheDesignInstanceHasNoParameterlessConstructor()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/DependentDesignInstance.axaml", $"""
            <UserControl {AvaloniaXmlns} {XamlXmlns} {DesignXmlns} xmlns:vm="using:AxView.Tests.Fixtures"
                         x:DataType="vm:FixtureViewModel"
                         d:DataContext="{"{"}d:DesignInstance vm:FixtureDependentViewModel{"}"}" />
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
    public void Render_ShowsThePreviewWithControlOfAStylesRootWithTheStyles()
    {
        using TempDir dir = new();
        string file = dir.Write("Styles/Texts.axaml", $"""
            <Styles {AvaloniaXmlns}>
              <Design.PreviewWith>
                <Border Padding="20"><TextBlock Text="Sample" /></Border>
              </Design.PreviewWith>
              <Style Selector="TextBlock">
                <Setter Property="Tag" Value="styled" />
              </Style>
            </Styles>
            """);

        Control preview = Renderer(dir).Render(file);

        Window window = new() { Content = preview };
        window.Show();
        TextBlock text = Assert.IsType<TextBlock>(Assert.IsType<Border>(preview).Child);
        Assert.Equal("styled", text.Tag);
    }

    [AvaloniaFact]
    public void Render_ShowsThePreviewWithControlOfAResourceDictionaryRootWithTheResources()
    {
        using TempDir dir = new();
        string file = dir.Write("Styles/Colors.axaml", $"""
            <ResourceDictionary {AvaloniaXmlns} {XamlXmlns}>
              <Design.PreviewWith>
                <Border Padding="20" />
              </Design.PreviewWith>
              <x:String x:Key="Greeting">Hello</x:String>
            </ResourceDictionary>
            """);

        Control preview = Renderer(dir).Render(file);

        _ = Assert.IsType<Border>(preview);
        Assert.True(preview.TryFindResource("Greeting", out object? greeting));
        Assert.Equal("Hello", greeting);
    }

    [AvaloniaTheory]
    [InlineData("Styles")]
    [InlineData("ResourceDictionary")]
    public void Render_RejectsAStyleOrResourceRootWithoutPreviewWith(string rootName)
    {
        using TempDir dir = new();
        string file = dir.Write("Styles/Plain.axaml", $"""<{rootName} {AvaloniaXmlns} />""");

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => Renderer(dir).Render(file));

        Assert.Contains("Design.PreviewWith", ex.Message, StringComparison.Ordinal);
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
