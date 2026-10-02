using AxView.Protocol;

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

    /// <summary>Finds the project that builds the app of the output. The project is in a folder above the output.</summary>
    /// <exception cref="PreviewSetupException">No project above the output has the assembly name of the app.</exception>
    public static string FindAppProject(AppOutput output)
    {
        for (DirectoryInfo? dir = new(output.Directory); dir is not null; dir = dir.Parent)
        {
            string? project = Directory.EnumerateFiles(dir.FullName, "*.csproj")
                .FirstOrDefault(p => ProjectAssembly.ReadName(p) == output.AppName);
            if (project is not null)
            {
                return project;
            }
        }

        throw new PreviewSetupException(
            $"No project above '{output.Directory}' builds '{output.AppName}'. Cannot build the app.");
    }
}
