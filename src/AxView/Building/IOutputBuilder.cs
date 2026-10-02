using AxView.Resolution;

namespace AxView.Building;

/// <summary>Builds the project that makes an app output.</summary>
internal interface IOutputBuilder
{
    /// <summary>Builds the app project of the output and waits for the build to stop.</summary>
    /// <exception cref="PreviewSetupException">The build failed. The message contains the build errors.</exception>
    Task BuildAsync(AppOutput output);
}
