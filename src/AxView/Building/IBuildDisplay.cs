namespace AxView.Building;

/// <summary>Shows the progress of each build of the app.</summary>
internal interface IBuildDisplay
{
    /// <summary>Shows that a build of the app starts.</summary>
    /// <param name="appName">The assembly name of the app.</param>
    /// <returns>The progress of the build. Dispose it when the build stops.</returns>
    IBuildProgress Show(string appName);
}
