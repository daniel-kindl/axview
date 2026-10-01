using System.Reflection;
using System.Text.RegularExpressions;

namespace AxView.Host;

/// <summary>Keeps the <c>x:Class</c> directive only when the XAML loader can create the code-behind type.</summary>
/// <remarks>
/// With <c>x:Class</c>, the loader creates the code-behind type, so its constructor runs.
/// The loader fails when the type has no parameterless constructor and no constructor with one IServiceProvider.
/// Without <c>x:Class</c>, the loader creates the root element type in design mode.
/// </remarks>
internal static partial class CodeBehindClass
{
    /// <summary>Removes the <c>x:Class</c> directive when the loader cannot create its type.</summary>
    /// <param name="xaml">The AXAML text.</param>
    /// <param name="localAssembly">The assembly that the project builds.</param>
    /// <returns>The text with no change, or the text without the <c>x:Class</c> directive.</returns>
    public static string RemoveIfNotCreatable(string xaml, Assembly localAssembly)
    {
        Match match = ClassDirective().Match(xaml);
        if (!match.Success)
        {
            return xaml;
        }

        Type? type = localAssembly.GetType(match.Groups["name"].Value);
        return type is null || IsCreatable(type) ? xaml : xaml.Remove(match.Index, match.Length);
    }

    private static bool IsCreatable(Type type) =>
        type.GetConstructor(Type.EmptyTypes) is not null || type.GetConstructor([typeof(IServiceProvider)]) is not null;

    [GeneratedRegex("""\s[\w.-]+:Class\s*=\s*(?<q>["'])(?<name>[\w.+]+)\k<q>""")]
    private static partial Regex ClassDirective();
}
