extern alias host;

using Avalonia.Headless.XUnit;

using AxView.Tests.Fixtures;

using host::AxView.Host.UI;

namespace AxView.Tests.Host;

public sealed class ViewModelSwitchTests
{
    [AvaloniaFact]
    public void New_SelectsTheViewModelFromTheXaml()
    {
        ViewModelSwitch viewModelSwitch = new([typeof(FixtureViewModel)]);

        Assert.Null(viewModelSwitch.SelectedType);
    }

    [AvaloniaFact]
    public void SetSelectedTypeName_SelectsTheTypeWithThatFullName()
    {
        ViewModelSwitch viewModelSwitch = new([typeof(FixtureViewModel), typeof(FixtureOtherViewModel)])
        {
            SelectedTypeName = typeof(FixtureOtherViewModel).FullName,
        };

        Assert.Equal(typeof(FixtureOtherViewModel), viewModelSwitch.SelectedType);
    }

    [AvaloniaFact]
    public void SetSelectedTypeName_UnknownName_SelectsTheViewModelFromTheXaml()
    {
        ViewModelSwitch viewModelSwitch = new([typeof(FixtureViewModel)]) { SelectedTypeName = typeof(FixtureViewModel).FullName };

        viewModelSwitch.SelectedTypeName = "Gone.MissingViewModel";

        Assert.Null(viewModelSwitch.SelectedType);
    }

    [AvaloniaFact]
    public void SetSelectedTypeName_Null_SelectsTheViewModelFromTheXaml()
    {
        ViewModelSwitch viewModelSwitch = new([typeof(FixtureViewModel)]) { SelectedTypeName = typeof(FixtureViewModel).FullName };

        viewModelSwitch.SelectedTypeName = null;

        Assert.Null(viewModelSwitch.SelectedTypeName);
    }

    [AvaloniaFact]
    public void New_NoTypes_IsDisabled()
    {
        ViewModelSwitch viewModelSwitch = new([]);

        Assert.False(viewModelSwitch.IsEnabled);
    }
}
