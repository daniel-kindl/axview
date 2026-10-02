using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

using AxView.Protocol;

namespace AxView.Host.Startup;

/// <summary>Takes the target app's themes, styles, resources, and data templates out of a new instance of the target app.</summary>
/// <remarks>
/// The importer calls only the target app's Initialize, which loads App.axaml.
/// It does not call OnFrameworkInitializationCompleted, so the target's windows and services do not start.
/// Each call creates a new instance. Thus each call gives new style objects.
/// </remarks>
internal static class TargetAppImporter
{
    /// <summary>Creates the target app and takes its styles.</summary>
    /// <param name="options">The options of the host. They name the assemblies to search for the app.</param>
    /// <param name="notes">Gets a note about the result.</param>
    /// <returns>The styles of the app, or <see cref="TargetStyles.None"/> when the app is missing or fails.</returns>
    public static TargetStyles Import(HostOptions options, List<string> notes)
    {
        Type? appType = FindAppType(options.AssemblyName) ?? FindAppType(options.AppAssemblyName);
        if (appType is null)
        {
            notes.Add("No Application class found. Rendering without the app's styles.");
            return TargetStyles.None;
        }

        try
        {
            Application source = (Application)Activator.CreateInstance(appType)!;
            source.Initialize();

            IResourceDictionary resources = source.Resources;
            source.Resources = new ResourceDictionary();

            List<IStyle> styles = [.. source.Styles];
            source.Styles.Clear();

            List<IDataTemplate> templates = [.. source.DataTemplates];
            source.DataTemplates.Clear();

            notes.Add($"Styles from {appType.FullName}.");
            return new TargetStyles(styles, resources, templates, source.RequestedThemeVariant);
        }
        catch (Exception ex)
        {
            // The target's App.axaml can throw any exception. The preview still works without its styles.
            notes.Add($"Cannot import {appType.FullName}: {ex.GetBaseException().Message}");
            return TargetStyles.None;
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
