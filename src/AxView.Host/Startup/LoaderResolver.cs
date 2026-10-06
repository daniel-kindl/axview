using System.Runtime.Loader;

namespace AxView.Host.Startup;

/// <summary>Loads the assemblies that the launcher found outside the app: the runtime XAML loader and the chrome theme.</summary>
/// <remarks>The target's deps file does not list these assemblies, so the default probe fails.</remarks>
internal static class LoaderResolver
{
    /// <summary>Loads the assembly from the path when the default probe cannot find an assembly of the same name.</summary>
    /// <param name="assemblyPath">The assembly file. Its file name is the assembly name.</param>
    public static void Register(string assemblyPath)
    {
        string name = Path.GetFileNameWithoutExtension(assemblyPath);
        AssemblyLoadContext.Default.Resolving += (context, requested) =>
            string.Equals(requested.Name, name, StringComparison.OrdinalIgnoreCase)
                ? context.LoadFromAssemblyPath(assemblyPath)
                : null;
    }
}
