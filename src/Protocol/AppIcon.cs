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
    private static readonly Lazy<WindowIcon> icon = new(Draw);

    /// <summary>Gets the icon. All windows share one instance.</summary>
    public static WindowIcon Get() => icon.Value;

    private static WindowIcon Draw()
    {
        Border face = new()
        {
            Width = Size,
            Height = Size,
            Background = new SolidColorBrush(Color.Parse("#7B3F85")),
            CornerRadius = new CornerRadius(Size / 5.0),
            Child = new TextBlock
            {
                Text = "Ax",
                FontSize = 30,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            },
        };
        face.Measure(new Size(Size, Size));
        face.Arrange(new Rect(0, 0, Size, Size));

        RenderTargetBitmap bitmap = new(new PixelSize(Size, Size));
        bitmap.Render(face);
        return new WindowIcon(bitmap);
    }
}
