using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;

using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Shows the error of the last load as a card at the bottom of the stage.</summary>
/// <remarks>
/// The header has the copy button and the collapse button. A collapsed panel shows only the header.
/// The panel stays collapsed for the next error, until the user expands it.
/// </remarks>
internal sealed class ErrorPanel : Border
{
    private const double MaximumTextHeight = 240;
    private const string CollapseTip = "Collapse the error";
    private const string ExpandTip = "Expand the error";


    private readonly SelectableTextBlock errorText;
    private readonly TextBlock note;
    private readonly DockPanel details;
    private readonly Button collapseButton;
    private readonly PathIcon collapseIcon;

    /// <summary>The note that tells the user that the preview shows the last good render.</summary>
    public const string LastGoodRenderNote = "Showing the last good render.";

    /// <summary>Creates a hidden panel.</summary>
    public ErrorPanel()
    {
        IsVisible = false;
        VerticalAlignment = VerticalAlignment.Bottom;
        Margin = new Thickness(ChromeStyle.SpacingLarge);
        Padding = new Thickness(ChromeStyle.SpacingLarge, ChromeStyle.SpacingMedium, ChromeStyle.SpacingMedium, ChromeStyle.SpacingLarge);
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(ChromeStyle.CornerRadius);
        BoxShadow = ChromeStyle.CardShadow;
        ChromeStyle.SetBrush(this, BackgroundProperty, ChromeStyle.CardBrushKey);
        ChromeStyle.SetBrush(this, BorderBrushProperty, ChromeStyle.ErrorBrushKey);

        errorText = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap, FontFamily = ChromeStyle.Monospace, FontSize = ChromeStyle.FontSizeNormal };
        note = new TextBlock { Text = LastGoodRenderNote, FontSize = ChromeStyle.FontSizeSmall };
        ChromeStyle.SetBrush(note, TextBlock.ForegroundProperty, ChromeStyle.MutedTextBrushKey);
        DockPanel.SetDock(note, Dock.Top);
        details = new DockPanel
        {
            Children =
            {
                note,
                new ScrollViewer
                {
                    MaxHeight = MaximumTextHeight,
                    Margin = new Thickness(0, ChromeStyle.SpacingMedium, 0, 0),
                    Content = errorText,
                },
            },
        };

        Button copyButton = ChromeControls.CreateIconButton(ChromeIcon.Copy, "Copy the error");
        copyButton.Click += async (_, _) => await CopyAsync();
        collapseButton = ChromeControls.CreateIconButton(ChromeIcon.Collapse, CollapseTip);
        collapseIcon = (PathIcon)collapseButton.Content!;
        collapseButton.Click += (_, _) => SetCollapsed(details.IsVisible);

        Child = new DockPanel { Children = { BuildHeader(copyButton, collapseButton), details } };
    }

    /// <summary>Shows an error, or hides the panel.</summary>
    /// <param name="error">The error text. Null hides the panel.</param>
    /// <param name="isLastGoodRenderShown">True when the preview shows the last good render of the file.</param>
    public void Show(string? error, bool isLastGoodRenderShown)
    {
        errorText.Text = error;
        note.IsVisible = isLastGoodRenderShown;
        IsVisible = error is not null;
    }

    private static DockPanel BuildHeader(Button copyButton, Button collapseButton)
    {
        PathIcon icon = ChromeControls.CreateIcon(ChromeIcon.Error);
        icon.Margin = new Thickness(0, 0, ChromeStyle.SpacingMedium, 0);
        ChromeStyle.SetBrush(icon, PathIcon.ForegroundProperty, ChromeStyle.ErrorBrushKey);
        StackPanel buttons = new() { Orientation = Orientation.Horizontal, Children = { copyButton, collapseButton } };
        DockPanel.SetDock(buttons, Dock.Right);
        DockPanel.SetDock(icon, Dock.Left);
        TextBlock title = new() { Text = "Error", FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        DockPanel header = new() { Children = { buttons, icon, title } };
        DockPanel.SetDock(header, Dock.Top);
        return header;
    }

    // A collapsed panel shows a chevron that points up.
    private void SetCollapsed(bool isCollapsed)
    {
        details.IsVisible = !isCollapsed;
        collapseIcon.RenderTransform = isCollapsed ? new RotateTransform(180) : null;
        string tip = isCollapsed ? ExpandTip : CollapseTip;
        ToolTip.SetTip(collapseButton, tip);
        AutomationProperties.SetName(collapseButton, tip);
    }

    private async Task CopyAsync()
    {
        IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(errorText.Text ?? string.Empty);
        }
    }
}
