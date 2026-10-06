extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;

namespace AxView.Tests.Host;

public sealed class SnapshotWindowTests
{
    private const string AvaloniaXmlns = "xmlns=\"https://github.com/avaloniaui\"";

    [AvaloniaFact]
    public void New_ShowsOnlyThePreviewOfTheFile()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");

        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None);
        window.Show();

        Assert.Equal("Hello", Assert.IsType<TextBlock>(Assert.IsType<PreviewScope>(window.Content).Content).Text);
        window.Close();
    }

    [AvaloniaFact]
    public void New_WithAViewModelType_SetsTheDataContext()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");

        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None, typeof(FixtureViewModel));
        window.Show();

        TextBlock text = Assert.IsType<TextBlock>(Assert.IsType<PreviewScope>(window.Content).Content);
        _ = Assert.IsType<FixtureViewModel>(text.DataContext);
        window.Close();
    }

    private static PreviewRenderer Renderer(TempDir dir) => new(typeof(SnapshotWindowTests).Assembly, dir.Path, DesignServices.None);
}
