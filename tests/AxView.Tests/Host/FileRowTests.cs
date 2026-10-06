extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;

using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class FileRowTests
{
    [Theory]
    [InlineData("View", "View")]
    [InlineData("Styles", "Styles")]
    [InlineData("Resources", "Resources")]
    [InlineData("Gallery", "Gallery")]
    public void IconOf_GivesTheIconOfTheKind(string kind, string icon) =>
        Assert.Equal(Enum.Parse<ChromeIcon>(icon), FileRow.IconOf(Enum.Parse<FileKind>(kind)));

    [AvaloniaFact]
    public void New_ShowsTheIcon_TheTitle_AndTheFolder()
    {
        FileRow row = new(new FileListEntry("Main", "Views", "Views/Main.axaml", FileKind.View));

        _ = Assert.Single(row.GetLogicalDescendants().OfType<PathIcon>());
        Assert.Equal(["Main", "Views"], row.GetLogicalDescendants().OfType<TextBlock>().Select(text => text.Text));
        Assert.Equal("Views/Main.axaml", ToolTip.GetTip(row));
    }
}
