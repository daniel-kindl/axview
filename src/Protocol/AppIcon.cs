using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AxView.Protocol;

/// <summary>The icon of the AxView windows: a rounded square with the letters "Ax".</summary>
/// <remarks>The code draws the icon. Thus the host needs no resource file in the target runtime.</remarks>
internal static class AppIcon
{
    private const int Size = 64;

    // The letters fill about half of the icon.
    private const double LetterSizeRatio = 30.0 / 64;
    private static readonly Lazy<WindowIcon> icon = new(Draw);

    /// <summary>The style class of the control that <see cref="CreateFace"/> gives.</summary>
    /// <remarks>The icon has fixed brand colors. They do not follow the theme variant.</remarks>
    public const string FaceClass = "axview-app-icon";

    /// <summary>Gets the icon. All windows share one instance.</summary>
    public static WindowIcon Get() => icon.Value;

    /// <summary>Creates the icon as a control, for example for the header of a window.</summary>
    /// <param name="size">The width and the height, in device-independent pixels.</param>
    /// <returns>A new control.</returns>
    public static Control CreateFace(double size) =>
        new Border
        {
            Width = size,
            Height = size,
            Background = new SolidColorBrush(Color.Parse("#7B3F85")),
            CornerRadius = new CornerRadius(size / 5.0),
            Classes = { FaceClass },
            Child = new TextBlock
            {
                Text = "Ax",
                FontSize = size * LetterSizeRatio,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            },
        };

    private static WindowIcon Draw()
    {
        Control face = CreateFace(Size);
        face.Measure(new Size(Size, Size));
        face.Arrange(new Rect(0, 0, Size, Size));

        // The icon copies the pixels. Dispose the bitmap.
        using RenderTargetBitmap bitmap = new(new PixelSize(Size, Size));
        bitmap.Render(face);
        return new WindowIcon(bitmap);
    }
}
