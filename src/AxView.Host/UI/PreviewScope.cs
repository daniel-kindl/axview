using Avalonia.Controls;
using Avalonia.Styling;

using AxView.Host.Startup;

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
        DataTemplates.AddRange(target.DataTemplates);
        AppThemeVariant = target.ThemeVariant ?? ThemeVariant.Default;
        RequestedThemeVariant = AppThemeVariant;
        // The scope has no parent yet. Thus the search finds only the Window theme of the target app.
        bool hasWindowTheme = this.TryFindResource(typeof(Window), out object? found) && found is ControlTheme;
        frame = new PreviewFrame(hasWindowTheme ? (ControlTheme)found! : null);
        Child = frame;
    }

    /// <summary>Gets the theme variant of the target app. <see cref="ThemeVariant.Default"/> when the app requests none.</summary>
    public ThemeVariant AppThemeVariant { get; }

    /// <summary>Gets or sets the preview.</summary>
    public object? Content
    {
        get => frame.Content;
        set => frame.Content = value;
    }
}
