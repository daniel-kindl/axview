using System.Reflection;

using AxView.Protocol;

namespace AxView.Host.Rendering;

/// <summary>Makes the renderers of the referenced projects of the app.</summary>
internal static class ProjectRenderers
{
    /// <summary>Makes a renderer for each project folder that has a project file and an assembly that the host can load.</summary>
    /// <param name="projectDirectories">The folders of the projects.</param>
    /// <param name="services">The design services that create the ViewModels.</param>
    /// <returns>The renderers of the projects in the order of the folders. A project that the host cannot load has none.</returns>
    public static IEnumerable<PreviewRenderer> Create(IEnumerable<string> projectDirectories, DesignServices services)
    {
        foreach (string directory in projectDirectories)
        {
            string? project = Directory.EnumerateFiles(directory, "*.csproj").FirstOrDefault();
            if (project is null)
            {
                continue;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.Load(ProjectAssembly.ReadName(project));
            }
            catch (FileNotFoundException)
            {
                // A project that the app output does not contain has no include to load.
                continue;
            }

            yield return new PreviewRenderer(assembly, directory, services);
        }
    }
}
