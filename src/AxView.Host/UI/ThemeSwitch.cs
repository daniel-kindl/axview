using Avalonia.Controls;
using Avalonia.Styling;

using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Lets the user select the theme variant of the preview: Default, Light, or Dark.</summary>
/// <remarks>Default gives the preview the variant of the target app. The AxView chrome keeps its own variant.</remarks>
internal sealed class ThemeSwitch : ComboBox
{
    private readonly PreviewScope target;

    public ThemeSwitch(PreviewScope target)
    {
        this.target = target;
        ItemsSource = Enum.GetValues<PreviewTheme>();
        SelectedItem = PreviewTheme.Default;
        SelectionChanged += (_, _) => Apply();
    }

    // A subclass has no control theme of its own. Use the ComboBox theme.
    protected override Type StyleKeyOverride => typeof(ComboBox);

    private void Apply()
    {
        if (SelectedItem is PreviewTheme.Light)
        {
            target.RequestedThemeVariant = ThemeVariant.Light;
        }
        else if (SelectedItem is PreviewTheme.Dark)
        {
            target.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            target.RequestedThemeVariant = target.AppThemeVariant;
        }
    }
}
