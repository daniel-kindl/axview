extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;
using host::AxView.Host.Startup;
using host::AxView.Host.UI;
using host::AxView.Protocol;

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
    public void Save_GoodFile_GivesOk()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", $"""<TextBlock {AvaloniaXmlns} Text="Hello" />""");
        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None);
        window.Show();

        ExitReason reason = window.Save(Path.Combine(dir.Path, "hello.png"));

        Assert.Equal(ExitReason.Ok, reason);
        window.Close();
    }

    [AvaloniaFact]
    public void Save_BrokenFile_GivesRenderFailed()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Broken.axaml", "<TextBlock");
        SnapshotWindow window = new(Renderer(dir), file, TargetStyles.None);
        window.Show();

        ExitReason reason = window.Save(Path.Combine(dir.Path, "broken.png"));

        Assert.Equal(ExitReason.RenderFailed, reason);
        window.Close();
    }

    private static PreviewRenderer Renderer(TempDir dir) => new(typeof(SnapshotWindowTests).Assembly, dir.Path, DesignServices.None);
}
