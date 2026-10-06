using System.Text.Json;

namespace AxView.Resolution;

/// <summary>A build output of an executable Avalonia app. The host runs inside its runtime.</summary>
internal sealed record AppOutput(string Directory, string DepsFile, string RuntimeConfig)
{
    private const string RuntimeConfigSuffix = ".runtimeconfig.json";
    private const string AvaloniaLibraryPrefix = "Avalonia/";
    private static readonly string[] standardThemeLibraryPrefixes = ["Avalonia.Themes.Fluent/", "Avalonia.Themes.Simple/"];

    /// <summary>The app's assembly name. The runtimeconfig file name carries it.</summary>
    public string AppName => AppNameOf(RuntimeConfig);

    /// <summary>Makes the output of the app that the runtimeconfig file belongs to.</summary>
    /// <remarks>The deps file has the same app name. This method does not check that the files exist.</remarks>
    public static AppOutput FromRuntimeConfig(string runtimeConfig)
    {
        string directory = Path.GetDirectoryName(runtimeConfig)!;
        string appName = AppNameOf(runtimeConfig);
        return new AppOutput(directory, Path.Combine(directory, appName + ".deps.json"), runtimeConfig);
    }

    /// <summary>Reads the version of the Avalonia package from the deps file.</summary>
    /// <exception cref="PreviewSetupException">The deps file does not reference Avalonia.</exception>
    public string ReadAvaloniaVersion()
    {
        string? library = ReadLibraryNames()
            .FirstOrDefault(name => name.StartsWith(AvaloniaLibraryPrefix, StringComparison.OrdinalIgnoreCase));
        return library?[AvaloniaLibraryPrefix.Length..]
            ?? throw new PreviewSetupException($"'{DepsFile}' does not reference the Avalonia package.");
    }

    /// <summary>Tells if the deps file references the Fluent or the Simple theme package.</summary>
    public bool ReferencesStandardTheme() =>
        ReadLibraryNames().Any(name => standardThemeLibraryPrefixes.Any(
            prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

    // Each library of the deps file has a name of the form "<package>/<version>".
    private List<string> ReadLibraryNames()
    {
        using FileStream stream = File.OpenRead(DepsFile);
        using JsonDocument doc = JsonDocument.Parse(stream);
        return doc.RootElement.TryGetProperty("libraries", out JsonElement libraries)
            ? [.. libraries.EnumerateObject().Select(library => library.Name)]
            : [];
    }

    private static string AppNameOf(string runtimeConfig) => Path.GetFileName(runtimeConfig)[..^RuntimeConfigSuffix.Length];
}
