extern alias host;

using Avalonia;

using host::AxView.Host.Startup;

namespace AxView.Tests.Host;

public sealed class BuilderExtensionsTests
{
    [Fact]
    public void ApplyAvailable_AppliesUseReactiveUIOfTheReactiveUIAvaloniaReactivePackage()
    {
        List<string> applied = [];

        _ = BuilderExtensions.ApplyAvailable(AppBuilder.Configure<Application>(), applied);

        Assert.Contains("ReactiveUI.Avalonia.Reactive.UseReactiveUI", applied);
    }
}
