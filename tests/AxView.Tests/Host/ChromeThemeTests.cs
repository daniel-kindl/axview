extern alias host;

using Avalonia.Headless.XUnit;
using Avalonia.Styling;

using host::AxView.Host.Startup;

namespace AxView.Tests.Host;

public sealed class ChromeThemeTests
{
    [AvaloniaFact]
    public void Load_FluentInTheDeps_GivesTheFluentTheme()
    {
        IStyle? theme = ChromeTheme.Load();

        Assert.Equal("Avalonia.Themes.Fluent.FluentTheme", theme?.GetType().FullName);
    }
}
