namespace AxView.Host.UI;

/// <summary>One control kind in the <see cref="ControlGallery"/>. It has one row per variant and one column per state.</summary>
/// <param name="Title">The section header.</param>
/// <param name="States">The columns.</param>
/// <param name="Variants">The rows.</param>
internal sealed record GallerySection(string Title, IReadOnlyList<GalleryState> States, IReadOnlyList<GalleryVariant> Variants);
