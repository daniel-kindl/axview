using Avalonia.Headless.XUnit;

using AxView.Picker;
using AxView.Protocol;
using AxView.Tests.Fixtures;

namespace AxView.Tests.Launcher;

public sealed class AppIconTests
{
    [AvaloniaFact]
    public void Get_GivesTheSameIconEachTime() => Assert.Same(AppIcon.Get(), AppIcon.Get());

    [AvaloniaFact]
    public void PickerWindow_AndAboutDialog_HaveTheIcon()
    {
        using TempDir dir = new();

        PickerWindow picker = new(new RecentFiles(Path.Combine(dir.Path, "settings.json")));
        AboutDialog about = new(new AboutInfo("1.0.0", new DateOnly(2026, 10, 2), "11.3.22"));

        Assert.Same(AppIcon.Get(), picker.Icon);
        Assert.Same(AppIcon.Get(), about.Icon);
    }
}
