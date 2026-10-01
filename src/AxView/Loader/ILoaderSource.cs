using AxView.Resolution;

namespace AxView.Loader;

/// <summary>Gives the XAML loader assembly for an Avalonia version.</summary>
/// <remarks>The loader uses Avalonia internals, so its version must agree with the target app's Avalonia exactly.</remarks>
internal interface ILoaderSource
{
    /// <summary>Returns the path of the loader assembly for the version.</summary>
    /// <exception cref="PreviewSetupException">No loader of the version is available.</exception>
    string Find(string avaloniaVersion);
}
