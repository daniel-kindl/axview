using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace AxView.Host.UI;

/// <summary>Shows controls in each state, so that the user can check the app styles.</summary>
/// <remarks>
/// A real state needs real input, for example the pointer over a control. Thus the gallery sets the pseudo-classes itself.
/// A selector such as <c>Button:pointerover /template/ ContentPresenter</c> then matches as for real input.
/// </remarks>
internal static class ControlGallery
{
    private const double SectionSpacing = 24;
    private const double SectionTitleSize = 18;
    private const double HeaderOpacity = 0.6;
    private const string ErrorText = "The value is not valid.";

    private static readonly Thickness pageMargin = new(16);
    private static readonly Thickness cellMargin = new(8);

    /// <summary>Creates the gallery of the common controls, with a row for each style class of the app.</summary>
    /// <param name="appStyles">The styles of the app.</param>
    public static Control Create(IEnumerable<IStyle> appStyles) =>
        Create(GallerySections.WithStyleClasses(GallerySections.All, StyleClasses.Find(appStyles)));

    /// <summary>Creates the gallery.</summary>
    /// <param name="sections">The sections to show.</param>
    public static Control Create(IReadOnlyList<GallerySection> sections)
    {
        StackPanel page = new() { Margin = pageMargin, Spacing = SectionSpacing };
        foreach (GallerySection section in sections)
        {
            page.Children.Add(CreateSection(section));
        }

        return page;
    }

    /// <summary>Creates one control of a variant, in a state.</summary>
    /// <param name="variant">The row of the cell.</param>
    /// <param name="state">The column of the cell.</param>
    public static Control CreateCell(GalleryVariant variant, GalleryState state)
    {
        Control cell = variant.Create();
        cell.Margin = cellMargin;
        cell.VerticalAlignment = VerticalAlignment.Center;
        cell.HorizontalAlignment = HorizontalAlignment.Left;

        ApplyState(cell, state);
        return cell;
    }

    /// <summary>Puts a control in a state without real input. Also for the items of a list in a cell.</summary>
    /// <param name="control">The control.</param>
    /// <param name="state">The state.</param>
    public static void ApplyState(Control control, GalleryState state)
    {
        // Real pointer input would clear a forced pseudo-class. Only a control in the normal state takes input.
        control.IsHitTestVisible = state == GalleryState.Normal;
        IPseudoClasses pseudoClasses = control.Classes;
        switch (state)
        {
            case GalleryState.PointerOver:
                pseudoClasses.Set(":pointerover", true);
                break;
            case GalleryState.Pressed:
                pseudoClasses.Set(":pointerover", true);
                pseudoClasses.Set(":pressed", true);
                break;
            case GalleryState.Focused:
                pseudoClasses.Set(":focus", true);
                pseudoClasses.Set(":focus-visible", true);
                break;
            case GalleryState.Disabled:
                control.IsEnabled = false;
                break;
            case GalleryState.Error:
                DataValidationErrors.SetErrors(control, [ErrorText]);
                break;
            default:
                break;
        }
    }

    private static StackPanel CreateSection(GallerySection section)
    {
        Grid grid = new() { ColumnDefinitions = new ColumnDefinitions("Auto," + string.Join(',', section.States.Select(_ => "Auto"))) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int column = 0; column < section.States.Count; column++)
        {
            AddToGrid(grid, Header(section.States[column].ToString()), 0, column + 1);
        }

        for (int row = 0; row < section.Variants.Count; row++)
        {
            GalleryVariant variant = section.Variants[row];
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            AddToGrid(grid, Header(variant.Name), row + 1, 0);
            for (int column = 0; column < section.States.Count; column++)
            {
                AddToGrid(grid, new GalleryCell { Child = CreateCell(variant, section.States[column]) }, row + 1, column + 1);
            }
        }

        TextBlock title = new() { Text = section.Title, FontSize = SectionTitleSize, FontWeight = FontWeight.SemiBold };
        return new StackPanel { Children = { title, grid } };
    }

    private static TextBlock Header(string text) => new()
    {
        Text = text,
        Margin = cellMargin,
        Opacity = HeaderOpacity,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static void AddToGrid(Grid grid, Control control, int row, int column)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        grid.Children.Add(control);
    }
}
