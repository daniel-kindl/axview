namespace AxView.Protocol;

/// <summary>The size of the preview.</summary>
/// <param name="Preset">The selected size preset.</param>
/// <param name="Width">The width of the preview, in device-independent pixels. Not applicable to <see cref="PreviewSizePreset.Fit"/>.</param>
/// <param name="Height">The height of the preview, in device-independent pixels. Not applicable to <see cref="PreviewSizePreset.Fit"/>.</param>
internal sealed record PreviewSize(PreviewSizePreset Preset, double Width, double Height)
{
    /// <summary>The preview fills the free space of the window.</summary>
    public static PreviewSize Fit { get; } = new(PreviewSizePreset.Fit, 0, 0);
}
