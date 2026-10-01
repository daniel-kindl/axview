using System.Runtime.Loader;

namespace AxView.Host.Startup;

/// <summary>Loads the runtime XAML loader from the path the launcher found.</summary>
/// <remarks>The target's deps file does not usually list the loader, so the default probe fails.</remarks>
internal static class LoaderResolver
{
    public static void Register(string loaderPath)
    {
        string name = Path.GetFileNameWithoutExtension(loaderPath);
        AssemblyLoadContext.Default.Resolving += (context, requested) =>
            string.Equals(requested.Name, name, StringComparison.OrdinalIgnoreCase)
                ? context.LoadFromAssemblyPath(loaderPath)
                : null;
    }
}
