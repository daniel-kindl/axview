using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace AxView.Host.Startup;

/// <summary>Takes the target app's themes, styles, resources, and data templates out of a new instance of the target app.</summary>
/// <remarks>
/// The importer calls only the target app's Initialize, which loads App.axaml.
/// It calls OnFrameworkInitializationCompleted only when the user opts in. The ApplicationLifetime is null then,
/// so the target's windows do not start. The target can set up static services there, for example for a ViewLocator.
/// Each call creates a new instance. Thus each call gives new style objects.
/// </remarks>
internal static class TargetAppImporter
{
    /// <summary>Creates the target app and takes its styles.</summary>
    /// <param name="projects">The projects of the preview. The search for the app looks in the view assembly, then in the app assembly.</param>
    /// <param name="notes">Gets a note about the result.</param>
    /// <param name="runStartup">True to call OnFrameworkInitializationCompleted of the app.</param>
    /// <returns>The styles of the app, or <see cref="TargetStyles.None"/> when the app is missing or fails.</returns>
    public static TargetStyles Import(ProjectSet projects, List<string> notes, bool runStartup = false)
    {
        Type? appType = FindAppType(projects.ViewAssembly) ?? FindAppType(projects.AppAssembly);
        if (appType is null)
        {
            notes.Add("No Application class found. Rendering without the app's styles.");
            return TargetStyles.None;
        }

        return ImportFrom(appType, runStartup, notes);
    }

    /// <summary>Creates an app of the given type and takes its styles.</summary>
    /// <param name="appType">The type of the target app.</param>
    /// <param name="runStartup">True to call OnFrameworkInitializationCompleted of the app.</param>
    /// <param name="notes">Gets a note about the result.</param>
    /// <returns>The styles of the app, or <see cref="TargetStyles.None"/> when the app fails.</returns>
    public static TargetStyles ImportFrom(Type appType, bool runStartup, List<string> notes)
    {
        try
        {
            Application source = (Application)Activator.CreateInstance(appType)!;
            source.Initialize();
            if (runStartup)
            {
                source.OnFrameworkInitializationCompleted();
            }

            IResourceDictionary resources = source.Resources;
            source.Resources = new ResourceDictionary();

            // A style can have only one owner. Thus remove the styles from the app before the new list gets them.
            List<IStyle> appStyles = [.. source.Styles];
            source.Styles.Clear();
            Styles styles = [.. appStyles];

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

    private static Type? FindAppType(Assembly? assembly)
    {
        if (assembly is null)
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
