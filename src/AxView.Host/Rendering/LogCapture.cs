using System.Globalization;
using System.Text.RegularExpressions;

using Avalonia;
using Avalonia.Logging;

namespace AxView.Host.Rendering;

/// <summary>Catches the Avalonia log events of level Warning and higher, for example binding errors.</summary>
/// <remarks>
/// The capture replaces the Avalonia log sink and gives each event also to the previous sink.
/// <see cref="Dispose"/> puts back the previous sink.
/// </remarks>
internal sealed partial class LogCapture : ILogSink, IDisposable
{
    private const LogEventLevel MinimumLevel = LogEventLevel.Warning;

    private readonly ILogSink? previous;
    private readonly List<LogEntry> entries = [];
    private readonly object entriesLock = new();

    private LogCapture(ILogSink? previous)
    {
        this.previous = previous;
    }

    /// <summary>Gets a copy of the events that the capture caught, oldest first.</summary>
    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (entriesLock)
            {
                return [.. entries];
            }
        }
    }

    /// <summary>Starts a capture. The capture becomes the Avalonia log sink.</summary>
    public static LogCapture Start()
    {
        LogCapture capture = new(Logger.Sink);
        Logger.Sink = capture;
        return capture;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogEventLevel level, string area) => level >= MinimumLevel || (previous?.IsEnabled(level, area) ?? false);

    /// <inheritdoc />
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Log(level, area, source, messageTemplate, []);

    /// <inheritdoc />
    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        if (previous?.IsEnabled(level, area) ?? false)
        {
            previous.Log(level, area, source, messageTemplate, propertyValues);
        }

        if (level < MinimumLevel)
        {
            return;
        }

        LogEntry entry = new(level.ToString(), area, Describe(source), Format(messageTemplate, propertyValues));
        lock (entriesLock)
        {
            entries.Add(entry);
        }
    }

    /// <summary>Puts back the previous log sink.</summary>
    public void Dispose()
    {
        if (ReferenceEquals(Logger.Sink, this))
        {
            Logger.Sink = previous;
        }
    }

    private static string? Describe(object? source) =>
        source switch
        {
            null => null,
            StyledElement { Name: string name } element when name.Length > 0 => $"{element.GetType().Name}#{name}",
            _ => source.GetType().Name,
        };

    // Puts the values into the template in order. A template has holes like {Property}.
    private static string Format(string template, object?[] values)
    {
        int valueIndex = 0;
        return TemplateHole().Replace(
            template,
            hole => valueIndex < values.Length ? Convert.ToString(values[valueIndex++], CultureInfo.InvariantCulture) ?? string.Empty : hole.Value);
    }

    [GeneratedRegex(@"\{[^{}]*\}")]
    private static partial Regex TemplateHole();
}
