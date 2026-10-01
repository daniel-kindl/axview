using System.Text.RegularExpressions;

namespace AxView.Host.Rendering;

/// <summary>Adds the owning assembly to each CLR namespace declaration that does not name one.</summary>
/// <remarks>
/// Runtime-loaded XAML keeps "using:Ns" without an assembly. The runtime type resolver then has no assembly
/// to search, so a type in a binding path (for example $parent[local:MyView]) fails. Compiled XAML does not
/// have this problem.
/// </remarks>
internal static partial class XmlnsQualifier
{
    /// <summary>Rewrites "using:Ns" and "clr-namespace:Ns" to "clr-namespace:Ns;assembly=Owner".</summary>
    /// <param name="xaml">The AXAML text.</param>
    /// <param name="findOwner">Returns the one assembly that defines a namespace, or null.</param>
    /// <returns>The rewritten text. Declarations with no known owner do not change.</returns>
    public static string Qualify(string xaml, Func<string, string?> findOwner) =>
        ClrNamespace().Replace(xaml, match =>
        {
            string clrNamespace = match.Groups["ns"].Value;
            string? owner = findOwner(clrNamespace);
            if (owner is null)
            {
                return match.Value;
            }

            string quote = match.Groups["q"].Value;
            return $"{match.Groups["attr"].Value}={quote}clr-namespace:{clrNamespace};assembly={owner}{quote}";
        });

    [GeneratedRegex("""(?<attr>xmlns(?::[\w.-]+)?)\s*=\s*(?<q>["'])(?:using|clr-namespace):(?<ns>[\w.]+)\k<q>""")]
    private static partial Regex ClrNamespace();
}
