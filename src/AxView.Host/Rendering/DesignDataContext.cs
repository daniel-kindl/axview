using System.Text.RegularExpressions;
using System.Xml.Linq;

using Avalonia.Controls;

namespace AxView.Host.Rendering;

/// <summary>Gives the controls of a preview a DataContext from their XAML.</summary>
/// <remarks>
/// A control with a <c>d:DataContext</c> with a <c>d:DesignInstance</c> gets an instance of that type.
/// The sources for the root, in order of priority:
/// <list type="number">
/// <item><description>
/// The <c>Design.DataContext</c> of the root, or its <c>d:DataContext</c> with a <c>d:DesignInstance</c>.
/// </description></item>
/// <item><description>The DataContext that the code-behind constructor sets.</description></item>
/// <item><description>
/// The type of the root <c>Design.DataContext</c> that the loader cannot create. Its content is ignored.
/// </description></item>
/// <item><description>A new instance of the root <c>x:DataType</c>.</description></item>
/// </list>
/// The <see cref="DesignServices"/> create the types of the last two sources.
/// </remarks>
internal static partial class DesignDataContext
{
    private const string ExtensionPrefix = "axview";
    private const string XamlPrefix = "axviewx";
    private const string DesignDataContextElement = "Design.DataContext";

    private static readonly XNamespace xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>Changes the design DataContexts into a form that the loader can create.</summary>
    /// <param name="xaml">The AXAML text.</param>
    /// <param name="findType">Returns the type with a CLR namespace and a name, or null.</param>
    /// <returns>The changed text.</returns>
    /// <remarks>
    /// The loader has no <c>d:DesignInstance</c>. A <c>d:DataContext</c> with it becomes a
    /// <c>DataContext</c> with a <see cref="DesignInstanceExtension"/>. The root then declares the extension namespace.
    /// These parts go when their type has no parameterless constructor:
    /// <list type="bullet">
    /// <item><description>A <c>d:DataContext</c> with a <c>d:DesignInstance</c>.</description></item>
    /// <item><description>A <c>Design.DataContext</c> element.</description></item>
    /// </list>
    /// Blank text replaces a removed part. This keeps the line numbers of later load errors correct.
    /// </remarks>
    public static string PrepareForLoader(string xaml, Func<string, string, Type?> findType)
    {
        bool hasInstance = false;
        string withInstances = DesignInstanceAttribute().Replace(xaml, match =>
        {
            Match value = DesignInstanceType().Match(match.Groups["arguments"].Value);
            Type? type = value.Success ? FindClrType(xaml, value.Groups["prefix"].Value, value.Groups["name"].Value, findType) : null;
            if (type is null || !IsCreatable(type))
            {
                return new string(' ', match.Length);
            }

            hasInstance = true;
            string quote = match.Groups["q"].Value;
            return $"DataContext={quote}{{{ExtensionPrefix}:DesignInstance {{{XamlPrefix}:Type {value.Value}}}}}{quote}";
        });
        string creatable = DesignElement().Replace(withInstances, match =>
        {
            Type? type = FindClrType(xaml, match.Groups["prefix"].Value, match.Groups["name"].Value, findType);
            return type is null || IsCreatable(type) ? match.Value : new string('\n', match.Value.Count(c => c == '\n'));
        });
        return hasInstance ? DeclareExtensionNamespaces(creatable) : creatable;
    }

    /// <summary>Sets the DataContext of the root control.</summary>
    /// <param name="root">The loaded root control.</param>
    /// <param name="xaml">The AXAML text that the root came from.</param>
    /// <param name="findType">Returns the type with a CLR namespace and a name, or null.</param>
    /// <param name="services">The design services that create the DataContext.</param>
    /// <remarks>The constructors of the design types are user code. They can throw any exception.</remarks>
    public static void Apply(Control root, string xaml, Func<string, string, Type?> findType, DesignServices services)
    {
        // Also applies Design.Width, Design.Height, and Design.DesignStyle.
        Design.ApplyDesignModeProperties(root, root);
        XElement element = XDocument.Parse(xaml).Root!;
        root.DataContext ??= services.CreateOrNull(FindDesignType(element, findType))
            ?? services.CreateOrNull(FindDataType(element, xaml, findType));
    }

    private static bool IsCreatable(Type type) => type.GetConstructor(Type.EmptyTypes) is not null;

    // Reads the type of the first element in <Design.DataContext> of the root.
    private static Type? FindDesignType(XElement root, Func<string, string, Type?> findType)
    {
        XElement? instance = root.Element(root.Name.Namespace + DesignDataContextElement)?.Elements().FirstOrDefault();
        Match? clrNamespace = instance is null ? null : XmlClrNamespace().Match(instance.Name.NamespaceName);
        return clrNamespace is { Success: true } ? findType(clrNamespace.Groups["ns"].Value, instance!.Name.LocalName) : null;
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

    private static string DeclareExtensionNamespaces(string xaml)
    {
        Type extension = typeof(DesignInstanceExtension);
        string extensionNamespace = $"clr-namespace:{extension.Namespace};assembly={extension.Assembly.GetName().Name}";
        string declarations = $" xmlns:{ExtensionPrefix}=\"{extensionNamespace}\" xmlns:{XamlPrefix}=\"{xamlNamespace.NamespaceName}\"";
        return RootElementName().Replace(xaml, match => match.Value + declarations, 1);
    }

    // <Design.DataContext><prefix:Name ... /></Design.DataContext>, also with content in the prefix:Name element.
    [GeneratedRegex(
        """<Design\.DataContext>\s*<(?<prefix>[\w.-]+):(?<name>[\w.]+)\b.*?</Design\.DataContext>""",
        RegexOptions.Singleline)]
    private static partial Regex DesignElement();

    // The d:DataContext="{d:DesignInstance ...}" attribute of any element.
    [GeneratedRegex("""(?<d>[\w.-]+):DataContext\s*=\s*(?<q>["'])\{\k<d>:DesignInstance\b(?<arguments>[^"']*)\k<q>""")]
    private static partial Regex DesignInstanceAttribute();

    // The type in the arguments of a d:DesignInstance. These forms are possible:
    // prefix:Name, Type=prefix:Name, Type={x:Type prefix:Name}, Type={x:Type TypeName=prefix:Name}
    [GeneratedRegex("""(?<=^\s+(?:Type\s*=\s*)?(?:\{[\w.-]+:Type\s+(?:TypeName\s*=\s*)?)?)(?<prefix>[\w.-]+):(?<name>[\w.]+)""")]
    private static partial Regex DesignInstanceType();

    // The name of the root element, after the XML declaration and comments.
    [GeneratedRegex("""\A(?:\s|<\?.*?\?>|<!--.*?-->)*<[\w.:-]+""", RegexOptions.Singleline)]
    private static partial Regex RootElementName();

    // The value of a CLR namespace declaration: using:Name or clr-namespace:Name;assembly=...
    [GeneratedRegex("""^(?:using|clr-namespace):(?<ns>[\w.]+)""")]
    private static partial Regex XmlClrNamespace();

    [GeneratedRegex("""xmlns:(?<prefix>[\w.-]+)\s*=\s*(?<q>["'])(?:using|clr-namespace):(?<ns>[\w.]+)[^"']*\k<q>""")]
    private static partial Regex ClrNamespaceDeclaration();
}
