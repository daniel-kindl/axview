using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

namespace AxView.Protocol;

/// <summary>Creates the small controls that the AxView chrome uses in more than one window.</summary>
internal static class ChromeControls
{
    /// <summary>Creates an icon in the size of the view box of <see cref="ChromeIcons"/>.</summary>
    /// <param name="icon">The icon.</param>
    /// <returns>A new icon. It takes the foreground of its parent.</returns>
    public static PathIcon CreateIcon(ChromeIcon icon) =>
        new() { Width = ChromeIcons.Size, Height = ChromeIcons.Size, Data = ChromeIcons.CreateGeometry(icon) };

    /// <summary>Creates a button that shows only an icon. The tip tells the user what the button does.</summary>
    /// <param name="icon">The icon.</param>
    /// <param name="tip">The tooltip and the automation name.</param>
    /// <returns>A new button with no background. The theme shows a background on pointer over.</returns>
    public static Button CreateIconButton(ChromeIcon icon, string tip)
    {
        Button button = new()
        {
            Content = CreateIcon(icon),
            Padding = new Thickness(ChromeStyle.SpacingMedium - ChromeStyle.SpacingExtraSmall),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
        };
        ToolTip.SetTip(button, tip);
        AutomationProperties.SetName(button, tip);
        return button;
    }

    /// <summary>Creates a vertical line between two groups of controls.</summary>
    /// <returns>A new separator. It takes its brush from the window resources.</returns>
    public static Border CreateSeparator()
    {
        Border separator = new() { Width = 1, Margin = new Thickness(ChromeStyle.SpacingMedium, ChromeStyle.SpacingSmall) };
        ChromeStyle.SetBrush(separator, Border.BackgroundProperty, ChromeStyle.BorderBrushKey);
        return separator;
    }
}
