namespace AxView.Host.UI;

/// <summary>The kind of an entry of the file list. The sidebar shows an icon for each kind.</summary>
internal enum FileKind
{
    /// <summary>A file with a control root, for example a view or a window.</summary>
    View,

    /// <summary>A file with a <c>Styles</c> or a <c>Style</c> root.</summary>
    Styles,

    /// <summary>A file with a <c>ResourceDictionary</c> root.</summary>
    Resources,

    /// <summary>The control gallery.</summary>
    Gallery,
}
