using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace AxView.Host.Startup;

/// <summary>The styles, resources, data templates, and theme variant that the target app loads in its App.axaml.</summary>
/// <param name="Styles">
/// The styles of the app, in the order of <c>Application.Styles</c>. An owner adds this object as one child style.
/// Thus an include reload can replace a style in it.
/// </param>
/// <param name="Resources">The resources of the app.</param>
/// <param name="DataTemplates">The data templates of the app.</param>
/// <param name="ThemeVariant">The theme variant that the app requests. Null when the app requests none.</param>
internal sealed record TargetStyles(
    Styles Styles,
    IResourceDictionary Resources,
    IReadOnlyList<IDataTemplate> DataTemplates,
    ThemeVariant? ThemeVariant)
{
    /// <summary>Gets an empty set, for a target app that has no Application class.</summary>
    public static TargetStyles None => new([], new ResourceDictionary(), [], null);

    /// <summary>Moves the styles, resources, and data templates into the app.</summary>
    /// <param name="app">The app that gets them.</param>
    public void ApplyTo(Application app)
    {
        app.Styles.Add(Styles);
        app.Resources = Resources;
        app.DataTemplates.AddRange(DataTemplates);
    }
}
