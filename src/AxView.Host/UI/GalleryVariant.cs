using Avalonia.Controls;

namespace AxView.Host.UI;

/// <summary>One row of a <see cref="GallerySection"/>, for example a checked CheckBox.</summary>
/// <param name="Name">The row header.</param>
/// <param name="Create">Creates a new control for one cell of the row.</param>
internal sealed record GalleryVariant(string Name, Func<Control> Create);
