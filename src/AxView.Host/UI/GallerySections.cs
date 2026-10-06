using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace AxView.Host.UI;

/// <summary>The sections of the <see cref="ControlGallery"/>.</summary>
/// <remarks>The sections use only controls of the Avalonia 11.0 API. Avalonia 12 has the same controls.</remarks>
internal static class GallerySections
{
    private const double InputWidth = 180;
    private const double RangeWidth = 180;
    private const double RangeValue = 40;
    private const double ScrollBarViewport = 25;
    private const string Placeholder = "Placeholder";
    private const string Text = "Text";

    private static readonly GalleryState[] buttonStates =
        [GalleryState.Normal, GalleryState.PointerOver, GalleryState.Pressed, GalleryState.Focused, GalleryState.Disabled];

    private static readonly GalleryState[] toggleStates =
        [GalleryState.Normal, GalleryState.PointerOver, GalleryState.Pressed, GalleryState.Disabled];

    private static readonly GalleryState[] inputStates =
        [GalleryState.Normal, GalleryState.PointerOver, GalleryState.Focused, GalleryState.Disabled, GalleryState.Error];

    private static readonly GalleryState[] rangeStates = [GalleryState.Normal, GalleryState.PointerOver, GalleryState.Disabled];

    private static readonly GalleryState[] containerStates = [GalleryState.Normal, GalleryState.Disabled];

    /// <summary>Gets the sections of the common controls.</summary>
    public static IReadOnlyList<GallerySection> All { get; } =
    [
        new("Buttons", buttonStates,
        [
            new("Button", () => new Button { Content = "Button" }),
            new("RepeatButton", () => new RepeatButton { Content = "RepeatButton" }),
            new("ToggleButton", () => new ToggleButton { Content = "ToggleButton" }),
            new("ToggleButton checked", () => new ToggleButton { Content = "ToggleButton", IsChecked = true }),
            new("SplitButton", () => new SplitButton { Content = "SplitButton" }),
        ]),
        new("CheckBox", toggleStates,
        [
            new("Unchecked", () => new CheckBox { Content = "CheckBox" }),
            new("Checked", () => new CheckBox { Content = "CheckBox", IsChecked = true }),
            new("Indeterminate", () => new CheckBox { Content = "CheckBox", IsThreeState = true, IsChecked = null }),
        ]),
        new("RadioButton", toggleStates,
        [
            new("Unchecked", () => Radio(isChecked: false)),
            new("Checked", () => Radio(isChecked: true)),
        ]),
        new("ToggleSwitch", toggleStates,
        [
            new("Off", () => new ToggleSwitch()),
            new("On", () => new ToggleSwitch { IsChecked = true }),
        ]),
        new("Text input", inputStates,
        [
            new("TextBox empty", () => new TextBox { Watermark = Placeholder, Width = InputWidth }),
            new("TextBox", () => new TextBox { Text = Text, Width = InputWidth }),
            new("NumericUpDown", () => new NumericUpDown { Value = (decimal)RangeValue, Width = InputWidth }),
            new("AutoCompleteBox", () => new AutoCompleteBox { Text = Text, Width = InputWidth }),
        ]),
        new("Selection input", inputStates,
        [
            new("ComboBox empty", () => new ComboBox { PlaceholderText = Placeholder, Width = InputWidth }),
            new("ComboBox", () => new ComboBox { ItemsSource = new[] { Text }, SelectedIndex = 0, Width = InputWidth }),
            new("CalendarDatePicker", () => new CalendarDatePicker { SelectedDate = DateTime.Today, Width = InputWidth }),
            new("DatePicker", () => new DatePicker { SelectedDate = DateTimeOffset.Now }),
            new("TimePicker", () => new TimePicker { SelectedTime = DateTime.Now.TimeOfDay }),
        ]),
        new("Ranges", rangeStates,
        [
            new("Slider", () => new Slider { Value = RangeValue, Width = RangeWidth }),
            new("ProgressBar", () => new ProgressBar { Value = RangeValue, Width = RangeWidth }),
            new("ProgressBar indeterminate", () => new ProgressBar { IsIndeterminate = true, Width = RangeWidth }),
            new("ScrollBar", () => new ScrollBar
            {
                Orientation = Orientation.Horizontal,
                AllowAutoHide = false,
                ViewportSize = ScrollBarViewport,
                Value = RangeValue,
                Width = RangeWidth,
            }),
        ]),
        new("Containers", containerStates,
        [
            new("Expander", () => new Expander { Header = "Expander", Content = Text }),
            new("Expander expanded", () => new Expander { Header = "Expander", Content = Text, IsExpanded = true }),
            new("TabControl", TabControl),
            new("Menu", Menu),
        ]),
        new("Lists", containerStates,
        [
            new("ListBox", ListBox),
            new("TreeView", TreeView),
        ]),
    ];

