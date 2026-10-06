using System.Text.RegularExpressions;

using Avalonia.Styling;

namespace AxView.Host.UI;

/// <summary>Finds the style classes in the selectors of the app styles, for example <c>Button.primary</c>.</summary>
/// <remarks>
/// The search reads the text of each selector. It uses only the control that the style sets:
/// the last part before <c>/template/</c>, without pseudo-classes, names, and property conditions.
/// A nested selector gets the text of its parent in place of <c>^</c>.
/// </remarks>
internal static partial class StyleClasses
{
    private const string TemplateSeparator = "/template/";
    private const string NestingMark = "^";

    /// <summary>Finds the style classes. Each class occurs once, in the order of the styles.</summary>
    /// <param name="styles">The styles of the app.</param>
    public static IReadOnlyList<StyleClass> Find(IEnumerable<IStyle> styles)
    {
        List<StyleClass> found = [];
        foreach (IStyle style in styles)
        {
            Add(style, string.Empty, found);
        }

        return found;
    }

    private static void Add(IStyle style, string parentSelector, List<StyleClass> found)
    {
        string selector = parentSelector;
        if (style is Style { Selector: not null } withSelector)
        {
            selector = withSelector.Selector.ToString().Replace(NestingMark, parentSelector, StringComparison.Ordinal);
            AddClasses(selector, found);
        }

        foreach (IStyle child in style.Children)
        {
            Add(child, selector, found);
        }
    }

    private static void AddClasses(string selector, List<StyleClass> found)
    {
        foreach (string alternative in selector.Split(','))
        {
            string target = alternative.Split(TemplateSeparator)[0];
            string[] parts = target.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            Match match = TypeAndClasses().Match(Conditions().Replace(parts[^1], string.Empty));
            StyleClass styleClass = new(match.Groups["type"].Value, match.Groups["classes"].Value.TrimStart('.'));
            if (match.Success && !found.Contains(styleClass))
            {
                found.Add(styleClass);
            }
        }
    }

    // Pseudo-classes such as :pointerover and :is(Button), property conditions, and names.
    [GeneratedRegex(@":[\w-]+(\([^)]*\))?|\[[^\]]*\]|#[\w-]+")]
    private static partial Regex Conditions();

    [GeneratedRegex(@"^(?<type>\w+)(?<classes>(\.[\w-]+)+)$")]
    private static partial Regex TypeAndClasses();
}
