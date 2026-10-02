extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

using AxView.Tests.Fixtures;

using host::AxView.Host.Rendering;

namespace AxView.Tests.Host;

public sealed class LivePreviewTests
{
    private const string AvaloniaXmlns = "xmlns=\"https://github.com/avaloniaui\"";
    private const string BrokenXaml = "<TextBlock";
    private static readonly TimeSpan changeTimeout = TimeSpan.FromSeconds(3);

    [AvaloniaFact]
    public void Show_GivesThePreviewOfTheFile()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", TextBlock("Hello"));
        using LivePreview preview = new(Renderer(dir));

        preview.Show(file);

        Assert.Equal("Hello", TextOf(preview));
        Assert.Null(preview.Error);
        Assert.Equal(Path.GetFullPath(file), preview.FilePath);
    }

    [AvaloniaFact]
    public void Show_StylesFile_TellsThatThePreviewIsAStylePreview()
    {
        using TempDir dir = new();
        string file = dir.Write("Styles/Texts.axaml", $"""
            <Styles {AvaloniaXmlns}>
              <Design.PreviewWith>
                <Border />
              </Design.PreviewWith>
            </Styles>
            """);
        using LivePreview preview = new(Renderer(dir));

        preview.Show(file);

        Assert.True(preview.IsStylePreview);
    }

    [AvaloniaFact]
    public void Show_GivesTheRenderedViewBeforeTheChange()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", TextBlock("Hello"));
        using LivePreview preview = new(Renderer(dir));
        object? root = null;
        bool isChangedBeforeRender = false;
        preview.Rendered += (_, view) => root = view.Control;
        preview.Changed += (_, _) => isChangedBeforeRender = root is null;

        preview.Show(file);

        Assert.Same(preview.View, root);
        Assert.False(isChangedBeforeRender);
    }

    [AvaloniaFact]
    public void Show_BrokenFile_GivesAnErrorAndNoView()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Broken.axaml", BrokenXaml);
        using LivePreview preview = new(Renderer(dir));

        preview.Show(file);

        Assert.Null(preview.View);
        Assert.NotNull(preview.Error);
    }

    [AvaloniaFact]
    public void Show_AnotherFile_ForgetsTheLastGoodPreviewOfTheFirstFile()
    {
        using TempDir dir = new();
        string good = dir.Write("Views/Good.axaml", TextBlock("Good"));
        string broken = dir.Write("Views/Broken.axaml", BrokenXaml);
        using LivePreview preview = new(Renderer(dir));
        preview.Show(good);

        preview.Show(broken);

        Assert.Null(preview.View);
        Assert.NotNull(preview.Error);
    }

    [AvaloniaFact]
    public void Save_KeepsTheSelectedViewModel()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", TextBlock("Before"));
        using LivePreview preview = new(Renderer(dir));
        preview.Show(file, typeof(FixtureOtherViewModel));

        File.WriteAllText(file, TextBlock("After"));

        Assert.True(WaitForChange(preview));
        Assert.Equal("After", TextOf(preview));
        _ = Assert.IsType<FixtureOtherViewModel>(preview.View!.DataContext);
    }

    [AvaloniaFact]
    public void Save_LoadsTheFileAgain()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", TextBlock("Before"));
        using LivePreview preview = new(Renderer(dir));
        preview.Show(file);

        File.WriteAllText(file, TextBlock("After"));

        Assert.True(WaitForChange(preview));
        Assert.Equal("After", TextOf(preview));
    }

    [AvaloniaFact]
    public void Reload_KeepsTheSelectedViewModel()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", TextBlock("Hello"));
        using LivePreview preview = new(Renderer(dir));
        preview.Show(file, typeof(FixtureOtherViewModel));
        Control first = preview.View!;

        preview.Reload();

        Assert.NotSame(first, preview.View);
        _ = Assert.IsType<FixtureOtherViewModel>(preview.View!.DataContext);
    }

    [AvaloniaFact]
    public void Save_BrokenXaml_KeepsTheLastGoodPreviewAndGivesAnError()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", TextBlock("Good"));
        using LivePreview preview = new(Renderer(dir));
        preview.Show(file);

        File.WriteAllText(file, BrokenXaml);

        Assert.True(WaitForChange(preview));
        Assert.Equal("Good", TextOf(preview));
        Assert.NotNull(preview.Error);
    }

    [AvaloniaFact]
    public void Save_FixedXaml_ClearsTheError()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", BrokenXaml);
        using LivePreview preview = new(Renderer(dir));
        preview.Show(file);

        File.WriteAllText(file, TextBlock("Fixed"));

        Assert.True(WaitForChange(preview));
        Assert.Equal("Fixed", TextOf(preview));
        Assert.Null(preview.Error);
    }

    [AvaloniaFact]
    public void Save_OfAFileThatIsNotShown_DoesNotLoadAgain()
    {
        using TempDir dir = new();
        string first = dir.Write("Views/First.axaml", TextBlock("First"));
        string second = dir.Write("Views/Second.axaml", TextBlock("Second"));
        using LivePreview preview = new(Renderer(dir));
        preview.Show(first);
        preview.Show(second);

        File.WriteAllText(first, TextBlock("Changed"));

        Assert.False(WaitForChange(preview));
        Assert.Equal("Second", TextOf(preview));
    }

    [AvaloniaFact]
    public void Save_AfterDispose_DoesNotLoadAgain()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", TextBlock("Before"));
        LivePreview preview = new(Renderer(dir));
        preview.Show(file);

        preview.Dispose();
        File.WriteAllText(file, TextBlock("After"));

        Assert.False(WaitForChange(preview));
    }

    [AvaloniaFact]
    public void DispatcherError_KeepsTheLastGoodPreviewAndGivesTheError()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", TextBlock("Good"));
        using LivePreview preview = new(Renderer(dir));
        preview.Show(file);
        int changes = 0;
        preview.Changed += (_, _) => changes++;

        Dispatcher.UIThread.Post(() => throw new InvalidOperationException("Layout failed."));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, changes);
        Assert.Equal("Good", TextOf(preview));
        Assert.Equal("InvalidOperationException: Layout failed.", preview.Error);
    }

    [AvaloniaFact]
    public void Show_RaisesChanged()
    {
        using TempDir dir = new();
        string file = dir.Write("Views/Hello.axaml", TextBlock("Hello"));
        using LivePreview preview = new(Renderer(dir));
        int changes = 0;
        preview.Changed += (_, _) => changes++;

        preview.Show(file);

        Assert.Equal(1, changes);
    }

    private static string TextBlock(string text) => $"""<TextBlock {AvaloniaXmlns} Text="{text}" />""";

    private static string? TextOf(LivePreview preview) => Assert.IsType<TextBlock>(preview.View).Text;

    private static PreviewRenderer Renderer(TempDir dir) => new(typeof(LivePreviewTests).Assembly, dir.Path, DesignServices.None);

    // Run the dispatcher loop until the preview changes, or until the timeout.
    private static bool WaitForChange(LivePreview preview)
    {
        bool isChanged = false;
        DispatcherFrame frame = new();
        void OnChanged(object? sender, EventArgs e)
        {
            isChanged = true;
            frame.Continue = false;
        }

        preview.Changed += OnChanged;
        using (DispatcherTimer.RunOnce(() => frame.Continue = false, changeTimeout))
        {
            Dispatcher.UIThread.PushFrame(frame);
        }

        preview.Changed -= OnChanged;
        return isChanged;
    }
}
