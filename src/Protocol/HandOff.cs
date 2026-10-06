using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxView.Protocol;

/// <summary>The request of a stopped host to the launcher: the exit reason and the file that the next host shows.</summary>
/// <remarks>
/// The host calls <see cref="Save"/> and exits with the exit code that it returns.
/// The launcher calls <see cref="Take"/> with the exit code of the host.
/// The file is JSON: <c>{ "NextFile": "/src/Other/Views/Main.axaml" }</c>. The exit code gives the reason.
/// </remarks>
internal sealed record HandOff
{
    private const string NextFileKey = "NextFile";

    /// <summary>Makes a hand-off.</summary>
    /// <param name="reason">The exit reason. <see cref="ExitReason.Rebuild"/> or <see cref="ExitReason.SwitchProject"/>.</param>
    /// <param name="nextFile">The full path of the AXAML file that the next host shows.</param>
    /// <exception cref="ArgumentException">The reason does not start a next host.</exception>
    public HandOff(ExitReason reason, string nextFile)
    {
        if (!IsHandOffReason((int)reason))
        {
            throw new ArgumentException($"The exit reason {reason} does not start a next host.", nameof(reason));
        }

        Reason = reason;
        NextFile = nextFile;
    }

    /// <summary>Gets the exit reason of the host.</summary>
    public ExitReason Reason { get; }

    /// <summary>Gets the full path of the AXAML file that the next host shows.</summary>
    public string NextFile { get; }

    /// <summary>Reads and deletes the hand-off file of a stopped host.</summary>
    /// <param name="path">The hand-off file of the host options.</param>
    /// <param name="exitCode">The exit code of the host.</param>
    /// <returns>The hand-off, or null when the exit code starts no next host, or when the host wrote no file.</returns>
    /// <exception cref="JsonException">The file is not a valid hand-off.</exception>
    public static HandOff? Take(string path, int exitCode)
    {
        if (!IsHandOffReason(exitCode) || !File.Exists(path))
        {
            return null;
        }

        string json = File.ReadAllText(path);
        File.Delete(path);
        string nextFile = JsonNode.Parse(json)?[NextFileKey]?.GetValue<string>()
            ?? throw new JsonException($"'{path}' contains no hand-off.");
        return new HandOff((ExitReason)exitCode, nextFile);
    }

    /// <summary>Writes the hand-off file.</summary>
    /// <param name="path">The hand-off file of the host options.</param>
    /// <returns>The exit code that the host must give.</returns>
    public int Save(string path)
    {
        JsonObject json = new() { [NextFileKey] = NextFile };
        File.WriteAllText(path, json.ToJsonString());
        return (int)Reason;
    }

    private static bool IsHandOffReason(int exitCode) =>
        exitCode is (int)ExitReason.SwitchProject or (int)ExitReason.Rebuild;
}
