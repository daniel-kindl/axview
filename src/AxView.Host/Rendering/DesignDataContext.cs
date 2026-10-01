using System.Text.RegularExpressions;
using System.Xml.Linq;

using Avalonia.Controls;

namespace AxView.Host.Rendering;

/// <summary>Gives the root control of a preview a DataContext from its XAML.</summary>
/// <remarks>
/// The sources, in order of priority:
/// <list type="number">
/// <item><description>
/// The <c>Design.DataContext</c> of the root, or its <c>d:DataContext</c> with a <c>d:DesignInstance</c>.
/// </description></item>
/// <item><description>The DataContext that the code-behind constructor sets.</description></item>
/// <item><description>A new instance of the root <c>x:DataType</c>, if it has a parameterless constructor.</description></item>
/// </list>
/// </remarks>
internal static partial class DesignDataContext
{
    private static readonly XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly XNamespace blendNamespace = "http://schemas.microsoft.com/expression/blend/2008";

    /// <summary>Removes the design DataContexts that the loader cannot create.</summary>
    /// <param name="xaml">The AXAML text.</param>
    /// <param name="findType">Returns the type with a CLR namespace and a name, or null.</param>
    /// <returns>The text with no change, or the text with blank text in place of the removed parts.</returns>
    /// <remarks>
    /// The loader has no <c>d:DesignInstance</c>, so each <c>d:DataContext</c> with it goes.
    /// <see cref="Apply"/> creates the type of the <c>d:DesignInstance</c>.
    /// A <c>Design.DataContext</c> element goes when its type has no parameterless constructor.
    /// The blank text keeps the line numbers and positions of later load errors correct.
    /// </remarks>
    public static string RemoveIfNotCreatable(string xaml, Func<string, string, Type?> findType)
    {
        string withoutInstances = DesignInstanceAttribute().Replace(xaml, match => new string(' ', match.Length));
        return DesignElement().Replace(withoutInstances, match =>
        {
            Type? type = FindClrType(xaml, match.Groups["prefix"].Value, match.Groups["name"].Value, findType);
            return type is null || IsCreatable(type) ? match.Value : new string('\n', match.Value.Count(c => c == '\n'));
        });
    }

    /// <summary>Sets the DataContext of the root control.</summary>
    /// <param name="root">The loaded root control.</param>
    /// <param name="xaml">The AXAML text that the root came from.</param>
    /// <param name="findType">Returns the type with a CLR namespace and a name, or null.</param>
    /// <remarks>The constructors of the design types are user code. They can throw any exception.</remarks>
    public static void Apply(Control root, string xaml, Func<string, string, Type?> findType)
    {
        // Also applies Design.Width, Design.Height, and Design.DesignStyle.
        Design.ApplyDesignModeProperties(root, root);
        XElement element = XDocument.Parse(xaml).Root!;
        object? designInstance = CreateOrNull(FindDesignInstanceType(element, xaml, findType));
        if (designInstance is not null)
        {
            root.DataContext = designInstance;
            return;
        }

        root.DataContext ??= CreateOrNull(FindDataType(element, xaml, findType));
    }

    private static bool IsCreatable(Type type) => type.GetConstructor(Type.EmptyTypes) is not null;

    private static object? CreateOrNull(Type? type) =>
        type is not null && IsCreatable(type) ? Activator.CreateInstance(type) : null;

    // Reads d:DataContext="{d:DesignInstance prefix:Name}" of the root element.
    private static Type? FindDesignInstanceType(XElement root, string xaml, Func<string, string, Type?> findType)
    {
        string value = (string?)root.Attribute(blendNamespace + "DataContext") ?? string.Empty;
        Match match = DesignInstanceValue().Match(value);
        return match.Success ? FindClrType(xaml, match.Groups["prefix"].Value, match.Groups["name"].Value, findType) : null;
    }

    // Reads x:DataType="prefix:Name" of the root element. A DataTemplate can have its own x:DataType.
    private static Type? FindDataType(XElement root, string xaml, Func<string, string, Type?> findType)
    {
        string[] parts = ((string?)root.Attribute(xamlNamespace + "DataType"))?.Split(':') ?? [];
        return parts.Length == 2 ? FindClrType(xaml, parts[0], parts[1], findType) : null;
    }

    private static Type? FindClrType(string xaml, string prefix, string name, Func<string, string, Type?> findType)
    {
        Match? declaration = ClrNamespaceDeclaration().Matches(xaml).FirstOrDefault(m => m.Groups["prefix"].Value == prefix);
        return declaration is null ? null : findType(declaration.Groups["ns"].Value, name);
    }

    // <Design.DataContext><prefix:Name ... /></Design.DataContext>, also with content in the prefix:Name element.
    [GeneratedRegex(
        """<Design\.DataContext>\s*<(?<prefix>[\w.-]+):(?<name>[\w.]+)\b.*?</Design\.DataContext>""",
        RegexOptions.Singleline)]
    private static partial Regex DesignElement();

    // The d:DataContext="{d:DesignInstance ...}" attribute of any element.
    [GeneratedRegex("""(?<d>[\w.-]+):DataContext\s*=\s*(?<q>["'])\{\k<d>:DesignInstance\b[^"']*\k<q>""")]
    private static partial Regex DesignInstanceAttribute();

    // {d:DesignInstance prefix:Name, ...} or {d:DesignInstance Type=prefix:Name, ...}
    [GeneratedRegex("""^\{[\w.-]+:DesignInstance\s+(?:Type\s*=\s*)?(?<prefix>[\w.-]+):(?<name>[\w.]+)""")]
    private static partial Regex DesignInstanceValue();

    [GeneratedRegex("""xmlns:(?<prefix>[\w.-]+)\s*=\s*(?<q>["'])(?:using|clr-namespace):(?<ns>[\w.]+)[^"']*\k<q>""")]
    private static partial Regex ClrNamespaceDeclaration();
}
