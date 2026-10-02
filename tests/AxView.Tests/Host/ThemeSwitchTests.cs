extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

using host::AxView.Host.UI;

namespace AxView.Tests.Host;

public sealed class ThemeSwitchTests
{
    [AvaloniaFact]
    public void Select_Dark_SetsTheDarkVariantOnTheTarget()
    {
        Window window = new();

        _ = new ThemeSwitch(window) { SelectedItem = PreviewTheme.Dark };

        Assert.Equal(ThemeVariant.Dark, window.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void Select_Light_SetsTheLightVariantOnTheTarget()
    {
        Window window = new();

        _ = new ThemeSwitch(window) { SelectedItem = PreviewTheme.Light };

        Assert.Equal(ThemeVariant.Light, window.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void Select_Default_ClearsTheVariantSoTheTargetUsesTheAppVariant()
    {
        Window window = new();
        ThemeSwitch themeSwitch = new(window) { SelectedItem = PreviewTheme.Dark };

        themeSwitch.SelectedItem = PreviewTheme.Default;

        Assert.False(window.IsSet(Window.RequestedThemeVariantProperty));
    }

    [AvaloniaFact]
    public void New_SelectsDefault()
    {
        ThemeSwitch themeSwitch = new(new Window());

        Assert.Equal(PreviewTheme.Default, themeSwitch.SelectedItem);
    }
}
