using System.Xml;

namespace AxView.Host.UI;

/// <summary>Finds the <see cref="FileKind"/> of an AXAML file from the name of its root element.</summary>
internal static class FileKindReader
{
    private static readonly XmlReaderSettings readerSettings = new() { DtdProcessing = DtdProcessing.Prohibit, IgnoreComments = true };

    /// <summary>Reads the root element of the file. The reader stops after the root element.</summary>
    /// <param name="path">The full path of the AXAML file.</param>
    /// <returns>The kind. <see cref="FileKind.View"/> when the file is not valid XML or cannot be read.</returns>
    public static FileKind Read(string path)
    {
        try
        {
            using XmlReader reader = XmlReader.Create(path, readerSettings);
            _ = reader.MoveToContent();
            return reader.LocalName switch
            {
                "Styles" or "Style" => FileKind.Styles,
                "ResourceDictionary" => FileKind.Resources,
                _ => FileKind.View,
            };
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            // The load of the file shows the error. The list only needs an icon.
            return FileKind.View;
        }
    }
}
