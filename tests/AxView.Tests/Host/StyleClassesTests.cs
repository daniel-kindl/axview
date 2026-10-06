extern alias host;

using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

using host::AxView.Host.UI;

namespace AxView.Tests.Host;

public sealed class StyleClassesTests
{
    [AvaloniaFact]
    public void Find_TypeAndClass_GivesTheClass()
    {
        Styles styles = [new Style(x => x.OfType<Button>().Class("primary"))];

        IReadOnlyList<StyleClass> found = StyleClasses.Find(styles);

        Assert.Equal([new StyleClass("Button", "primary")], found);
    }

    [AvaloniaFact]
    public void Find_TwoClasses_GivesBothInOneEntry()
    {
        Styles styles = [new Style(x => x.OfType<Button>().Class("primary").Class("large"))];

        IReadOnlyList<StyleClass> found = StyleClasses.Find(styles);

        Assert.Equal([new StyleClass("Button", "primary.large")], found);
    }

    [AvaloniaFact]
    public void Find_PseudoClassesAndTemplateParts_GivesTheClassOnce()
    {
        Styles styles =
        [
            new Style(x => x.OfType<Button>().Class("primary")),
            new Style(x => x.OfType<Button>().Class("primary").Class(":pointerover").Template().OfType<ContentPresenter>()),
        ];

        IReadOnlyList<StyleClass> found = StyleClasses.Find(styles);

        Assert.Equal([new StyleClass("Button", "primary")], found);
    }

    [AvaloniaFact]
    public void Find_NestedStyleWithAClass_GivesTheTypeOfTheParent()
    {
        Style parent = new(x => x.OfType<Button>()) { Children = { new Style(x => x.Nesting().Class("ghost")) } };
        Styles styles = [parent];

        IReadOnlyList<StyleClass> found = StyleClasses.Find(styles);

        Assert.Equal([new StyleClass("Button", "ghost")], found);
    }

    [AvaloniaFact]
    public void Find_StylesInStyles_GivesTheInnerClasses()
    {
        Styles styles = [new Styles { new Style(x => x.OfType<CheckBox>().Class("switch")) }];

        IReadOnlyList<StyleClass> found = StyleClasses.Find(styles);

        Assert.Equal([new StyleClass("CheckBox", "switch")], found);
    }

    [AvaloniaFact]
    public void Find_PseudoClassOnly_GivesNothing()
    {
        Styles styles = [new Style(x => x.OfType<Button>().Class(":pointerover"))];

        IReadOnlyList<StyleClass> found = StyleClasses.Find(styles);

        Assert.Empty(found);
    }
}
