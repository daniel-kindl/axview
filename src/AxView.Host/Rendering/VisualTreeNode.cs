namespace AxView.Host.Rendering;

/// <summary>One visual of the tree that <see cref="VisualTreeDump"/> saves.</summary>
/// <param name="Type">The short type name of the visual, for example <c>TextBlock</c>.</param>
/// <param name="Name">The name of the visual, or null when it has none.</param>
/// <param name="Classes">The style classes and the pseudo-classes of the visual, for example <c>:disabled</c>.</param>
/// <param name="X">The left edge, relative to the root of the tree, in device-independent pixels.</param>
/// <param name="Y">The top edge, relative to the root of the tree, in device-independent pixels.</param>
/// <param name="Width">The width of the visual, in device-independent pixels.</param>
/// <param name="Height">The height of the visual, in device-independent pixels.</param>
/// <param name="IsVisible">The value of the IsVisible property of the visual. A parent can still hide the visual.</param>
/// <param name="Text">The text of a TextBlock or a TextBox, or null for other visuals.</param>
/// <param name="DataContext">The full type name of the DataContext, or null when the DataContext is the same as in the parent.</param>
/// <param name="Children">The visual children.</param>
internal sealed record VisualTreeNode(
    string Type,
    string? Name,
    IReadOnlyList<string> Classes,
    double X,
    double Y,
    double Width,
    double Height,
    bool IsVisible,
    string? Text,
    string? DataContext,
    IReadOnlyList<VisualTreeNode> Children);
