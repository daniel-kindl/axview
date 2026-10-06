namespace AxView.Building;

/// <summary>Shows each build in a <see cref="BuildWindow"/>. Call it on the UI thread.</summary>
internal sealed class BuildWindowDisplay : IBuildDisplay
{
    /// <inheritdoc />
    public IBuildProgress Show(string appName)
    {
        BuildWindow window = new(appName);
        window.Show();
        return window;
    }
}
