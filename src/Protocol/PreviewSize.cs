using System.Globalization;

namespace AxView.Protocol;

/// <summary>The size of the preview.</summary>
/// <remarks>
/// The text form is the preset name, for example <c>Phone</c>, or the width and height of a custom size, for example <c>500x300</c>.
/// </remarks>
/// <param name="Preset">The selected size preset.</param>
/// <param name="Width">The width of the preview, in device-independent pixels. Not applicable to <see cref="PreviewSizePreset.Fit"/>.</param>
/// <param name="Height">The height of the preview, in device-independent pixels. Not applicable to <see cref="PreviewSizePreset.Fit"/>.</param>
internal sealed record PreviewSize(PreviewSizePreset Preset, double Width, double Height)
{
    /// <summary>The smallest width or height of a custom size.</summary>
    public const double MinimumSide = 1;

    /// <summary>The largest width or height of a custom size.</summary>
    public const double MaximumSide = 10000;

    private const char SideSeparator = 'x';

    private static readonly Dictionary<PreviewSizePreset, PreviewSize> presetSizes = new()
    {
        [PreviewSizePreset.Phone] = new(PreviewSizePreset.Phone, 390, 844),
        [PreviewSizePreset.Tablet] = new(PreviewSizePreset.Tablet, 820, 1180),
        [PreviewSizePreset.Desktop] = new(PreviewSizePreset.Desktop, 1920, 1080),
    };

    /// <summary>The preview fills the free space of the window.</summary>
    public static PreviewSize Fit { get; } = new(PreviewSizePreset.Fit, 0, 0);

    /// <summary>Gives the size of a preset.</summary>
    /// <exception cref="ArgumentException">The preset is <see cref="PreviewSizePreset.Custom"/>. It has no size of its own.</exception>
    public static PreviewSize FromPreset(PreviewSizePreset preset)
    {
        if (preset == PreviewSizePreset.Fit)
        {
            return Fit;
        }

        return presetSizes.TryGetValue(preset, out PreviewSize? size)
            ? size
            : throw new ArgumentException($"The preset '{preset}' has no size.", nameof(preset));
    }

    /// <summary>Reads the text form of a size. The preset name can have any case.</summary>
    /// <exception cref="ArgumentException">The text is not a preset name or a valid width and height.</exception>
    public static PreviewSize Parse(string text)
    {
        string? presetName = Enum.GetNames<PreviewSizePreset>()
            .FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase));
        if (presetName is not (null or nameof(PreviewSizePreset.Custom)))
        {
            return FromPreset(Enum.Parse<PreviewSizePreset>(presetName));
        }

        string[] sides = text.Split(SideSeparator);
        if (sides.Length == 2 && TryParseSide(sides[0], out double width) && TryParseSide(sides[1], out double height))
        {
            return new PreviewSize(PreviewSizePreset.Custom, width, height);
        }

        throw new ArgumentException(
            $"The size '{text}' is not Fit, Phone, Tablet, Desktop, or <width>{SideSeparator}<height> " +
            $"with each side from {MinimumSide} to {MaximumSide}.");
    }

    /// <summary>Writes the text form that <see cref="Parse"/> reads.</summary>
    public string ToArgument() =>
        Preset == PreviewSizePreset.Custom
            ? string.Create(CultureInfo.InvariantCulture, $"{Width}{SideSeparator}{Height}")
            : Preset.ToString();

    private static bool TryParseSide(string text, out double side) =>
        double.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out side)
        && side is >= MinimumSide and <= MaximumSide;
}
