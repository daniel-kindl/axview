using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Shows the preview in the size that the user selects.</summary>
/// <remarks>
/// Fit fills the stage with the preview.
/// A size shows the preview as a centered card on a neutral backdrop, with the size below it.
/// A card that is larger than the stage scrolls.
/// </remarks>
internal sealed class PreviewStage : Panel
{
    private static readonly BoxShadows cardShadow = new(new BoxShadow { OffsetY = 2, Blur = 12, Color = Color.FromArgb(0x40, 0, 0, 0) });

    private readonly Control preview;
    private readonly Border backdrop = new();
    private readonly Border card;
    private readonly TextBlock sizeLabel;
    private readonly DockPanel layout;

    /// <summary>Puts the preview on the stage. Fit is the first size.</summary>
    /// <param name="preview">The preview. The stage sets its width and height.</param>
    public PreviewStage(Control preview)
    {
        this.preview = preview;
        card = new Border { Child = preview };
        ChromeStyle.SetBrush(card, Border.BorderBrushProperty, ChromeStyle.BorderBrushKey);
        sizeLabel = new TextBlock
        {
            FontSize = ChromeStyle.FontSizeSmall,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, ChromeStyle.SpacingMedium, 0, 0),
        };
        ChromeStyle.SetBrush(sizeLabel, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        DockPanel.SetDock(sizeLabel, Dock.Bottom);
        layout = new DockPanel { Children = { sizeLabel, card } };

        // A preview with no background of its own shows the window background in Fit.
        ChromeStyle.SetBrush(backdrop, Border.BackgroundProperty, ChromeStyle.StageBrushKey);
        Children.Add(backdrop);
        Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = layout,
        });
        Apply();
    }

    /// <summary>Gets or sets the size of the preview.</summary>
    public PreviewSize Size
    {
        get;
        set
        {
            field = value;
            Apply();
        }
    } = PreviewSize.Fit;

    private void Apply()
    {
        bool isFit = Size.Preset == PreviewSizePreset.Fit;
        if (isFit)
        {
            preview.ClearValue(WidthProperty);
            preview.ClearValue(HeightProperty);
        }
        else
        {
            preview.Width = Size.Width;
            preview.Height = Size.Height;
            sizeLabel.Text = string.Format(CultureInfo.InvariantCulture, "{0:0} × {1:0}", Size.Width, Size.Height);
        }

        sizeLabel.IsVisible = !isFit;
        backdrop.IsVisible = !isFit;
        card.BorderThickness = isFit ? default : new Thickness(1);
        card.BoxShadow = isFit ? default : cardShadow;
        layout.Margin = isFit ? default : new Thickness(ChromeStyle.SpacingExtraLarge);
        layout.HorizontalAlignment = isFit ? HorizontalAlignment.Stretch : HorizontalAlignment.Center;
        layout.VerticalAlignment = isFit ? VerticalAlignment.Stretch : VerticalAlignment.Center;
    }
}
