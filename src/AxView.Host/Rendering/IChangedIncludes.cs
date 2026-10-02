using Avalonia.Markup.Xaml;

namespace AxView.Host.Rendering;

/// <summary>Finds the include files of an AXAML file that changed after the build.</summary>
internal interface IChangedIncludes
{
    /// <summary>Gives a loader document for each include file of the XAML that changed after the build.</summary>
    /// <remarks>The loader puts a document in place of the compiled include with the same avares URI.</remarks>
    /// <param name="path">The AXAML file.</param>
    /// <param name="xaml">The text of the file.</param>
    IReadOnlyList<RuntimeXamlLoaderDocument> FindChanged(string path, string xaml);
}
