using System.Text.Json;

namespace AxView.Protocol;

/// <summary>The data that a stopped host gives to the launcher for the next host.</summary>
/// <remarks>The file is JSON: <c>{ "NextFile": "/src/Other/Views/Main.axaml" }</c>.</remarks>
/// <param name="NextFile">The full path of the AXAML file that the next host shows.</param>
internal sealed record HandOff(string NextFile)
{
    /// <summary>Reads the hand-off file.</summary>
    /// <returns>The hand-off, or null when the host wrote no file.</returns>
    /// <exception cref="JsonException">The file is not a valid hand-off.</exception>
    public static HandOff? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<HandOff>(json) ?? throw new JsonException($"'{path}' contains no hand-off.");
    }

    /// <summary>Writes the hand-off file.</summary>
    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this));
}
