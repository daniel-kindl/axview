extern alias host;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class SnapshotWindowTests
{
    private const string AvaloniaXmlns = "xmlns=\"https://github.com/avaloniaui\"";

    [AvaloniaFact]
    public void New_ShowsOnlyThePreviewOfTheFile()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");

        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None);
        window.Show();

        Assert.Equal("Hello", Assert.IsType<TextBlock>(Assert.IsType<PreviewScope>(window.Content).Content).Text);
        window.Close();
    }

    [AvaloniaFact]
    public void New_WithAViewModelType_SetsTheDataContext()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");

        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None, typeof(FixtureViewModel));
        window.Show();

        TextBlock text = Assert.IsType<TextBlock>(Assert.IsType<PreviewScope>(window.Content).Content);
        _ = Assert.IsType<FixtureViewModel>(text.DataContext);
        window.Close();
    }

    [AvaloniaFact]
    public void Save_WithAPreviewSize_WritesAPngOfThatSize()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string png = Path.Combine(dir.Path, "hello.png");
        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None) { PreviewSize = PreviewSize.Parse("Phone") };
        window.Show();

        window.Save(png);

        using Bitmap bitmap = new(png);
        Assert.Equal(new PixelSize(390, 844), bitmap.PixelSize);
        window.Close();
    }

    [AvaloniaFact]
    public void Save_WithFit_WritesAPngOfTheDefaultWindowSize()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        string png = Path.Combine(dir.Path, "hello.png");
        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None);
        window.Show();

        window.Save(png);

        using Bitmap bitmap = new(png);
        Assert.Equal(new PixelSize(1280, 800), bitmap.PixelSize);
        window.Close();
    }

    [AvaloniaFact]
    public void Save_WithFitsContent_WritesAPngOfTheContentSize()
    {
        using TempDir dir = new();
        string file = dir.Write(
            "Views/Card.axaml",
            $"""<Border {AvaloniaXmlns} Width="300" Height="200" Margin="8" />""");
        string png = Path.Combine(dir.Path, "card.png");
        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None) { FitsContent = true };
        window.Show();

        window.Save(png);

        using Bitmap bitmap = new(png);
        Assert.Equal(new PixelSize(316, 216), bitmap.PixelSize);
        window.Close();
    }

    [AvaloniaFact]
    public void Save_WithFitsContentAndAPreviewSize_LimitsTheContentToThatSize()
    {
        using TempDir dir = new();
        string file = dir.Write(
            "Views/Wide.axaml",
            $"""<Border {AvaloniaXmlns} MinWidth="100" MinHeight="50" />""");
        string png = Path.Combine(dir.Path, "wide.png");
        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None)
        {
            PreviewSize = PreviewSize.Parse("500x300"),
            FitsContent = true,
        };
        window.Show();

        window.Save(png);

        using Bitmap bitmap = new(png);
        Assert.Equal(new PixelSize(100, 50), bitmap.PixelSize);
        window.Close();
    }

    [AvaloniaFact]
    public void ShowTheme_Dark_GivesThePreviewTheDarkVariant()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None);

        window.ShowTheme(PreviewTheme.Dark);
        window.Show();

        Assert.Equal(ThemeVariant.Dark, Assert.IsType<PreviewScope>(window.Content).ActualThemeVariant);
        window.Close();
    }

    private static PreviewRenderer Renderer(TempDir dir) => new(typeof(SnapshotWindowTests).Assembly, dir.Path, DesignServices.None);
}
