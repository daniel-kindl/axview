using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using AxView.Host.Rendering;

namespace AxView.Host.UI;

/// <summary>Holds one control of the <see cref="ControlGallery"/>. Shows the error when the control fails in layout.</summary>
/// <remarks>
/// The control applies its template in the measure. A template of the app can miss a part that the control needs.
/// Then only this cell shows the error. The other cells of the gallery stay.
/// </remarks>
internal sealed class GalleryCell : Decorator
{
    private const double ErrorMaxWidth = 240;

    protected override Size MeasureOverride(Size availableSize)
    {
        try
        {
            return base.MeasureOverride(availableSize);
        }
        catch (Exception ex)
        {
            // The template is user code. It can throw any exception.
            Child = new TextBlock
            {
                Text = ExceptionText.Describe(ex),
                Foreground = Brushes.Red,
                MaxWidth = ErrorMaxWidth,
                TextWrapping = TextWrapping.Wrap,
                Margin = Child?.Margin ?? default,
            };
            return base.MeasureOverride(availableSize);
        }
    }
}
