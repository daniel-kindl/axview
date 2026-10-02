using System.Text.Json;

using AxView.Protocol;

namespace AxView.Picker;

/// <summary>Finds the settings file. AxView is portable, so the file is next to the app when the app folder is writable.</summary>
internal static class SettingsLocation
{
    /// <summary>The name of the settings file.</summary>
    public const string FileName = "settings.json";

    private const string LegacyRecentFileName = "recent.json";

    /// <summary>
    /// Finds the settings file of the running app.
    /// Copies the recent files of an old AxView version into a new settings file.
    /// </summary>
    public static string ForCurrentUser()
    {
        string userDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxView");
        string settingsFile = Find(AppContext.BaseDirectory, userDirectory);
        ImportRecentFiles(Path.Combine(userDirectory, LegacyRecentFileName), settingsFile);
        return settingsFile;
    }

    /// <summary>Gives the settings file in the app folder, or in the user folder when the app folder is not writable.</summary>
    /// <param name="appDirectory">The folder of the app. For example, a folder below Program Files is not writable.</param>
    /// <param name="userDirectory">The AxView folder in the local application data of the user.</param>
    public static string Find(string appDirectory, string userDirectory) =>
        Path.Combine(IsWritable(appDirectory) ? appDirectory : userDirectory, FileName);

    /// <summary>Copies an old recent file list into the settings file, if the settings file does not exist yet.</summary>
    /// <remarks>Old AxView versions kept the list in a JSON array of paths in <c>recent.json</c>.</remarks>
    public static void ImportRecentFiles(string legacyFile, string settingsFile)
    {
        if (File.Exists(settingsFile) || !File.Exists(legacyFile))
        {
            return;
        }

        try
        {
            List<string> files = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(legacyFile)) ?? [];
            (Settings.Default with { RecentFiles = files }).Save(settingsFile);
        }
        catch (JsonException)
        {
            // A corrupt old list only loses the recent files. It must not stop the app.
        }
    }

    private static bool IsWritable(string directory)
    {
        try
        {
            using FileStream probe = File.Create(Path.Combine(directory, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
