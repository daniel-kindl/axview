extern alias host;

using Avalonia.Controls;

using host::AxView.Host.Startup;

namespace AxView.Tests.Host;

// The flag is static. Other tests must not see it while it is set.
[CollectionDefinition(nameof(DesignModeTests), DisableParallelization = true)]
[Collection(nameof(DesignModeTests))]
public sealed class DesignModeTests
{
    [Fact]
    public void Enable_SetsTheDesignModeFlagOfAvalonia()
    {
        try
        {
            bool isEnabled = DesignMode.Enable();

            Assert.True(isEnabled);
            Assert.True(Design.IsDesignMode);
        }
        finally
        {
            typeof(Design).GetProperty(nameof(Design.IsDesignMode))!.SetValue(null, false);
        }
    }
}
