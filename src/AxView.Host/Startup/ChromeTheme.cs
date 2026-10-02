using System.Reflection;

using Avalonia.Styling;

namespace AxView.Host.Startup;

/// <summary>Gives the control theme of the AxView chrome.</summary>
/// <remarks>
/// The host cannot reference a theme package: the target app decides which ones exist, and in which version.
/// </remarks>
internal static class ChromeTheme
{
    private static readonly (string Assembly, string Type)[] known =
    [
        ("Avalonia.Themes.Fluent", "Avalonia.Themes.Fluent.FluentTheme"),
        ("Avalonia.Themes.Simple", "Avalonia.Themes.Simple.SimpleTheme"),
    ];

    /// <summary>Creates the Fluent theme, else the Simple theme, when the target app ships it.</summary>
    /// <returns>The theme, or null when the target app ships neither theme.</returns>
    public static IStyle? Load()
    {
        foreach ((string assemblyName, string typeName) in known)
        {
            Type? type;
            try
            {
                type = Assembly.Load(assemblyName).GetType(typeName);
            }
            catch (FileNotFoundException)
            {
                continue;
            }

            // The constructor has one optional parameter: an IServiceProvider.
            ConstructorInfo? constructor = type?.GetConstructors().FirstOrDefault(c => c.GetParameters().All(p => p.IsOptional));
            if (constructor is not null)
            {
                object?[] arguments = [.. constructor.GetParameters().Select(p => p.DefaultValue)];
                return (IStyle)constructor.Invoke(arguments);
            }
        }

        return null;
    }
}
