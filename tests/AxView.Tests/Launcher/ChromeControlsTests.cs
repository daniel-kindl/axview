using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;

using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class ChromeControlsTests
{
    [AvaloniaFact]
    public void CreateIconButton_ShowsTheIconAndTheTip()
    {
        Button button = ChromeControls.CreateIconButton(ChromeIcon.About, "About AxView");

        PathIcon icon = Assert.IsType<PathIcon>(button.Content);
        Assert.NotNull(icon.Data);
        Assert.Equal("About AxView", ToolTip.GetTip(button));
        Assert.Equal("About AxView", AutomationProperties.GetName(button));
    }

    [AvaloniaFact]
    public void CreateSeparator_IsAVerticalLineWithTheBorderBrush()
    {
        Border separator = ChromeControls.CreateSeparator();
        Window window = new() { Content = separator, RequestedThemeVariant = ThemeVariant.Dark };
        window.Resources.MergedDictionaries.Add(ChromeStyle.CreateResources());

        Assert.Equal(1, separator.Width);
        Color color = Assert.IsType<ISolidColorBrush>(separator.Background, exactMatch: false).Color;
        Assert.Equal(ChromeBrushes.ColorOf(ChromeStyle.BorderBrushKey, ThemeVariant.Dark), color);
    }
}
