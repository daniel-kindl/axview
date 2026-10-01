using AxView.Protocol;
using AxView.Resolution;

namespace AxView.Hosting;

/// <summary>Runs one host until it exits.</summary>
internal interface IHostProcess
{
    /// <summary>Starts the host for the target and waits for it to exit.</summary>
    /// <returns>The exit code of the host. See <see cref="ExitReason"/>.</returns>
    /// <exception cref="PreviewSetupException">The host cannot start.</exception>
    Task<int> RunAsync(PreviewTarget target, HostOptions options);
}
