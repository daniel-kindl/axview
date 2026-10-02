extern alias host;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;

using host::AxView.Host.Startup;

namespace AxView.Tests.Host;

public sealed class TargetStylesTests
{
    [AvaloniaFact]
    public void ReplaceWith_ChangesTheDataTemplatesOfTheApp()
    {
        IDataTemplate before = new FuncDataTemplate<string>((_, _) => new TextBlock());
        IDataTemplate after = new FuncDataTemplate<string>((_, _) => new TextBlock());
        IDataTemplate other = new FuncDataTemplate<int>((_, _) => new TextBlock());
        TargetStyles target = new([], new ResourceDictionary(), [before], null);
        Application app = new() { DataTemplates = { other } };
        target.ApplyTo(app);

        target.ReplaceWith(new TargetStyles([], new ResourceDictionary(), [after], null));

        Assert.Equal([other, after], app.DataTemplates);
        Assert.Equal([after], target.DataTemplates);
    }
}
