using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace AxView.Protocol;

/// <summary>The style set of the AxView chrome: a spacing scale, font sizes, a corner radius, and theme brushes.</summary>
/// <remarks>
/// The chrome theme can be Fluent or Simple. The two themes have different resource keys.
/// Thus the style set defines its own brushes for the light and the dark variant.
/// </remarks>
/// <example>
/// <code>
/// Resources.MergedDictionaries.Add(ChromeStyle.CreateResources());
/// ChromeStyle.SetBrush(statusBar, Border.BorderBrushProperty, ChromeStyle.BorderBrushKey);
/// </code>
/// </example>
internal static class ChromeStyle
{
    /// <summary>The smallest space, for example between a label and its control.</summary>
    public const double SpacingExtraSmall = 2;

    /// <summary>A small space, for example between the lines of a row.</summary>
    public const double SpacingSmall = 4;

    /// <summary>The default space between controls.</summary>
    public const double SpacingMedium = 8;

    /// <summary>A large space, for example inside a panel.</summary>
    public const double SpacingLarge = 12;

    /// <summary>The space at the edge of a window.</summary>
    public const double SpacingExtraLarge = 24;

    /// <summary>The font size of secondary text, for example a folder or a badge.</summary>
    public const double FontSizeSmall = 11;

    /// <summary>The font size of dense text, for example the status bar and the error text.</summary>
    public const double FontSizeNormal = 12;

    /// <summary>The font size of a window heading.</summary>
    public const double FontSizeHeading = 18;

    /// <summary>The font size of the app name in the picker.</summary>
    public const double FontSizeTitle = 28;

    /// <summary>The corner radius of a badge, a card, or a panel.</summary>
    public const double CornerRadius = 4;

    /// <summary>The resource key of the brush of a separator line or a border.</summary>
    public const string BorderBrushKey = "AxView.Chrome.BorderBrush";

    /// <summary>The resource key of the brush of secondary text.</summary>
    public const string MutedTextBrushKey = "AxView.Chrome.MutedTextBrush";

    /// <summary>The resource key of the AxView accent brush.</summary>
    public const string AccentBrushKey = "AxView.Chrome.AccentBrush";

    /// <summary>The resource key of the brush of a warning, as text or as a fill.</summary>
    public const string WarningBrushKey = "AxView.Chrome.WarningBrush";

    /// <summary>The resource key of the brush of an error, as text or as a fill.</summary>
    public const string ErrorBrushKey = "AxView.Chrome.ErrorBrush";

    /// <summary>The resource key of the neutral brush of the backdrop around the preview.</summary>
    public const string StageBrushKey = "AxView.Chrome.StageBrush";

    /// <summary>The resource key of the background brush of a card, for example the error panel.</summary>
    public const string CardBrushKey = "AxView.Chrome.CardBrush";

    /// <summary>The resource key of the brush of text on a warning or an error fill.</summary>
    public const string StatusForegroundBrushKey = "AxView.Chrome.StatusForegroundBrush";

    /// <summary>Gets the shadow of a card, for example the preview card or the error panel.</summary>
    public static BoxShadows CardShadow { get; } = new(new BoxShadow { OffsetY = 2, Blur = 12, Color = Color.FromArgb(0x40, 0, 0, 0) });

    /// <summary>Creates the theme brushes. Each window needs its own instance, because a dictionary has one owner.</summary>
    /// <returns>A dictionary with a light and a dark theme dictionary.</returns>
    public static ResourceDictionary CreateResources() =>
        new()
        {
            ThemeDictionaries =
            {
                [ThemeVariant.Light] = CreateBrushes(
                    border: "#D6D6D6", mutedText: "#616161", accent: "#7B3F85", warning: "#9D5D00", error: "#C42B1C", stage: "#E8E8E8", card: "#FFFFFF"),
                [ThemeVariant.Dark] = CreateBrushes(
                    border: "#404040", mutedText: "#A6A6A6", accent: "#C792D0", warning: "#C77700", error: "#E0493E", stage: "#141414", card: "#2B2B2B"),
            },
        };

    /// <summary>Binds a brush property of a chrome element to a theme brush. The brush follows the theme variant.</summary>
    /// <param name="element">The chrome element. It finds the brush in the resources of its window.</param>
    /// <param name="property">The brush property, for example <see cref="Border.BorderBrushProperty"/>.</param>
    /// <param name="key">The resource key, for example <see cref="BorderBrushKey"/>.</param>
    public static void SetBrush(StyledElement element, AvaloniaProperty property, string key) =>
        _ = element.Bind(property, element.GetResourceObservable(key));

    private static ResourceDictionary CreateBrushes(
        string border, string mutedText, string accent, string warning, string error, string stage, string card) =>
        new()
        {
            [BorderBrushKey] = CreateBrush(border),
            [MutedTextBrushKey] = CreateBrush(mutedText),
            [AccentBrushKey] = CreateBrush(accent),
            [WarningBrushKey] = CreateBrush(warning),
            [ErrorBrushKey] = CreateBrush(error),
            [StageBrushKey] = CreateBrush(stage),
            [CardBrushKey] = CreateBrush(card),
            [StatusForegroundBrushKey] = new SolidColorBrush(Colors.White),
        };

    private static SolidColorBrush CreateBrush(string color) => new(Color.Parse(color));
}
