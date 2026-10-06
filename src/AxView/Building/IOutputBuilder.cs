using AxView.Resolution;

namespace AxView.Building;

/// <summary>Builds the project that makes an app output.</summary>
internal interface IOutputBuilder
{
    /// <summary>Builds the app project of the output and waits for the build to stop.</summary>
    /// <param name="output">The app output to build.</param>
    /// <param name="progress">Receives each line of the build output.</param>
    /// <param name="cancellation">Stops the build.</param>
    /// <exception cref="PreviewSetupException">The build failed or was canceled. The message tells the cause.</exception>
    Task BuildAsync(AppOutput output, IProgress<string> progress, CancellationToken cancellation);
}
