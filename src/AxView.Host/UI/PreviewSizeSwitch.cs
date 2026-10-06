using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

using AxView.Protocol;

namespace AxView.Host.UI;

/// <summary>Lets the user select the size of the preview: a preset, or a custom width and height.</summary>
/// <remarks>A typed width or height selects Custom.</remarks>
internal sealed class PreviewSizeSwitch : StackPanel
{
    private const double MinimumSide = 1;
    private const double MaximumSide = 10000;

    private static readonly Dictionary<PreviewSizePreset, Size> presetSizes = new()
    {
        [PreviewSizePreset.Phone] = new Size(390, 844),
        [PreviewSizePreset.Tablet] = new Size(820, 1180),
        [PreviewSizePreset.Desktop] = new Size(1920, 1080),
    };

    private readonly PreviewStage target;
    private readonly ComboBox presets;
    private readonly NumericUpDown widthBox;
    private readonly NumericUpDown heightBox;
    private bool isSyncing;

    public PreviewSizeSwitch(PreviewStage target)
    {
        this.target = target;
        Orientation = Orientation.Horizontal;
        Spacing = 4;

        presets = new ComboBox { ItemsSource = Enum.GetValues<PreviewSizePreset>(), SelectedItem = PreviewSizePreset.Fit };
        presets.SelectionChanged += (_, _) => OnPresetSelected();
        widthBox = SideBox("Width");
        heightBox = SideBox("Height");

        Children.Add(presets);
        Children.Add(widthBox);
        Children.Add(new TextBlock { Text = "×", VerticalAlignment = VerticalAlignment.Center });
        Children.Add(heightBox);
        OnPresetSelected();
    }

    /// <summary>The selected size. For a preset, the getter gives the preset width and height.</summary>
    public PreviewSize Size
    {
        get
        {
            PreviewSizePreset preset = presets.SelectedItem is PreviewSizePreset selected ? selected : PreviewSizePreset.Fit;
            return preset == PreviewSizePreset.Fit
                ? PreviewSize.Fit
                : new PreviewSize(preset, (double)(widthBox.Value ?? 0), (double)(heightBox.Value ?? 0));
        }

        set
        {
            if (value.Preset == PreviewSizePreset.Custom)
            {
                SetBoxes(new Size(value.Width, value.Height));
            }

            presets.SelectedItem = value.Preset;
            Apply();
        }
    }

    private NumericUpDown SideBox(string name)
    {
        NumericUpDown box = new()
        {
            Width = 64,
            ShowButtonSpinner = false,
            Minimum = (decimal)MinimumSide,
            Maximum = (decimal)MaximumSide,
            Increment = 10,
            FormatString = "0",
        };
        ToolTip.SetTip(box, name);
        box.ValueChanged += (_, _) => OnSideTyped();
        return box;
    }

    private void OnPresetSelected()
    {
        PreviewSizePreset preset = Size.Preset;
        if (presetSizes.TryGetValue(preset, out Size size))
        {
            SetBoxes(size);
        }

        // Fit has no width and height to show.
        foreach (Control child in Children.Skip(1))
        {
            child.IsVisible = preset != PreviewSizePreset.Fit;
        }

        Apply();
    }

    private void OnSideTyped()
    {
        if (isSyncing)
        {
            return;
        }

        if (presets.SelectedItem is not PreviewSizePreset.Custom)
        {
            // Custom has no preset size, so the selection keeps the typed values.
            presets.SelectedItem = PreviewSizePreset.Custom;
        }

        Apply();
    }

    private void SetBoxes(Size size)
    {
        isSyncing = true;
        widthBox.Value = (decimal)Math.Clamp(size.Width, MinimumSide, MaximumSide);
        heightBox.Value = (decimal)Math.Clamp(size.Height, MinimumSide, MaximumSide);
        isSyncing = false;
    }

    private void Apply() => target.Size = Size;
}
