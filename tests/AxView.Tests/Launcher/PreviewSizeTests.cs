using AxView.Protocol;

namespace AxView.Tests.Launcher;

public sealed class PreviewSizeTests
{
    [Theory]
    [InlineData("Fit")]
    [InlineData("Phone")]
    [InlineData("Tablet")]
    [InlineData("Desktop")]
    [InlineData("500x300")]
    public void Parse_ReadsTheTextThatToArgumentWrites(string text)
    {
        PreviewSize size = PreviewSize.Parse(text);

        Assert.Equal(text, size.ToArgument());
    }

    [Fact]
    public void Parse_Preset_GivesThePresetSize() =>
        Assert.Equal(new PreviewSize(PreviewSizePreset.Phone, 390, 844), PreviewSize.Parse("phone"));

    [Fact]
    public void Parse_WidthAndHeight_GivesACustomSize() =>
        Assert.Equal(new PreviewSize(PreviewSizePreset.Custom, 500, 300), PreviewSize.Parse("500x300"));

    [Theory]
    [InlineData("")]
    [InlineData("Huge")]
    [InlineData("Custom")]
    [InlineData("1")]
    [InlineData("500")]
    [InlineData("500x")]
    [InlineData("0x300")]
    [InlineData("500x10001")]
    [InlineData("-5x300")]
    [InlineData("axb")]
    public void Parse_RejectsTextThatIsNotASize(string text)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => PreviewSize.Parse(text));

        Assert.Contains($"'{text}'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromPreset_Fit_GivesFit() => Assert.Equal(PreviewSize.Fit, PreviewSize.FromPreset(PreviewSizePreset.Fit));

    [Fact]
    public void FromPreset_Custom_Throws() =>
        _ = Assert.Throws<ArgumentException>(() => PreviewSize.FromPreset(PreviewSizePreset.Custom));
}
