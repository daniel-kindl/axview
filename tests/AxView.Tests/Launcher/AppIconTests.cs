using Avalonia.Headless.XUnit;

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

        AxView.Picker.PickerWindow picker = new(new AxView.Picker.RecentFiles(Path.Combine(dir.Path, "settings.json")));
        AboutDialog about = new(new AboutInfo("1.0.0", new DateOnly(2026, 10, 2), "11.3.22"));

        Assert.Same(AppIcon.Get(), picker.Icon);
        Assert.Same(AppIcon.Get(), about.Icon);
    }
}
