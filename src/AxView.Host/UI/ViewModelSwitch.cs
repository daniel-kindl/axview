using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace AxView.Host.UI;

/// <summary>Lets the user select the ViewModel type of the preview, or the DataContext from the XAML.</summary>
/// <remarks>The first item is the DataContext from the XAML. The other items are the ViewModel types, by short name.</remarks>
internal sealed class ViewModelSwitch : ComboBox
{
    private const string FromXaml = "Auto (from XAML)";
    private const double MinimumWidth = 240;
    private const string Help =
        "The DataContext of the preview.\n" +
        "Auto: Design.DataContext, else a new x:DataType.\n" +
        "The list has the *ViewModel types with a parameterless constructor.";

    private readonly IReadOnlyList<Type> types;

    /// <summary>Shows the types and selects the DataContext from the XAML.</summary>
    /// <param name="types">The ViewModel types that the user can select.</param>
    public ViewModelSwitch(IReadOnlyList<Type> types)
    {
        this.types = types;
        ItemsSource = new object[] { FromXaml }.Concat(types).ToList();
        ItemTemplate = new FuncDataTemplate<object>((item, _) => Item(item));
        SelectedItem = FromXaml;
        IsEnabled = types.Count > 0;
        MinWidth = MinimumWidth;
        ToolTip.SetTip(this, Help);
    }

    /// <summary>Gets the selected ViewModel type. Null when the DataContext from the XAML is selected.</summary>
    public Type? SelectedType => SelectedItem as Type;

    /// <summary>Gets or sets the full name of the selected type. Null or an unknown name selects the DataContext from the XAML.</summary>
    public string? SelectedTypeName
    {
        get => SelectedType?.FullName;
        set => SelectedItem = types.FirstOrDefault(t => t.FullName == value) ?? (object)FromXaml;
    }

    // A subclass has no control theme of its own. Use the ComboBox theme.
    protected override Type StyleKeyOverride => typeof(ComboBox);

    private static TextBlock Item(object? item)
    {
        if (item is not Type type)
        {
            return new TextBlock { Text = FromXaml };
        }

        TextBlock text = new() { Text = type.Name };
        ToolTip.SetTip(text, type.FullName);
        return text;
    }
}
