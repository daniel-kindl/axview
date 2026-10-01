using System.Text.Json;

namespace AxView;

/// <summary>A build output of an executable Avalonia app. The host runs inside its runtime.</summary>
internal sealed record AppOutput(string Directory, string DepsFile, string RuntimeConfig)
{
    private const string RuntimeConfigSuffix = ".runtimeconfig.json";
    private const string AvaloniaLibraryPrefix = "Avalonia/";

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
        using FileStream stream = File.OpenRead(DepsFile);
        using JsonDocument doc = JsonDocument.Parse(stream);
        if (doc.RootElement.TryGetProperty("libraries", out JsonElement libraries))
        {
            foreach (JsonProperty library in libraries.EnumerateObject())
            {
                if (library.Name.StartsWith(AvaloniaLibraryPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return library.Name[AvaloniaLibraryPrefix.Length..];
                }
            }
        }

        throw new PreviewSetupException($"'{DepsFile}' does not reference the Avalonia package.");
    }

    private static string AppNameOf(string runtimeConfig) => Path.GetFileName(runtimeConfig)[..^RuntimeConfigSuffix.Length];
}
