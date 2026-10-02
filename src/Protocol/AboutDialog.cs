using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AxView.Protocol;

/// <summary>A dialog that shows the version, the build date, and the Avalonia version. The picker and the preview window use it.</summary>
internal sealed class AboutDialog : Window
{
    /// <summary>Makes the dialog.</summary>
    /// <param name="info">The facts to show.</param>
    public AboutDialog(AboutInfo info)
    {
        Title = "About AxView";
        SizeToContent = SizeToContent.WidthAndHeight;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Button close = new() { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        close.Click += (_, _) => Close();

        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = "AxView", FontSize = 24, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = $"Version {info.Version}" },
                new TextBlock { Text = $"Build date {info.BuildDate:yyyy-MM-dd}" },
                new TextBlock { Text = $"Avalonia {info.AvaloniaVersion}" },
                close,
            },
        };
    }
}
