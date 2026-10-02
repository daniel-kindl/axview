using System.Xml.Linq;

namespace AxView.Protocol;

/// <summary>Reads the name of the assembly that a project builds.</summary>
internal static class ProjectAssembly
{
    /// <summary>Gives the <c>AssemblyName</c> of the project file, or the file name without the extension.</summary>
    /// <param name="projectPath">A .csproj file.</param>
    /// <remarks>A name with an MSBuild property, such as <c>$(Name)</c>, is not a name that this method can read.</remarks>
    public static string ReadName(string projectPath)
    {
        XDocument doc = XDocument.Load(projectPath);
        string? name = doc.Descendants()
            .Where(e => e.Name.LocalName == "AssemblyName")
            .Select(e => e.Value.Trim())
            .LastOrDefault(v => v.Length > 0 && !v.Contains("$(", StringComparison.Ordinal));
        return name ?? Path.GetFileNameWithoutExtension(projectPath);
    }
}
