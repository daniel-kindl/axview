using System.Reflection;

using Avalonia.Controls;

namespace AxView.Host.Startup;

/// <summary>Sets the design mode flag of Avalonia, so that app code that checks the flag runs its design path.</summary>
/// <remarks>The setter of <c>Design.IsDesignMode</c> is internal in Avalonia 11 and 12. Thus the host calls it by reflection.</remarks>
internal static class DesignMode
{
    /// <summary>Sets <c>Design.IsDesignMode</c> to true.</summary>
    /// <returns>False when the runtime has no setter for the flag.</returns>
    public static bool Enable()
    {
        MethodInfo? setter = typeof(Design).GetProperty(nameof(Design.IsDesignMode))?.GetSetMethod(nonPublic: true);
        if (setter is null)
        {
            return false;
        }

        _ = setter.Invoke(null, [true]);
        return true;
    }
}
