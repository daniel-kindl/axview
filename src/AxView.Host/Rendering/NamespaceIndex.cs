using System.Reflection;

namespace AxView.Host.Rendering;

/// <summary>Maps each CLR namespace to the assembly that defines it.</summary>
/// <remarks>The index covers the local assembly and the assemblies it references.</remarks>
internal sealed class NamespaceIndex
{
    private readonly Dictionary<string, HashSet<string>> owners = new(StringComparer.Ordinal);

    public NamespaceIndex(Assembly localAssembly)
    {
        Add(localAssembly, SafeTypes(localAssembly.GetTypes));
        foreach (AssemblyName reference in localAssembly.GetReferencedAssemblies())
        {
            Assembly? assembly = TryLoad(reference);
            if (assembly is not null)
            {
                Add(assembly, SafeTypes(assembly.GetExportedTypes));
            }
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

    private static Assembly? TryLoad(AssemblyName name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private static IEnumerable<Type> SafeTypes(Func<Type[]> getTypes)
    {
        try
        {
            return getTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
