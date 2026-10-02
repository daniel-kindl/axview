using System.Text.Json;
using System.Text.Json.Serialization;

namespace AxView.Protocol;

/// <summary>The AxView settings. The launcher and the host both read and write one JSON file.</summary>
/// <remarks>
/// The file is JSON. Each value is optional. Enum values are names:
/// <code>
/// {
///   "RecentFiles": [ "/src/Ui/Views/Main.axaml" ],
///   "Window": { "Mode": "Maximized", "X": 10, "Y": 20, "Width": 1280, "Height": 800 },
///   "Theme": "Dark"
/// }
/// </code>
/// Each change loads the file again, changes one value, and saves the file.
/// Thus the launcher and the host keep the values of the other.
/// </remarks>
internal sealed record Settings
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>The settings when the file is missing or not valid.</summary>
    public static Settings Default { get; } = new();

    /// <summary>The AXAML files that the user opened last, newest first.</summary>
    public IReadOnlyList<string> RecentFiles { get; init; } = [];

    /// <summary>The last state and bounds of the preview window, or null to let the host select them.</summary>
    public WindowPlacement? Window { get; init; }

    /// <summary>The theme variant that the user selected last.</summary>
    public PreviewTheme Theme { get; init; }

    /// <summary>Reads the settings file.</summary>
    /// <returns>The settings, or <see cref="Default"/> when the file is missing or not valid.</returns>
    public static Settings Load(string path)
    {
        if (!File.Exists(path))
        {
            return Default;
        }

        try
        {
            Settings? settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), jsonOptions);

            // JSON can set a value to null. The settings must not stop the app.
            return settings is null ? Default : settings with { RecentFiles = settings.RecentFiles ?? [] };
        }
        catch (JsonException)
        {
            // A corrupt file only loses the settings. It must not stop the app.
            return Default;
        }
    }

    /// <summary>Writes the settings file. Makes the folder if necessary.</summary>
    public void Save(string path)
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, jsonOptions));
    }
}
