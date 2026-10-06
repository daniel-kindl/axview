extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

using host::AxView.Host.UI;

namespace AxView.Tests.Host;

public sealed class ControlThemesTests
{
    private static readonly ControlTheme pillButton = new(typeof(Button));

    [AvaloniaFact]
    public void Find_KeyedTheme_GivesTheKeyAndTheTheme()
    {
        ResourceDictionary resources = new() { ["PillButton"] = pillButton };

        IReadOnlyList<KeyedControlTheme> found = ControlThemes.Find([], resources);

        Assert.Equal([new KeyedControlTheme("PillButton", pillButton)], found);
    }

    // A theme with a type key is the default theme of the type. Each control of the gallery has it already.
    [AvaloniaFact]
    public void Find_TypeKey_GivesNothing()
    {
        ResourceDictionary resources = new() { [typeof(Button)] = pillButton };

        IReadOnlyList<KeyedControlTheme> found = ControlThemes.Find([], resources);

        Assert.Empty(found);
    }

    [AvaloniaFact]
    public void Find_OtherResource_GivesNothing()
    {
        ResourceDictionary resources = new() { ["Margin"] = 4.0 };

        IReadOnlyList<KeyedControlTheme> found = ControlThemes.Find([], resources);

        Assert.Empty(found);
    }

    [AvaloniaFact]
    public void Find_MergedDictionary_GivesTheInnerTheme()
    {
        ResourceDictionary inner = new() { ["PillButton"] = pillButton };
        ResourceDictionary resources = new() { MergedDictionaries = { inner } };

        IReadOnlyList<KeyedControlTheme> found = ControlThemes.Find([], resources);

        Assert.Equal([new KeyedControlTheme("PillButton", pillButton)], found);
    }

    [AvaloniaFact]
    public void Find_ResourcesOfStyles_GivesTheTheme()
    {
        Styles inner = new() { Resources = { ["PillButton"] = pillButton } };
        Styles styles = [inner];

        IReadOnlyList<KeyedControlTheme> found = ControlThemes.Find(styles, new ResourceDictionary());

        Assert.Equal([new KeyedControlTheme("PillButton", pillButton)], found);
    }

    [AvaloniaFact]
    public void Find_ResourcesOfAStyle_GivesTheTheme()
    {
        Style style = new(x => x.OfType<Button>()) { Resources = { ["PillButton"] = pillButton } };
        Styles styles = [style];

        IReadOnlyList<KeyedControlTheme> found = ControlThemes.Find(styles, new ResourceDictionary());

        Assert.Equal([new KeyedControlTheme("PillButton", pillButton)], found);
    }

    // The keyed themes of an Avalonia theme are parts of its default themes. The app does not use them.
    [AvaloniaFact]
    public void Find_AvaloniaTheme_GivesNothing()
    {
        Styles styles = [new FluentTheme()];

        IReadOnlyList<KeyedControlTheme> found = ControlThemes.Find(styles, new ResourceDictionary());

        Assert.Empty(found);
    }
}
