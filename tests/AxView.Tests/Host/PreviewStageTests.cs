extern alias host;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Threading;

using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class PreviewStageTests
{
    private const double StageWidth = 800;
    private const double StageHeight = 600;

    [AvaloniaFact]
    public void Fit_FillsTheStageWithNoCard()
    {
        ContentControl preview = new();
        PreviewStage stage = new(preview) { Size = new PreviewSize(PreviewSizePreset.Custom, 400, 300) };

        stage.Size = PreviewSize.Fit;
        Window window = Show(stage);

        Assert.Equal(new Size(StageWidth, StageHeight), preview.Bounds.Size);
        Assert.False(preview.IsSet(Layoutable.WidthProperty));
        Assert.False(preview.IsSet(Layoutable.HeightProperty));
        Assert.Equal(default, CardOf(preview).BorderThickness);
        Assert.False(SizeLabelOf(stage).IsVisible);
        Assert.False(BackdropOf(stage).IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Preset_ShowsTheCardCentered_AndTheSizeBelowIt()
    {
        ContentControl preview = new();
        PreviewStage stage = new(preview) { Size = new PreviewSize(PreviewSizePreset.Custom, 400, 300) };

        Window window = Show(stage);
        Border card = CardOf(preview);
        TextBlock sizeLabel = SizeLabelOf(stage);
        Point cardTopLeft = card.TranslatePoint(default, stage) ?? throw new InvalidOperationException("The card is not in the stage.");
        Point labelTopLeft = sizeLabel.TranslatePoint(default, stage) ?? throw new InvalidOperationException("The label is not in the stage.");
        window.Close();

        Assert.Equal(new Size(400, 300), preview.Bounds.Size);
        Assert.NotEqual(default, card.BorderThickness);
        Assert.NotEqual(0, card.BoxShadow.Count);
        Assert.Equal(StageWidth / 2, cardTopLeft.X + (card.Bounds.Width / 2), precision: 0);
        Assert.True(sizeLabel.IsVisible);
        Assert.True(BackdropOf(stage).IsVisible);
        Assert.Equal("400 × 300", sizeLabel.Text);
        Assert.True(labelTopLeft.Y >= cardTopLeft.Y + card.Bounds.Height, "The size is not below the card.");
    }

    [AvaloniaFact]
    public void Preset_LargerThanTheStage_KeepsItsSize()
    {
        ContentControl preview = new();
        PreviewStage stage = new(preview) { Size = new PreviewSize(PreviewSizePreset.Desktop, 1920, 1080) };

        Window window = Show(stage);
        window.Close();

        Assert.Equal(new Size(1920, 1080), preview.Bounds.Size);
    }

    private static Window Show(PreviewStage stage)
    {
        Window window = new() { Width = StageWidth, Height = StageHeight, Content = stage };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // A preview with no background of its own shows the window background in Fit, not the backdrop.
    private static Border BackdropOf(PreviewStage stage) => stage.Children.OfType<Border>().Single();

    private static Border CardOf(Control preview) => Assert.IsType<Border>(preview.Parent);

    private static TextBlock SizeLabelOf(PreviewStage stage) => stage.GetLogicalDescendants().OfType<TextBlock>().Single();
}
