using Avalonia.Controls;
using Avalonia.Styling;

using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Holds the preview. Gives the preview the styles, resources, data templates, and theme variant of the target app.</summary>
/// <remarks>
/// The target styles apply only inside the scope. Thus the AxView chrome keeps its own theme.
/// A popup of the preview finds the styles through its logical parent.
/// </remarks>
internal sealed class PreviewScope : ThemeVariantScope
{
    private readonly PreviewFrame frame;

    /// <summary>Puts the styles of the target app in the scope.</summary>
    /// <param name="target">The styles of the target app. The scope becomes their owner.</param>
    public PreviewScope(TargetStyles target)
    {
        Styles.Add(target.Styles);
        Resources = target.Resources;
        target.AddDataTemplatesTo(DataTemplates);
        AppThemeVariant = target.ThemeVariant ?? ThemeVariant.Default;
        RequestedThemeVariant = AppThemeVariant;
        target.ThemeVariantChanged += (_, _) => ChangeAppThemeVariant(target.ThemeVariant ?? ThemeVariant.Default);
        // The scope has no parent yet. Thus the search finds only the Window theme of the target app.
        bool hasWindowTheme = this.TryFindResource(typeof(Window), out object? found) && found is ControlTheme;
        frame = new PreviewFrame(hasWindowTheme ? (ControlTheme)found! : null);
        Child = frame;
    }

    /// <summary>Gets the theme variant of the target app. <see cref="ThemeVariant.Default"/> when the app requests none.</summary>
    public ThemeVariant AppThemeVariant { get; private set; }

    /// <summary>Gets or sets the preview.</summary>
    public object? Content
    {
        get => frame.Content;
        set => frame.Content = value;
    }

    /// <summary>Requests the variant of a theme. <see cref="PreviewTheme.Default"/> requests the variant of the target app.</summary>
    public void ShowTheme(PreviewTheme theme) =>
        RequestedThemeVariant = theme switch
        {
            PreviewTheme.Light => ThemeVariant.Light,
            PreviewTheme.Dark => ThemeVariant.Dark,
            _ => AppThemeVariant,
        };

    // A variant that the user selects stays. The scope follows the app only when it shows the app variant.
    private void ChangeAppThemeVariant(ThemeVariant variant)
    {
        bool isAppVariantShown = RequestedThemeVariant == AppThemeVariant;
        AppThemeVariant = variant;
        if (isAppVariantShown)
        {
            RequestedThemeVariant = variant;
        }
    }
}
