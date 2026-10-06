using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;

using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class ChromeStyleTests
{
    public static TheoryData<string> ThemedBrushKeys =>
    [
        ChromeStyle.BorderBrushKey,
        ChromeStyle.MutedTextBrushKey,
        ChromeStyle.AccentBrushKey,
        ChromeStyle.WarningBrushKey,
        ChromeStyle.ErrorBrushKey,
        ChromeStyle.StageBrushKey,
        ChromeStyle.CardBrushKey,
    ];

    [AvaloniaTheory]
    [MemberData(nameof(ThemedBrushKeys))]
    public void CreateResources_GivesTheBrushPerThemeVariant(string key)
    {
        ResourceDictionary resources = ChromeStyle.CreateResources();

        Assert.True(resources.TryGetResource(key, ThemeVariant.Light, out object? light));
        Assert.True(resources.TryGetResource(key, ThemeVariant.Dark, out object? dark));
        Color lightColor = Assert.IsType<ISolidColorBrush>(light, exactMatch: false).Color;
        Color darkColor = Assert.IsType<ISolidColorBrush>(dark, exactMatch: false).Color;
        Assert.NotEqual(lightColor, darkColor);
    }

    [AvaloniaFact]
    public void SetBrush_FollowsTheThemeVariant()
    {
        Border border = new();
        Window window = new() { Content = border, RequestedThemeVariant = ThemeVariant.Dark };
        window.Resources.MergedDictionaries.Add(ChromeStyle.CreateResources());

        ChromeStyle.SetBrush(border, Border.BorderBrushProperty, ChromeStyle.BorderBrushKey);
        Color dark = Assert.IsType<ISolidColorBrush>(border.BorderBrush, exactMatch: false).Color;
        window.RequestedThemeVariant = ThemeVariant.Light;
        Color light = Assert.IsType<ISolidColorBrush>(border.BorderBrush, exactMatch: false).Color;

        Assert.Equal(ChromeBrushes.ColorOf(ChromeStyle.BorderBrushKey, ThemeVariant.Dark), dark);
        Assert.Equal(ChromeBrushes.ColorOf(ChromeStyle.BorderBrushKey, ThemeVariant.Light), light);
    }

    [AvaloniaFact]
    public void PickerWindow_TakesEachBrushFromTheChromeStyle()
    {
        using TempDir dir = new();

        Picker.PickerWindow window = new(new Picker.RecentFiles(Path.Combine(dir.Path, "settings.json")))
        {
            RequestedThemeVariant = ThemeVariant.Dark,
        };

        ChromeBrushes.AssertFromChromeStyle(window, ThemeVariant.Dark);
    }
}
