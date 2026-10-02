using System.Reflection;

namespace AxView.Host.Rendering;

/// <summary>Maps each CLR namespace to the assembly that defines it.</summary>
/// <remarks>The index covers the <see cref="ProjectTypes"/> of the local assembly.</remarks>
internal sealed class NamespaceIndex
{
    private readonly Dictionary<string, HashSet<string>> owners = new(StringComparer.Ordinal);

    public NamespaceIndex(Assembly localAssembly)
    {
        foreach ((Assembly assembly, IReadOnlyList<Type> types) in ProjectTypes.List(localAssembly))
        {
            Add(assembly, types);
        }
    }

    /// <summary>Returns the only assembly that defines the namespace. Returns null if none or many do.</summary>
    public string? FindOwner(string clrNamespace) =>
        owners.TryGetValue(clrNamespace, out HashSet<string>? names) && names.Count == 1 ? names.First() : null;

    private void Add(Assembly assembly, IEnumerable<Type> types)
    {
        string name = assembly.GetName().Name!;
        foreach (string clrNamespace in types.Select(t => t.Namespace).OfType<string>().Distinct())
        {
            if (!owners.TryGetValue(clrNamespace, out HashSet<string>? names))
            {
                names = new HashSet<string>(StringComparer.Ordinal);
                owners[clrNamespace] = names;
            }

            _ = names.Add(name);
        }
    }
}
