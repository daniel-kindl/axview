extern alias host;

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;

using host::AxView.Host.UI;
using host::AxView.Protocol;

namespace AxView.Tests.Host;

public sealed class PreviewSizeSwitchTests
{
    [AvaloniaFact]
    public void New_SelectsFit()
    {
        PreviewSizeSwitch sizeSwitch = new(new ContentControl());

        Assert.Equal(PreviewSize.Fit, sizeSwitch.Size);
    }

    [AvaloniaFact]
    public void SetSize_Phone_SetsThePhoneSizeOnTheTarget()
    {
        ContentControl target = new();

        _ = new PreviewSizeSwitch(target) { Size = new PreviewSize(PreviewSizePreset.Phone, 0, 0) };

        Assert.Equal(390, target.Width);
        Assert.Equal(844, target.Height);
    }

    [AvaloniaFact]
    public void SetSize_Custom_SetsTheCustomSizeOnTheTarget()
    {
        ContentControl target = new();

        _ = new PreviewSizeSwitch(target) { Size = new PreviewSize(PreviewSizePreset.Custom, 500, 300) };

        Assert.Equal(500, target.Width);
        Assert.Equal(300, target.Height);
    }

    [AvaloniaFact]
    public void SetSize_Fit_ClearsTheSizeSoTheTargetFillsTheSpace()
    {
        ContentControl target = new();
        PreviewSizeSwitch sizeSwitch = new(target) { Size = new PreviewSize(PreviewSizePreset.Tablet, 0, 0) };

        sizeSwitch.Size = PreviewSize.Fit;

        Assert.False(target.IsSet(Layoutable.WidthProperty));
        Assert.False(target.IsSet(Layoutable.HeightProperty));
    }

    [AvaloniaFact]
    public void Size_GivesThePresetDimensions()
    {
        PreviewSizeSwitch sizeSwitch = new(new ContentControl()) { Size = new PreviewSize(PreviewSizePreset.Desktop, 0, 0) };

        Assert.Equal(new PreviewSize(PreviewSizePreset.Desktop, 1920, 1080), sizeSwitch.Size);
    }

    [AvaloniaFact]
    public void TypeWidth_OnAPreset_SelectsCustomAndKeepsTheHeight()
    {
        ContentControl target = new();
        PreviewSizeSwitch sizeSwitch = new(target) { Size = new PreviewSize(PreviewSizePreset.Phone, 0, 0) };

        sizeSwitch.Children.OfType<NumericUpDown>().First().Value = 500;

        Assert.Equal(new PreviewSize(PreviewSizePreset.Custom, 500, 844), sizeSwitch.Size);
        Assert.Equal(500, target.Width);
        Assert.Equal(844, target.Height);
    }

    [AvaloniaFact]
    public void SetSize_Fit_HidesTheWidthAndHeight()
    {
        PreviewSizeSwitch sizeSwitch = new(new ContentControl()) { Size = new PreviewSize(PreviewSizePreset.Phone, 0, 0) };

        sizeSwitch.Size = PreviewSize.Fit;

        Assert.All(sizeSwitch.Children.OfType<NumericUpDown>(), box => Assert.False(box.IsVisible));
    }
}
