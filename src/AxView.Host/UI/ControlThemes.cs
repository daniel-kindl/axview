using Avalonia.Controls;
using Avalonia.Styling;

namespace AxView.Host.UI;

/// <summary>Finds the control themes with a key in the app resources, for example <c>PillButton</c>.</summary>
/// <remarks>
/// The search reads the app resources and the resources of each app style.
/// It ignores an Avalonia theme, for example <c>FluentTheme</c>. Its keyed themes are parts of its default themes.
/// A theme with a type key is the default theme of the type. The search ignores it.
/// The XAML compiler puts the content of each <c>ResourceInclude</c> in place of the include.
/// Thus each merged dictionary is a <see cref="IResourceDictionary"/>.
/// </remarks>
internal static class ControlThemes
{
    private const string AvaloniaAssemblyPrefix = "Avalonia.";

    /// <summary>Finds the control themes. Each key occurs once, in the order of the resources.</summary>
    /// <param name="styles">The styles of the app.</param>
    /// <param name="resources">The resources of the app.</param>
    public static IReadOnlyList<KeyedControlTheme> Find(IEnumerable<IStyle> styles, IResourceDictionary resources)
    {
        List<KeyedControlTheme> found = [];
        Add(resources, found);
        foreach (IStyle style in styles)
        {
            Add(style, found);
        }

        return found;
    }

    // A Styles and a Style each have resources. A ControlTheme is a style too, but its resources apply only in the theme.
    private static void Add(IStyle style, List<KeyedControlTheme> found)
    {
        if (IsAvaloniaTheme(style))
        {
            return;
        }

        if (style is Styles { Resources: IResourceDictionary stylesResources })
        {
            Add(stylesResources, found);
        }
        else if (style is Style { Resources: IResourceDictionary styleResources })
        {
            Add(styleResources, found);
        }

        foreach (IStyle child in style.Children)
        {
            Add(child, found);
        }
    }

    private static void Add(IResourceDictionary resources, List<KeyedControlTheme> found)
    {
        // The value of a deferred resource is created on a read. Thus read each value through the dictionary.
        foreach (object key in resources.Keys.Where(key => key is not Type))
        {
            string name = key.ToString() ?? string.Empty;
            if (resources.TryGetValue(key, out object? value) && value is ControlTheme theme && !found.Exists(t => t.Key == name))
            {
                found.Add(new KeyedControlTheme(name, theme));
            }
        }

        foreach (IResourceDictionary merged in resources.MergedDictionaries.OfType<IResourceDictionary>())
        {
            Add(merged, found);
        }
    }

    // An Avalonia theme is a subclass of Styles in an Avalonia assembly, for example Avalonia.Themes.Fluent.
    private static bool IsAvaloniaTheme(IStyle style)
    {
        Type type = style.GetType();
        string assemblyName = type.Assembly.GetName().Name ?? string.Empty;
        return type != typeof(Styles) && style is Styles && assemblyName.StartsWith(AvaloniaAssemblyPrefix, StringComparison.Ordinal);
    }
}
