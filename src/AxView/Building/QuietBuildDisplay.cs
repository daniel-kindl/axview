namespace AxView.Building;

/// <summary>Shows no build. The snapshot modes use it, because they have no UI.</summary>
internal sealed class QuietBuildDisplay : IBuildDisplay, IBuildProgress
{
    /// <inheritdoc />
    public CancellationToken Cancellation => CancellationToken.None;

    /// <inheritdoc />
    public IBuildProgress Show(string appName) => this;

    /// <inheritdoc />
    public void Report(string value)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
