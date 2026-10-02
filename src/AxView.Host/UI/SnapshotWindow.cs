using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

using AxView.Host.Rendering;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Shows only the preview of one file, with no AxView chrome, so that a snapshot captures only the preview.</summary>
internal sealed class SnapshotWindow : Window
{
    private const double DefaultWidth = 1280;
    private const double DefaultHeight = 800;
    private const double ScreenDpi = 96;

    private readonly LivePreview livePreview;

    /// <summary>Loads the file and shows its preview.</summary>
    /// <param name="renderer">The renderer of the project.</param>
    /// <param name="axamlPath">The AXAML file to capture.</param>
    public SnapshotWindow(PreviewRenderer renderer, string axamlPath)
    {
        Width = DefaultWidth;
        Height = DefaultHeight;
        livePreview = new LivePreview(renderer);
        livePreview.Changed += (_, _) => Content = livePreview.View;
        livePreview.Show(axamlPath);
    }

    /// <summary>Writes a PNG of the window. Writes the error, if any, to the standard error stream.</summary>
    /// <param name="pngPath">The path of the PNG.</param>
    /// <returns><see cref="ExitReason.RenderFailed"/> when the load or a dispatcher job failed, else <see cref="ExitReason.Ok"/>.</returns>
    public ExitReason Save(string pngPath)
    {
        PixelSize size = PixelSize.FromSize(ClientSize, RenderScaling);
        using (RenderTargetBitmap bitmap = new(size, new Vector(ScreenDpi * RenderScaling, ScreenDpi * RenderScaling)))
        {
            bitmap.Render(this);
            bitmap.Save(pngPath);
        }

        if (livePreview.Error is null)
        {
            return ExitReason.Ok;
        }

        Console.Error.WriteLine($"axview: error: {livePreview.Error}");
        return ExitReason.RenderFailed;
    }

    protected override void OnClosed(EventArgs e)
    {
        livePreview.Dispose();
        base.OnClosed(e);
    }
}
