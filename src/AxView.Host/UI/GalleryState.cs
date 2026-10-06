namespace AxView.Host.UI;

/// <summary>A state of a control in the <see cref="ControlGallery"/>. The gallery forces the state without real input.</summary>
internal enum GalleryState
{
    /// <summary>The control as it is. It takes real input.</summary>
    Normal,

    /// <summary>The <c>:pointerover</c> pseudo-class.</summary>
    PointerOver,

    /// <summary>The <c>:pointerover</c> and <c>:pressed</c> pseudo-classes.</summary>
    Pressed,

    /// <summary>The <c>:focus</c> and <c>:focus-visible</c> pseudo-classes.</summary>
    Focused,

    /// <summary>The control is disabled.</summary>
    Disabled,

    /// <summary>The control has a data validation error.</summary>
    Error,
}
