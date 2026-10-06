using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

using AxView.Protocol;

namespace AxView.Tests.Launcher;

public sealed class ChromeIconsTests
{
    // The icon type is internal. Thus the data gives the icon names.
    public static TheoryData<string> IconNames => [.. Enum.GetNames<ChromeIcon>()];

    [AvaloniaTheory]
    [MemberData(nameof(IconNames))]
    public void CreateGeometry_FillsTheViewBox(string iconName)
    {
        ChromeIcon icon = Enum.Parse<ChromeIcon>(iconName);
        Geometry geometry = ChromeIcons.CreateGeometry(icon);

        Rect viewBox = new(0, 0, ChromeIcons.Size, ChromeIcons.Size);
        Assert.True(viewBox.Contains(geometry.Bounds), $"{icon} bounds {geometry.Bounds} are outside the view box.");
        Assert.True(geometry.Bounds.Width >= ChromeIcons.Size / 2, $"{icon} is too narrow.");
        Assert.True(geometry.Bounds.Height >= ChromeIcons.Size / 2, $"{icon} is too low.");
    }

    [AvaloniaFact]
    public void CreateGeometry_GivesANewGeometryEachTime() =>
        Assert.NotSame(ChromeIcons.CreateGeometry(ChromeIcon.Open), ChromeIcons.CreateGeometry(ChromeIcon.Open));

    [AvaloniaFact]
    public void CreateGeometry_RejectsAnUnknownIcon() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ChromeIcons.CreateGeometry((ChromeIcon)(-1)));
}
