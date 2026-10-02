namespace AxView.Protocol;

/// <summary>The size of the preview that the user selects.</summary>
internal enum PreviewSizePreset
{
    /// <summary>The preview fills the free space of the window.</summary>
    Fit,

    /// <summary>A phone screen.</summary>
    Phone,

    /// <summary>A tablet screen.</summary>
    Tablet,

    /// <summary>A desktop screen.</summary>
    Desktop,

    /// <summary>A width and a height that the user types.</summary>
    Custom,
}
