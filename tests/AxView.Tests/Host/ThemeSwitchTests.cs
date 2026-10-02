extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

using host::AxView.Host.Startup;
using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class ThemeSwitchTests
{
    [AvaloniaFact]
    public void Select_Dark_SetsTheDarkVariantOnTheTarget()
    {
        PreviewScope scope = new(TargetStyles.None);

        _ = new ThemeSwitch(scope) { SelectedItem = PreviewTheme.Dark };

        Assert.Equal(ThemeVariant.Dark, scope.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void Select_Light_SetsTheLightVariantOnTheTarget()
    {
        PreviewScope scope = new(TargetStyles.None);

        _ = new ThemeSwitch(scope) { SelectedItem = PreviewTheme.Light };

        Assert.Equal(ThemeVariant.Light, scope.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void Select_Default_SetsTheAppVariantOnTheTarget()
    {
        PreviewScope scope = new(new TargetStyles([], new ResourceDictionary(), [], ThemeVariant.Dark));
        ThemeSwitch themeSwitch = new(scope) { SelectedItem = PreviewTheme.Light };

        themeSwitch.SelectedItem = PreviewTheme.Default;

        Assert.Equal(ThemeVariant.Dark, scope.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void New_SelectsDefault()
    {
        ThemeSwitch themeSwitch = new(new PreviewScope(TargetStyles.None));

        Assert.Equal(PreviewTheme.Default, themeSwitch.SelectedItem);
    }
}
