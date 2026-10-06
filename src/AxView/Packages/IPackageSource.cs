using AxView.Resolution;

namespace AxView.Packages;

/// <summary>Gives the assembly of an Avalonia package for an Avalonia version.</summary>
/// <remarks>The packages use Avalonia internals, so their version must agree with the target app's Avalonia exactly.</remarks>
internal interface IPackageSource
{
    /// <summary>Returns the path of the assembly of the package for the version.</summary>
    /// <param name="packageId">The package, for example <c>Avalonia.Markup.Xaml.Loader</c>. Its assembly has the same name.</param>
    /// <param name="avaloniaVersion">The Avalonia version.</param>
    /// <exception cref="PreviewSetupException">No assembly of the package and version is available.</exception>
    string Find(string packageId, string avaloniaVersion);
}
