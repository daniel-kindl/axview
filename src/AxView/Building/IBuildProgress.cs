namespace AxView.Building;

/// <summary>Shows the progress of one build. The user can cancel the build. Dispose it when the build stops.</summary>
internal interface IBuildProgress : IProgress<string>, IDisposable
{
    /// <summary>Gets the token that the user cancels to stop the build.</summary>
    CancellationToken Cancellation { get; }
}
