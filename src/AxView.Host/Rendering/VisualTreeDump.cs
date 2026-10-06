using System.Text.Json;
using System.Text.Json.Serialization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AxView.Host.Rendering;

/// <summary>Saves the visual tree of a laid-out control as JSON. An agent reads the file to examine the layout.</summary>
/// <remarks>
/// The file has one <see cref="VisualTreeNode"/> object for each visual. A value that is null is not in the file.
/// The file has no indents, because the indents of a deep tree make the file many times larger.
/// This example has indents:
/// <code>
/// {
///   "Type": "StackPanel",
///   "Classes": [],
///   "X": 0, "Y": 0, "Width": 500, "Height": 300,
///   "IsVisible": true,
///   "DataContext": "Ui.ViewModels.MainViewModel",
///   "Children": [
///     { "Type": "TextBlock", "Name": "lblTitle", "Classes": [ "title" ], "X": 0, "Y": 0, "Width": 500, "Height": 19,
///       "IsVisible": true, "Text": "Hello", "Children": [] }
///   ]
/// }
/// </code>
/// </remarks>
internal static class VisualTreeDump
{
    private const int BoundsDecimals = 2;

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Writes the tree of the root and its visual descendants to a JSON file. Makes the folder if necessary.</summary>
    /// <param name="root">The root of the tree. The bounds of each visual are relative to it.</param>
    /// <param name="path">The path of the JSON file.</param>
    public static void Save(Visual root, string path)
    {
        VisualTreeNode tree = Describe(root, root, parentDataContext: null);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(tree, jsonOptions));
    }

    private static VisualTreeNode Describe(Visual visual, Visual root, object? parentDataContext)
    {
        object? dataContext = visual.DataContext;
        Rect bounds = new(visual.Bounds.Size);
        Matrix? transform = visual.TransformToVisual(root);
        if (transform is Matrix matrix)
        {
            bounds = bounds.TransformToAABB(matrix);
        }

        List<VisualTreeNode> children = [.. visual.GetVisualChildren().Select(child => Describe(child, root, dataContext))];
        return new VisualTreeNode(
            visual.GetType().Name,
            visual.Name,
            [.. visual.Classes],
            Math.Round(bounds.X, BoundsDecimals),
            Math.Round(bounds.Y, BoundsDecimals),
            Math.Round(bounds.Width, BoundsDecimals),
            Math.Round(bounds.Height, BoundsDecimals),
            visual.IsVisible,
            TextOf(visual),
            ReferenceEquals(dataContext, parentDataContext) ? null : dataContext?.GetType().FullName,
            children);
    }

    private static string? TextOf(Visual visual) =>
        visual switch
        {
            TextBlock textBlock => textBlock.Text,
            TextBox textBox => textBox.Text,
            _ => null,
        };
}
