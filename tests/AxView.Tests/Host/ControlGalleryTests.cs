extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;

using AxView.Tests.Fixtures;

using host::AxView.Host.Startup;
using host::AxView.Host.UI;

namespace AxView.Tests.Host;

public sealed class ControlGalleryTests
{
    private static readonly GalleryVariant button = new("Button", () => new Button { Content = "Button" });

    [AvaloniaFact]
    public void Create_HasACellForEachVariantAndState()
    {
        GallerySection section = new("Buttons", [GalleryState.Normal, GalleryState.Disabled], [button, button]);

        Control gallery = ControlGallery.Create([section]);

        Assert.Equal(4, gallery.GetLogicalDescendants().OfType<Button>().Count());
    }

    [AvaloniaFact]
    public void Create_DefaultSections_ShowTheCommonControls()
    {
        Control gallery = ControlGallery.Create(GallerySections.All);

        Type[] types = [.. gallery.GetLogicalDescendants().Select(c => c.GetType()).Distinct()];
        Assert.Contains(typeof(Button), types);
        Assert.Contains(typeof(CheckBox), types);
        Assert.Contains(typeof(TextBox), types);
        Assert.Contains(typeof(ComboBox), types);
        Assert.Contains(typeof(Slider), types);
    }

    [AvaloniaFact]
    public void WithStyleClasses_AddsARowForTheClassAfterTheRowOfTheType()
    {
        GalleryVariant checkBox = new("CheckBox", () => new CheckBox());
        GallerySection section = new("Mixed", [GalleryState.Normal], [button, checkBox]);

        IReadOnlyList<GallerySection> sections = GallerySections.WithStyleClasses([section], [new StyleClass("Button", "primary.large")]);

        IReadOnlyList<GalleryVariant> variants = sections.Single().Variants;
        Assert.Equal(["Button", "Button .primary.large", "CheckBox"], variants.Select(v => v.Name));
        Assert.Equal(["primary", "large"], variants[1].Create().Classes);
    }

    [AvaloniaFact]
    public void CreateCell_Normal_TakesInput()
    {
        Control cell = ControlGallery.CreateCell(button, GalleryState.Normal);

        Assert.True(cell.IsHitTestVisible);
        Assert.DoesNotContain(":pointerover", cell.Classes);
    }

    [AvaloniaTheory]
    [InlineData(nameof(GalleryState.PointerOver), ":pointerover")]
    [InlineData(nameof(GalleryState.Pressed), ":pressed")]
    [InlineData(nameof(GalleryState.Focused), ":focus")]
    [InlineData(nameof(GalleryState.Focused), ":focus-visible")]
    public void CreateCell_ForcedState_SetsThePseudoClass(string state, string pseudoClass)
    {
        Control cell = ControlGallery.CreateCell(button, Enum.Parse<GalleryState>(state));

        Assert.Contains(pseudoClass, cell.Classes);
    }

    // Real pointer input would clear the forced pseudo-class.
    [AvaloniaFact]
    public void CreateCell_ForcedState_TakesNoInput()
    {
        Control cell = ControlGallery.CreateCell(button, GalleryState.PointerOver);

        Assert.False(cell.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void CreateCell_Disabled_DisablesTheControl()
    {
        Control cell = ControlGallery.CreateCell(button, GalleryState.Disabled);

        Assert.False(cell.IsEnabled);
    }

    [AvaloniaFact]
    public void CreateCell_Error_SetsAValidationError()
    {
        Control cell = ControlGallery.CreateCell(new GalleryVariant("TextBox", () => new TextBox()), GalleryState.Error);

        Assert.True(DataValidationErrors.GetHasErrors(cell));
    }

    [AvaloniaFact]
    public void Show_ControlThrowsInLayout_CellShowsTheErrorAndTheOtherCellsStay()
    {
        GalleryVariant throwing = new("Throwing", () => new FixtureThrowingControl());
        GallerySection section = new("Mixed", [GalleryState.Normal], [throwing, button]);
        Window window = new() { Content = ControlGallery.Create([section]) };

        window.Show();

        Assert.Contains(window.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text?.Contains("Layout failed.", StringComparison.Ordinal) == true);
        _ = Assert.Single(window.GetLogicalDescendants().OfType<Button>());
        Assert.Empty(window.GetLogicalDescendants().OfType<FixtureThrowingControl>());
        window.Close();
    }

    [AvaloniaFact]
    public void Show_ForcedState_AppStyleForTheStateApplies()
    {
        Style hoverStyle = new(x => x.OfType<Button>().Class(":pointerover"))
        {
            Setters = { new Setter(Button.BackgroundProperty, Brushes.Red) },
        };
        GallerySection section = new("Buttons", [GalleryState.PointerOver], [button]);
        PreviewScope scope = new(new TargetStyles([hoverStyle], new ResourceDictionary(), [], null))
        {
            Content = ControlGallery.Create([section]),
        };
        Window window = new() { Content = scope };

        window.Show();

        Button cell = scope.GetLogicalDescendants().OfType<Button>().Single();
        Assert.Same(Brushes.Red, cell.Background);
        window.Close();
    }
}
