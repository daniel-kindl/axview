using System.Text.RegularExpressions;
using System.Xml.Linq;

using Avalonia.Controls;

namespace AxView.Host.Rendering;

/// <summary>Gives the root control of a preview a DataContext from its XAML.</summary>
/// <remarks>
/// The sources, in order of priority:
/// <list type="number">
/// <item><description>The <c>Design.DataContext</c> of the root.</description></item>
/// <item><description>The DataContext that the code-behind constructor sets.</description></item>
/// <item><description>A new instance of the root <c>x:DataType</c>, if it has a parameterless constructor.</description></item>
/// </list>
/// </remarks>
internal static partial class DesignDataContext
{
    private static readonly XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>Removes a <c>Design.DataContext</c> element when the loader cannot create its type.</summary>
    /// <param name="xaml">The AXAML text.</param>
    /// <param name="findType">Returns the type with a CLR namespace and a name, or null.</param>
    /// <returns>The text with no change, or the text with empty lines in place of the element.</returns>
    /// <remarks>The empty lines keep the line numbers of later load errors correct.</remarks>
    public static string RemoveIfNotCreatable(string xaml, Func<string, string, Type?> findType) =>
        DesignElement().Replace(xaml, match =>
        {
            Type? type = FindClrType(xaml, match.Groups["prefix"].Value, match.Groups["name"].Value, findType);
            return type is null || IsCreatable(type) ? match.Value : new string('\n', match.Value.Count(c => c == '\n'));
        });

    /// <summary>Sets the DataContext of the root control.</summary>
    /// <param name="root">The loaded root control.</param>
    /// <param name="xaml">The AXAML text that the root came from.</param>
    /// <param name="findType">Returns the type with a CLR namespace and a name, or null.</param>
    /// <remarks>The constructor of the <c>x:DataType</c> is user code. It can throw any exception.</remarks>
    public static void Apply(Control root, string xaml, Func<string, string, Type?> findType)
    {
        // Also applies Design.Width, Design.Height, and Design.DesignStyle.
        Design.ApplyDesignModeProperties(root, root);
        if (root.DataContext is not null)
        {
            return;
        }

        Type? dataType = FindDataType(xaml, findType);
        if (dataType is not null && IsCreatable(dataType))
        {
            root.DataContext = Activator.CreateInstance(dataType);
        }
    }

    private static bool IsCreatable(Type type) => type.GetConstructor(Type.EmptyTypes) is not null;

    // Reads x:DataType="prefix:Name" of the root element. A DataTemplate can have its own x:DataType.
    private static Type? FindDataType(string xaml, Func<string, string, Type?> findType)
    {
        XElement root = XDocument.Parse(xaml).Root!;
        string[] parts = ((string?)root.Attribute(xamlNamespace + "DataType"))?.Split(':') ?? [];
        return parts.Length == 2 ? FindClrType(xaml, parts[0], parts[1], findType) : null;
    }

    private static Type? FindClrType(string xaml, string prefix, string name, Func<string, string, Type?> findType)
    {
        Match? declaration = ClrNamespaceDeclaration().Matches(xaml).FirstOrDefault(m => m.Groups["prefix"].Value == prefix);
        return declaration is null ? null : findType(declaration.Groups["ns"].Value, name);
    }

    // <Design.DataContext><prefix:Name ... /></Design.DataContext>, the form that the Avalonia templates use.
    [GeneratedRegex("""<Design\.DataContext>\s*<(?<prefix>[\w.-]+):(?<name>[\w.]+)\b[^>]*?/>\s*</Design\.DataContext>""")]
    private static partial Regex DesignElement();

    [GeneratedRegex("""xmlns:(?<prefix>[\w.-]+)\s*=\s*(?<q>["'])(?:using|clr-namespace):(?<ns>[\w.]+)[^"']*\k<q>""")]
    private static partial Regex ClrNamespaceDeclaration();
}
