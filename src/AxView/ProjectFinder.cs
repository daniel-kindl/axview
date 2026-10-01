using System.Xml.Linq;

namespace AxView;

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
}
