using Avalonia.Controls;

namespace AxView.Host.Rendering;

/// <summary>The result of one render of an AXAML file.</summary>
/// <param name="Control">The control that the preview window shows.</param>
/// <param name="IsStylePreview">
/// True when the root is a <c>Styles</c>, <c>Style</c>, or <c>ResourceDictionary</c>.
/// Then the control is the <c>Design.PreviewWith</c> control, and it needs no DataContext.
/// </param>
internal sealed record RenderedView(Control Control, bool IsStylePreview);
