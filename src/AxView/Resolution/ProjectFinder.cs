using System.Xml.Linq;

namespace AxView.Resolution;

/// <summary>Finds the project that owns an AXAML file, and the names the runtime uses for it.</summary>
internal static class ProjectFinder
{
    public static string FindProject(string axamlPath)
    {
        DirectoryInfo? dir = new FileInfo(axamlPath).Directory;
        while (dir is not null)
        {
            string[] projects = Directory.GetFiles(dir.FullName, "*.csproj");
            if (projects.Length == 1)
            {
                return projects[0];
            }

            if (projects.Length > 1)
            {
                throw new PreviewSetupException($"More than one project in '{dir.FullName}'. Cannot choose the owner of '{axamlPath}'.");
            }

            dir = dir.Parent;
        }

        throw new PreviewSetupException($"No .csproj found above '{axamlPath}'.");
    }

    public static string ReadAssemblyName(string projectPath)
    {
        XDocument doc = XDocument.Load(projectPath);
        string? name = doc.Descendants()
            .Where(e => e.Name.LocalName == "AssemblyName")
            .Select(e => e.Value.Trim())
            .LastOrDefault(v => v.Length > 0 && !v.Contains("$(", StringComparison.Ordinal));
        return name ?? Path.GetFileNameWithoutExtension(projectPath);
    }

    /// <summary>Finds the project that builds the app of the output. The project is in a folder above the output.</summary>
    /// <exception cref="PreviewSetupException">No project above the output has the assembly name of the app.</exception>
    public static string FindAppProject(AppOutput output)
    {
        for (DirectoryInfo? dir = new(output.Directory); dir is not null; dir = dir.Parent)
        {
            string? project = Directory.EnumerateFiles(dir.FullName, "*.csproj")
                .FirstOrDefault(p => ReadAssemblyName(p) == output.AppName);
            if (project is not null)
            {
                return project;
            }
        }

        throw new PreviewSetupException(
            $"No project above '{output.Directory}' builds '{output.AppName}'. Cannot build the app.");
    }
}
