using AxView.Hosting;
using AxView.Protocol;
using AxView.Resolution;

namespace AxView.Tests.Fixtures;

/// <summary>A host process that exits with scripted exit codes. It can write a hand-off before it exits.</summary>
internal sealed class FakeHostProcess : IHostProcess
{
    private readonly Queue<(ExitReason Reason, string? NextFile)> script = new();
    private readonly List<HostOptions> runs = [];

    /// <summary>The options of each run, in order.</summary>
    public IReadOnlyList<HostOptions> Runs => runs;

    /// <summary>Adds the result of the next run.</summary>
    /// <param name="reason">The exit reason of the run.</param>
    /// <param name="nextFile">If set, the run writes a hand-off with this file.</param>
    public void Enqueue(ExitReason reason, string? nextFile = null) => script.Enqueue((reason, nextFile));

    /// <inheritdoc />
    public Task<int> RunAsync(PreviewTarget target, HostOptions options)
    {
        runs.Add(options);
        (ExitReason reason, string? nextFile) = script.Dequeue();
        if (nextFile is not null)
        {
            _ = new HandOff(reason, nextFile).Save(options.HandOffFile!);
        }

        return Task.FromResult((int)reason);
    }
}
