using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;

namespace AxView.Host.UI;

/// <summary>Frames the preview like a window of the target app.</summary>
/// <remarks>
/// The frame takes the setters of the target <see cref="Window"/> control theme that apply to a <see cref="ContentControl"/>.
/// Thus the preview gets the background, the foreground, and the font of a target window.
/// The frame skips the window template and the window-only properties. It has its own template.
/// </remarks>
internal sealed class PreviewFrame : ContentControl
{
    /// <summary>Creates the frame.</summary>
    /// <param name="windowTheme">The <see cref="Window"/> control theme of the target app. Null when the app has none.</param>
    public PreviewFrame(ControlTheme? windowTheme)
    {
        // A local template overrides the template setter of the theme.
        Template = new FuncControlTemplate<PreviewFrame>((_, _) => new ContentPresenter
        {
            Name = "PART_ContentPresenter",
            [!ContentPresenter.BackgroundProperty] = new TemplateBinding(BackgroundProperty),
            [!ContentPresenter.ContentProperty] = new TemplateBinding(ContentProperty),
            [!ContentPresenter.ContentTemplateProperty] = new TemplateBinding(ContentTemplateProperty),
        });
        if (windowTheme is not null)
        {
            ControlTheme theme = new(typeof(PreviewFrame));
            foreach (SetterBase setter in FrameSetters(windowTheme))
            {
                theme.Setters.Add(setter);
            }

            Theme = theme;
        }
    }

    // A base theme comes first, so the setters of the derived theme win.
    private static IEnumerable<SetterBase> FrameSetters(ControlTheme theme)
    {
        IEnumerable<SetterBase> inherited = theme.BasedOn is null ? [] : FrameSetters(theme.BasedOn);
        IEnumerable<SetterBase> own = theme.Setters
            .Where(s => s is Setter { Property: { } property } && AvaloniaPropertyRegistry.Instance.IsRegistered(typeof(PreviewFrame), property));
        return inherited.Concat(own);
    }
}
