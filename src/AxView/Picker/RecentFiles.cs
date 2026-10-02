using AxView.Protocol;

namespace AxView.Picker;

/// <summary>The AXAML files the user opened last, newest first. The <see cref="Settings"/> file stores them.</summary>
internal sealed class RecentFiles(string settingsFile)
{
    public const int Capacity = 10;

    private readonly List<string> files = [.. Settings.Load(settingsFile).RecentFiles];

    /// <summary>The settings file that stores the list.</summary>
    public string SettingsFile { get; } = settingsFile;

    public IReadOnlyList<string> Files => files;

    /// <summary>The list in the settings file of the running app.</summary>
    public static RecentFiles ForCurrentUser() => new(SettingsLocation.ForCurrentUser());

    public void Add(string path)
    {
        string full = Path.GetFullPath(path);
        _ = files.RemoveAll(f => string.Equals(f, full, StringComparison.OrdinalIgnoreCase));
        files.Insert(0, full);
        if (files.Count > Capacity)
        {
            files.RemoveRange(Capacity, files.Count - Capacity);
        }

        (Settings.Load(SettingsFile) with { RecentFiles = [.. files] }).Save(SettingsFile);
    }
}
