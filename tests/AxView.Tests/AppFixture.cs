namespace AxView.Tests;

/// <summary>Writes the files of a built Avalonia app into a temporary folder.</summary>
internal static class AppFixture
{
    /// <summary>A deps file that references Avalonia 11.3.22.</summary>
    public const string AvaloniaDeps = """{ "libraries": { "Avalonia/11.3.22": { "type": "package" } } }""";

    /// <summary>Writes an executable app output that ships the assembly. Returns the full output folder.</summary>
    public static string WriteAppOutput(
        TempDir dir, string relativeDir, string appName, string assemblyName, string deps = AvaloniaDeps)
    {
        _ = dir.Write($"{relativeDir}/{assemblyName}.dll");
        _ = dir.Write($"{relativeDir}/Avalonia.Desktop.dll");
        _ = dir.Write($"{relativeDir}/{appName}.dll");
        _ = dir.Write($"{relativeDir}/{appName}.deps.json", deps);
        _ = dir.Write($"{relativeDir}/{appName}.runtimeconfig.json", "{}");
        return Path.Combine(dir.Path, relativeDir.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>Writes a project with one view and an app output under <c>src/{name}</c>. Returns the view path.</summary>
    public static string WriteProject(TempDir dir, string name)
    {
        _ = dir.Write($"src/{name}/{name}.csproj", "<Project />");
        _ = WriteAppOutput(dir, $"src/{name}/bin/Debug/net9.0", name, name);
        return dir.Write($"src/{name}/Views/Main.axaml");
    }
}
