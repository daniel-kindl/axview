using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;

using AxView.Protocol;

namespace AxView.Tests.Fixtures;

/// <summary>Checks that a chrome window takes its brushes from the <see cref="ChromeStyle"/>.</summary>
/// <remarks>The launcher and the host compile their own <see cref="ChromeStyle"/>. Both give the same colors.</remarks>
internal static class ChromeBrushes
{
    private static readonly string[] keys =
    [
        ChromeStyle.BorderBrushKey,
        ChromeStyle.MutedTextBrushKey,
        ChromeStyle.AccentBrushKey,
        ChromeStyle.WarningBrushKey,
        ChromeStyle.ErrorBrushKey,
        ChromeStyle.StageBrushKey,
        ChromeStyle.StatusForegroundBrushKey,
    ];

    /// <summary>Gets the color of a chrome brush in a theme variant.</summary>
    /// <param name="key">The resource key of the brush.</param>
    /// <param name="variant">The theme variant.</param>
    /// <returns>The color of the brush.</returns>
    public static Color ColorOf(string key, ThemeVariant variant)
    {
        Assert.True(ChromeStyle.CreateResources().TryGetResource(key, variant, out object? brush));
        return Assert.IsType<ISolidColorBrush>(brush, exactMatch: false).Color;
    }

    /// <summary>Asserts that each brush that the window code sets is a chrome brush of the theme variant.</summary>
    /// <param name="window">The window. It must have the theme variant.</param>
    /// <param name="variant">The theme variant of the window.</param>
    public static void AssertFromChromeStyle(Window window, ThemeVariant variant)
    {
        HashSet<Color> chromeColors = [.. keys.Select(key => ColorOf(key, variant))];
        List<IBrush?> brushes = [];
        foreach (ILogical element in window.GetLogicalDescendants())
        {
            AddSet(brushes, element as Border, Border.BackgroundProperty);
            AddSet(brushes, element as Border, Border.BorderBrushProperty);
            AddSet(brushes, element as TextBlock, TextBlock.ForegroundProperty);
        }

        Assert.NotEmpty(brushes);
        Assert.All(brushes, brush => Assert.Contains(Assert.IsType<ISolidColorBrush>(brush, exactMatch: false).Color, chromeColors));
    }

    private static void AddSet(List<IBrush?> brushes, AvaloniaObject? element, AvaloniaProperty<IBrush?> property)
    {
        if (element is not null && element.IsSet(property))
        {
            brushes.Add(element.GetValue<IBrush?>(property));
        }
    }
}
