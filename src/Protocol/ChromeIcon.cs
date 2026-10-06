namespace AxView.Protocol;

/// <summary>An icon of the AxView chrome. <see cref="ChromeIcons"/> gives its geometry.</summary>
internal enum ChromeIcon
{
    /// <summary>A folder, for the Open button.</summary>
    Open,

    /// <summary>A circle with the letter "i", for the About button.</summary>
    About,

    /// <summary>A half filled circle, for the theme switch.</summary>
    Theme,

    /// <summary>Four corners, for the size switch.</summary>
    Size,

    /// <summary>Two sheets, for a copy button.</summary>
    Copy,

    /// <summary>A chevron that points down, for a collapse button.</summary>
    Collapse,

    /// <summary>A search glass, for a filter box.</summary>
    Search,

    /// <summary>A window, for a view file.</summary>
    View,

    /// <summary>A palette, for a styles file.</summary>
    Styles,

    /// <summary>A stack, for a resource dictionary file.</summary>
    Resources,

    /// <summary>Four tiles, for the control gallery.</summary>
    Gallery,

    /// <summary>A triangle with an exclamation mark, for a warning.</summary>
    Warning,

    /// <summary>A circle with a cross, for an error.</summary>
    Error,
}
