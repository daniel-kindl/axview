using System.Reflection;

using AxView.Host.Rendering;

namespace AxView.Host.Catalog;

/// <summary>Lists the ViewModel types that the user can select as the DataContext of a preview.</summary>
/// <remarks>
/// A ViewModel type is one of the <see cref="ProjectTypes"/> that agrees with all of these conditions:
/// <list type="bullet">
/// <item><description>It is a public, non-abstract, non-generic class.</description></item>
/// <item><description>Its name ends with <c>ViewModel</c>.</description></item>
/// <item><description>It has a public parameterless constructor.</description></item>
/// </list>
/// </remarks>
internal static class ViewModelCatalog
{
    private const string NameSuffix = "ViewModel";

    /// <summary>Returns the ViewModel types of the project, sorted by name.</summary>
    /// <param name="localAssembly">The assembly that the project builds.</param>
    public static IReadOnlyList<Type> List(Assembly localAssembly) =>
        [.. ProjectTypes.List(localAssembly)
            .SelectMany(item => item.Types)
            .Where(IsViewModel)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.FullName, StringComparer.Ordinal)];

    private static bool IsViewModel(Type type) =>
        type is { IsPublic: true, IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
        && type.Name.EndsWith(NameSuffix, StringComparison.Ordinal)
        && (type.GetConstructor(Type.EmptyTypes) is not null);
}
