using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

using AxView.Protocol;

namespace AxView.Host;

/// <summary>Moves the target app's themes, styles, resources, and data templates into the host app.</summary>
/// <remarks>
/// The importer calls only the target app's Initialize, which loads App.axaml.
/// It does not call OnFrameworkInitializationCompleted, so the target's windows and services do not start.
/// </remarks>
internal static class TargetAppImporter
{
    public static string Import(Application into, HostOptions options)
    {
        Type? appType = FindAppType(options.AssemblyName) ?? FindAppType(options.AppAssemblyName);
        if (appType is null)
        {
            return "No Application class found. Rendering without the app's styles.";
        }

        try
        {
            Application source = (Application)Activator.CreateInstance(appType)!;
            source.Initialize();

            into.RequestedThemeVariant = source.RequestedThemeVariant;

            IResourceDictionary resources = source.Resources;
            source.Resources = new ResourceDictionary();
            into.Resources = resources;

            List<IStyle> styles = [.. source.Styles];
            source.Styles.Clear();
            into.Styles.AddRange(styles);

            List<IDataTemplate> templates = [.. source.DataTemplates];
            source.DataTemplates.Clear();
            into.DataTemplates.AddRange(templates);

            return $"Styles from {appType.FullName}.";
        }
        catch (Exception ex)
        {
            // The target's App.axaml can throw any exception. The preview still works without its styles.
            return $"Cannot import {appType.FullName}: {ex.GetBaseException().Message}";
        }
    }

    private static Type? FindAppType(string? assemblyName)
    {
        if (assemblyName is null)
        {
            return null;
        }

        Assembly assembly;
        try
        {
            assembly = Assembly.Load(assemblyName);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }

        return types.FirstOrDefault(t =>
            t is { IsAbstract: false }
            && typeof(Application).IsAssignableFrom(t)
            && t.GetConstructor(Type.EmptyTypes) is not null);
    }
}
