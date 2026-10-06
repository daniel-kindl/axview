using System.Diagnostics.CodeAnalysis;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

using AxView.Host.Rendering;
using AxView.Host.Startup;
using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Shows only the preview of one file or one control, with no AxView chrome. A snapshot captures only the preview.</summary>
[SuppressMessage("Design", "CA1001", Justification = "OnClosed disposes the preview. A window has no Dispose.")]
internal sealed class SnapshotWindow : Window
{
    private const double DefaultWidth = 1280;
    private const double DefaultHeight = 800;
    private const double ScreenDpi = 96;

    private readonly PreviewScope scope;
    private readonly LivePreview? livePreview;
    private readonly bool isFullSize;

    /// <summary>Loads the file and shows its preview.</summary>
    /// <param name="renderer">The renderer of the project.</param>
    /// <param name="axamlPath">The AXAML file to capture.</param>
    /// <param name="target">The styles of the target app. The window becomes their owner.</param>
    /// <param name="viewModelType">The type of the root DataContext, or null to use the DataContext from the XAML.</param>
    public SnapshotWindow(PreviewRenderer renderer, string axamlPath, TargetStyles target, Type? viewModelType = null)
        : this(target)
    {
        livePreview = new LivePreview(renderer);
        livePreview.Changed += (_, _) => scope.Content = livePreview.View;
        livePreview.Show(axamlPath, viewModelType);
    }

    /// <summary>Shows a control, for example the control gallery. The snapshot has the full size of the control.</summary>
    /// <param name="content">The control to capture.</param>
    /// <param name="target">The styles of the target app. The window becomes their owner.</param>
    public SnapshotWindow(Control content, TargetStyles target)
        : this(target)
    {
        isFullSize = true;
        scope.Content = content;
    }

    private SnapshotWindow(TargetStyles target)
    {
        Width = DefaultWidth;
        Height = DefaultHeight;
        scope = new PreviewScope(target);
        Content = scope;
    }

    /// <summary>Gets or sets the size of the snapshot. <see cref="PreviewSize.Fit"/> gives the default window size.</summary>
    /// <remarks>A control that has the full size ignores this value.</remarks>
    public PreviewSize PreviewSize { get; set; } = PreviewSize.Fit;

    /// <summary>Gets the preview of the file, or null when the load failed or the window shows a control.</summary>
    public Control? View => livePreview?.View;

    /// <summary>Gets the error of the load or of a dispatcher job, or null.</summary>
    public string? Error => livePreview?.Error;

    /// <summary>Gets the place of the load error in the AXAML file, or null when it is not known.</summary>
    public XamlLocation? ErrorLocation => livePreview?.ErrorLocation;

    /// <summary>Gives the preview the variant of a theme.</summary>
    public void ShowTheme(PreviewTheme theme) => scope.ShowTheme(theme);

    /// <summary>Writes a PNG of the window. <see cref="Error"/> tells if the PNG shows the last good preview.</summary>
    /// <param name="pngPath">The path of the PNG.</param>
    public void Save(string pngPath)
    {
        Size clientSize = ClientSize;
        Visual captured = this;
        if (isFullSize)
        {
            // The operating system limits a window to the screen size. Thus lay out the preview again at its full size.
            // The next layout pass gives the preview the window size again.
            scope.Measure(Size.Infinity);
            clientSize = new Size(Math.Max(clientSize.Width, scope.DesiredSize.Width), scope.DesiredSize.Height);
            scope.Arrange(new Rect(clientSize));
            captured = scope;
        }
        else if (PreviewSize.Preset != PreviewSizePreset.Fit)
        {
            // The operating system limits a window to the screen size. Thus lay out the preview at the size, not the window.
            clientSize = new Size(PreviewSize.Width, PreviewSize.Height);
            scope.Measure(clientSize);
            scope.Arrange(new Rect(clientSize));
            captured = scope;
        }

        PixelSize size = PixelSize.FromSize(clientSize, RenderScaling);
        using RenderTargetBitmap bitmap = new(size, new Vector(ScreenDpi * RenderScaling, ScreenDpi * RenderScaling));
        bitmap.Render(captured);
        bitmap.Save(pngPath);
    }

    protected override void OnClosed(EventArgs e)
    {
        livePreview?.Dispose();
        base.OnClosed(e);
    }
}
