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
    // The data template collections of the owners. A replace changes the data templates in each one.
    private readonly List<DataTemplates> templateOwners = [];

    /// <summary>Gets an empty set, for a target app that has no Application class.</summary>
    public static TargetStyles None => new([], new ResourceDictionary(), [], null);

    /// <summary>Gets the data templates of the app.</summary>
    public IReadOnlyList<IDataTemplate> DataTemplates { get; private set; } = DataTemplates;

    /// <summary>Gets the theme variant that the app requests. Null when the app requests none.</summary>
    public ThemeVariant? ThemeVariant { get; private set; } = ThemeVariant;

    /// <summary>Occurs when <see cref="ReplaceWith"/> changes <see cref="ThemeVariant"/>.</summary>
    public event EventHandler? ThemeVariantChanged;

    /// <summary>Moves the styles, resources, and data templates into the app.</summary>
    /// <param name="app">The app that gets them.</param>
    public void ApplyTo(Application app)
    {
        app.Styles.Add(Styles);
        app.Resources = Resources;
        AddDataTemplatesTo(app.DataTemplates);
    }

    /// <summary>Adds the data templates to the data templates of an owner.</summary>
    /// <remarks>A later <see cref="ReplaceWith"/> replaces them in the owner too.</remarks>
    /// <param name="owner">The data templates of the owner.</param>
    public void AddDataTemplatesTo(DataTemplates owner)
    {
        owner.AddRange(DataTemplates);
        templateOwners.Add(owner);
    }

    /// <summary>
    /// Moves the styles, resources, and data templates of another set into this set, and takes its theme variant.
    /// The owners of this set see the change.
    /// </summary>
    /// <param name="source">The set that gives its styles and resources. It is empty after the call.</param>
    public void ReplaceWith(TargetStyles source)
    {
        // A style or a dictionary can have only one owner. Thus remove each one from the source before this set gets it.
        List<IStyle> styles = [.. source.Styles];
        source.Styles.Clear();
        Styles.Clear();
        Styles.AddRange(styles);

        ReplaceResources(source.Resources);
        ReplaceDataTemplates(source.DataTemplates);
        if (ThemeVariant != source.ThemeVariant)
        {
            ThemeVariant = source.ThemeVariant;
            ThemeVariantChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // An owner can have other data templates too. Thus remove only the templates of this set.
    private void ReplaceDataTemplates(IReadOnlyList<IDataTemplate> templates)
    {
        foreach (DataTemplates owner in templateOwners)
        {
            foreach (IDataTemplate template in DataTemplates)
            {
                _ = owner.Remove(template);
            }

            owner.AddRange(templates);
        }

        DataTemplates = [.. templates];
    }

    private void ReplaceResources(IResourceDictionary source)
    {
        // The value of a deferred resource is created on a read. Thus read each value through the dictionary.
        List<(object Key, object? Value)> entries = [];
        foreach (object key in source.Keys.ToList())
        {
            _ = source.TryGetValue(key, out object? value);
            entries.Add((key, value));
        }

        List<IResourceProvider> merged = [.. source.MergedDictionaries];
        List<KeyValuePair<ThemeVariant, IThemeVariantProvider>> themes = [.. source.ThemeDictionaries];
        source.Clear();
        source.MergedDictionaries.Clear();
        source.ThemeDictionaries.Clear();

        Resources.Clear();
        Resources.MergedDictionaries.Clear();
        Resources.ThemeDictionaries.Clear();
        foreach ((object key, object? value) in entries)
        {
            Resources.Add(key, value);
        }

        foreach (IResourceProvider provider in merged)
        {
            Resources.MergedDictionaries.Add(provider);
        }

        foreach (KeyValuePair<ThemeVariant, IThemeVariantProvider> theme in themes)
        {
            Resources.ThemeDictionaries.Add(theme.Key, theme.Value);
        }
    }
}
