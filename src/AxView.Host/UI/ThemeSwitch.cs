using Avalonia.Controls;
using Avalonia.Styling;

namespace AxView.Host.UI;

/// <summary>Lets the user select the theme variant of a window: Default, Light, or Dark.</summary>
/// <remarks>Default clears the window's variant, so the window uses the variant of the target app.</remarks>
internal sealed class ThemeSwitch : ComboBox
{
    private readonly TopLevel target;

    public ThemeSwitch(TopLevel target)
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
            target.ClearValue(TopLevel.RequestedThemeVariantProperty);
        }
    }
}
