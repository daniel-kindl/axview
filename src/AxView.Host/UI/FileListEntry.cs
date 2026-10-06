namespace AxView.Host.UI;

/// <summary>One entry of the file list of the preview window: a file of the project, or the control gallery.</summary>
/// <param name="Title">The first line: the file name without extension, or the name of the gallery.</param>
/// <param name="Detail">The second line: the folder of the file with <c>/</c> as separator, or a description of the gallery.</param>
/// <param name="RelativePath">The path of the file in the project. Null for the control gallery.</param>
/// <param name="Kind">The kind of the entry.</param>
internal sealed record FileListEntry(string Title, string Detail, string? RelativePath, FileKind Kind)
{
    /// <summary>Gets a value that tells if the entry is the control gallery.</summary>
    public bool IsGallery => RelativePath is null;
}
