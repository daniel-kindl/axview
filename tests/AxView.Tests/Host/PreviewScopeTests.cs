extern alias host;

using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

using host::AxView.Host.Startup;
using host::AxView.Host.UI;

namespace AxView.Tests.Host;

public sealed class PreviewScopeTests
{
    private const string TargetTag = "Target";

    [AvaloniaFact]
    public void Content_GetsTheTargetStyles()
    {
        TextBlock inside = new();
        TextBlock outside = new();
        PreviewScope scope = new(Target([TagStyle()])) { Content = inside };
        Window window = new() { Content = new StackPanel { Children = { scope, outside } } };

        window.Show();

        Assert.Equal(TargetTag, inside.Tag);
        Assert.Null(outside.Tag);
        window.Close();
    }

    [AvaloniaFact]
    public void Content_FindsTheTargetResources()
    {
        ResourceDictionary resources = new() { ["Accent"] = Brushes.Red };
        TextBlock inside = new();
        PreviewScope scope = new(new TargetStyles([], resources, [], null)) { Content = inside };
        Window window = new() { Content = scope };

        window.Show();

        Assert.Same(Brushes.Red, inside.FindResource("Accent"));
        window.Close();
    }

    [AvaloniaFact]
    public void New_FrameGetsTheLookOfTheTargetWindowTheme()
    {
        ControlTheme windowTheme = new(typeof(Window))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Red),
                new Setter(TextElement.FontSizeProperty, 21.0),
                new Setter(Window.SystemDecorationsProperty, SystemDecorations.None),
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Window>((_, _) => new Panel())),
            },
        };
        ResourceDictionary resources = new() { [typeof(Window)] = windowTheme };
        TextBlock inside = new();
        PreviewScope scope = new(new TargetStyles([], resources, [], null)) { Content = inside };
        Window window = new() { Content = scope };

        window.Show();

        TemplatedControl frame = Assert.IsAssignableFrom<TemplatedControl>(scope.Child);
        Assert.Same(Brushes.Red, frame.Background);
        Assert.Equal(21.0, inside.FontSize);
        Assert.Same(inside, frame.GetVisualDescendants().OfType<TextBlock>().Single());
        window.Close();
    }

    [AvaloniaFact]
    public void New_RequestsTheAppThemeVariant()
    {
        PreviewScope scope = new(new TargetStyles([], new ResourceDictionary(), [], ThemeVariant.Dark));

        Assert.Equal(ThemeVariant.Dark, scope.RequestedThemeVariant);
        Assert.Equal(ThemeVariant.Dark, scope.AppThemeVariant);
    }

    [AvaloniaFact]
    public void New_NoAppVariant_RequestsTheDefaultVariant()
    {
        PreviewScope scope = new(TargetStyles.None);

        Assert.Equal(ThemeVariant.Default, scope.AppThemeVariant);
    }

    private static TargetStyles Target(IReadOnlyList<IStyle> styles) =>
        new(styles, new ResourceDictionary(), [], null);

    private static Style TagStyle() => new(x => x.OfType<TextBlock>())
    {
        Setters = { new Setter(Control.TagProperty, TargetTag) },
    };
}
