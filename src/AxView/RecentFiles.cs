using System.Text.Json;

namespace AxView;

/// <summary>The AXAML files the user opened last, newest first. A JSON file stores them.</summary>
internal sealed class RecentFiles(string storePath)
{
    public const int Capacity = 10;

    private readonly string storePath = storePath;
    private readonly List<string> files = Load(storePath);

    public IReadOnlyList<string> Files => files;

    /// <summary>The store in the user's application data folder.</summary>
    public static RecentFiles ForCurrentUser() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxView", "recent.json"));

    public void Add(string path)
    {
        string full = Path.GetFullPath(path);
        _ = files.RemoveAll(f => string.Equals(f, full, StringComparison.OrdinalIgnoreCase));
        files.Insert(0, full);
        if (files.Count > Capacity)
        {
            files.RemoveRange(Capacity, files.Count - Capacity);
        }

        Save();
    }

    private void Save()
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(storePath)!);
        File.WriteAllText(storePath, JsonSerializer.Serialize(files));
    }

    private static List<string> Load(string storePath)
    {
        if (!File.Exists(storePath))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(storePath)) ?? [];
        }
        catch (JsonException)
        {
            // A corrupt store only loses the list. It must not stop the app.
            return [];
        }
    }
}
