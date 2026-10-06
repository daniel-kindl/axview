using System.Text.Json;
using System.Text.Json.Serialization;

using AxView.Protocol;

namespace AxView.Host.Rendering;

/// <summary>The result of one snapshot, for agents and scripts.</summary>
/// <remarks>
/// <see cref="Save"/> writes JSON. A value that is null is not in the file:
/// <code>
/// {
///   "File": "/src/Ui/Views/Main.axaml",
///   "ExitReason": "RenderFailed",
///   "LoadMilliseconds": 85.2,
///   "Error": "XmlException: Unexpected end of file. Line 2, position 3.",
///   "Line": 2,
///   "Column": 3,
///   "Log": [
///     { "Level": "Warning", "Area": "Binding", "Source": "TextBlock#lblTitle", "Message": "An error occurred binding ..." }
///   ]
/// }
/// </code>
/// </remarks>
/// <param name="File">The AXAML file of the snapshot.</param>
/// <param name="ExitReason">The exit reason of the host.</param>
/// <param name="LoadMilliseconds">The time of the XAML load, in milliseconds. Zero when the host did not load the file.</param>
/// <param name="Error">The error of the snapshot, or null.</param>
/// <param name="Line">The line of the error in the AXAML file, or null when it is not known.</param>
/// <param name="Column">The column of the error in the AXAML file, or null when it is not known.</param>
/// <param name="Log">The Avalonia log events of level Warning and higher, for example binding errors.</param>
internal sealed record SnapshotReport(
    string File,
    ExitReason ExitReason,
    double LoadMilliseconds,
    string? Error,
    int? Line,
    int? Column,
    IReadOnlyList<LogEntry> Log)
{
    private const int TimeDecimals = 1;

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Makes a report.</summary>
    /// <param name="file">The AXAML file of the snapshot.</param>
    /// <param name="exitReason">The exit reason of the host.</param>
    /// <param name="loadTime">The time of the XAML load.</param>
    /// <param name="error">The error of the snapshot, or null.</param>
    /// <param name="location">The place of the error in the AXAML file, or null.</param>
    /// <param name="log">The log events of the snapshot.</param>
    public static SnapshotReport Create(
        string file, ExitReason exitReason, TimeSpan loadTime, string? error, XamlLocation? location, IReadOnlyList<LogEntry> log) =>
        new(file, exitReason, Math.Round(loadTime.TotalMilliseconds, TimeDecimals), error, location?.Line, location?.Column, log);

    /// <summary>Writes the report to a JSON file. Makes the folder if necessary.</summary>
    public void Save(string path)
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(this, jsonOptions));
    }
}