    /// <summary>
    /// Adds a row for each style class and each keyed control theme of the app. The rows follow each row of the same control type.
    /// </summary>
    /// <param name="sections">The sections.</param>
    /// <param name="classes">The style classes of the app.</param>
    /// <param name="themes">The keyed control themes of the app.</param>
    public static IReadOnlyList<GallerySection> WithAppStyles(
        IReadOnlyList<GallerySection> sections, IReadOnlyList<StyleClass> classes, IReadOnlyList<KeyedControlTheme> themes) =>
        [.. sections.Select(section => section with { Variants = [.. section.Variants.SelectMany(v => WithAppStyles(v, classes, themes))] })];

    // The variant creates a control only to get its type. The style of a class and a control theme apply only to that exact type.
    private static IEnumerable<GalleryVariant> WithAppStyles(
        GalleryVariant variant, IReadOnlyList<StyleClass> classes, IReadOnlyList<KeyedControlTheme> themes)
    {
        yield return variant;
        Type type = variant.Create().GetType();
        foreach (StyleClass styleClass in classes.Where(c => c.TypeName == type.Name))
        {
            string[] names = styleClass.Classes.Split('.');
            yield return new GalleryVariant($"{variant.Name} .{styleClass.Classes}", () =>
            {
                Control control = variant.Create();
                control.Classes.AddRange(names);
                return control;
            });
        }

        foreach (KeyedControlTheme theme in themes.Where(t => t.Theme.TargetType == type))
        {
            yield return new GalleryVariant($"{variant.Name} {{{theme.Key}}}", () =>
            {
                Control control = variant.Create();
                control.Theme = theme.Theme;
                return control;
            });
        }
    }

    // A RadioButton without a group name unchecks the other RadioButtons of its parent.
    private static RadioButton Radio(bool isChecked) =>
        new() { Content = "RadioButton", IsChecked = isChecked, GroupName = Guid.NewGuid().ToString() };

    // A virtualizing panel shows only the items in the viewport of the last layout. A snapshot lays out the full gallery.
    private static ListBox ListBox() => new()
    {
        ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel()),
        SelectionMode = SelectionMode.Multiple,
        Items =
        {
            ListBoxItem("Normal", GalleryState.Normal),
            ListBoxItem("Pointer over", GalleryState.PointerOver),
            ListBoxItem("Pressed", GalleryState.Pressed),
            new ListBoxItem { Content = "Selected", IsSelected = true },
            ListBoxItem("Disabled", GalleryState.Disabled),
        },
    };

    private static ListBoxItem ListBoxItem(string text, GalleryState state)
    {
        ListBoxItem item = new() { Content = text };
        ControlGallery.ApplyState(item, state);
        return item;
    }

    private static TabControl TabControl() => new()
    {
        Items =
        {
            new TabItem { Header = "Selected", Content = Text },
            TabItem("Pointer over", GalleryState.PointerOver),
            TabItem("Disabled", GalleryState.Disabled),
        },
    };

    private static TabItem TabItem(string text, GalleryState state)
    {
        TabItem item = new() { Header = text };
        ControlGallery.ApplyState(item, state);
        return item;
    }

    private static Menu Menu() => new()
    {
        Items =
        {
            new MenuItem { Header = "Normal" },
            MenuItem("Pointer over", GalleryState.PointerOver),
            new MenuItem { Header = "Selected", IsSelected = true },
            MenuItem("Disabled", GalleryState.Disabled),
        },
    };

    private static MenuItem MenuItem(string text, GalleryState state)
    {
        MenuItem item = new() { Header = text };
        ControlGallery.ApplyState(item, state);
        return item;
    }

    private static TreeView TreeView() => new()
    {
        Items =
        {
            new TreeViewItem
            {
                Header = "Expanded",
                IsExpanded = true,
                Items =
                {
                    new TreeViewItem { Header = "Selected", IsSelected = true },
                    TreeViewItem("Pointer over", GalleryState.PointerOver),
                    TreeViewItem("Disabled", GalleryState.Disabled),
                },
            },
            new TreeViewItem { Header = "Collapsed", Items = { new TreeViewItem { Header = Text } } },
        },
    };

    private static TreeViewItem TreeViewItem(string text, GalleryState state)
    {
        TreeViewItem item = new() { Header = text };
        ControlGallery.ApplyState(item, state);
        return item;
    }
}
