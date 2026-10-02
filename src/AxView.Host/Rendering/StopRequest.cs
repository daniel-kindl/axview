using AxView.Protocol;

namespace AxView.Host.Rendering;

/// <summary>A request to stop the host, so that the launcher can start a new host.</summary>
/// <param name="Reason">The exit reason. <see cref="ExitReason.Rebuild"/> or <see cref="ExitReason.SwitchProject"/>.</param>
/// <param name="NextFile">The full path of the file that the next host shows.</param>
internal sealed record StopRequest(ExitReason Reason, string NextFile);
