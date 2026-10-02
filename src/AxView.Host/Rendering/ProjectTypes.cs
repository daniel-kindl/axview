using System.Reflection;

namespace AxView.Host.Rendering;

/// <summary>Gives the types that the XAML of a project can use.</summary>
/// <remarks>
/// These are all types of the local assembly and the exported types of the assemblies that it references.
/// A reference that does not load is skipped. A type that does not load is skipped.
/// </remarks>
internal static class ProjectTypes
{
    /// <summary>Loads the local assembly and its references, and gives the types of each.</summary>
    /// <param name="localAssembly">The assembly that the project builds.</param>
    /// <returns>One item for each assembly. The local assembly is first.</returns>
    public static IEnumerable<(Assembly Assembly, IReadOnlyList<Type> Types)> List(Assembly localAssembly)
    {
        yield return (localAssembly, SafeTypes(localAssembly.GetTypes));
        foreach (AssemblyName reference in localAssembly.GetReferencedAssemblies())
        {
            Assembly? assembly = TryLoad(reference);
            if (assembly is not null)
            {
                yield return (assembly, SafeTypes(assembly.GetExportedTypes));
            }
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

    private static Type[] SafeTypes(Func<Type[]> getTypes)
    {
        try
        {
            return getTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return [.. ex.Types.OfType<Type>()];
        }
    }
}
