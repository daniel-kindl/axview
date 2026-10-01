using System.Reflection;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AxView.Host;

/// <summary>Turns an AXAML file of one project into a control that the preview window can show.</summary>
/// <remarks>
/// The renderer reads the file, adds the owning assembly to each CLR namespace, and loads the XAML in design mode.
/// Relative URIs in the XAML resolve against the avares URI of the file.
/// </remarks>
/// <param name="localAssembly">The assembly that the project builds.</param>
/// <param name="projectDirectory">The folder of the project.</param>
internal sealed class PreviewRenderer(Assembly localAssembly, string projectDirectory)
{
    private const int ReadAttempts = 5;
    private static readonly TimeSpan readRetryDelay = TimeSpan.FromMilliseconds(40);

    private readonly Assembly localAssembly = localAssembly;
    private readonly string projectDirectory = projectDirectory;
    private readonly NamespaceIndex namespaces = new(localAssembly);

    /// <summary>Loads the file and returns its root control.</summary>
    /// <param name="axamlPath">An AXAML file of the project.</param>
    /// <exception cref="NotSupportedException">The root is not a control, or it is a top-level other than a window.</exception>
    /// <remarks>The user's XAML and its code can throw any other exception.</remarks>
    public Control Render(string axamlPath)
    {
        string xaml = ReadWithRetry(axamlPath);
        Uri baseUri = AvaresUri.For(localAssembly.GetName().Name!, projectDirectory, axamlPath);
        string qualified = XmlnsQualifier.Qualify(xaml, namespaces.FindOwner);
        object root = AvaloniaRuntimeXamlLoader.Load(qualified, localAssembly, null, baseUri, designMode: true);
        return root switch
        {
            Window window => Detach(window),
            TopLevel => throw new NotSupportedException($"Cannot preview a {root.GetType().Name} root."),
            Control control => control,
            _ => throw new NotSupportedException(
                $"The root is a {root.GetType().Name}, not a control. Style and resource previews are not supported yet."),
        };
    }

    // A Window cannot be a child. Move its content, styles, and resources into a plain container.
    private static Border Detach(Window window)
    {
        object? content = window.Content;
        window.Content = null;

        Border container = new()
        {
            Background = window.Background,
            DataContext = window.DataContext,
            Width = window.Width,
            Height = window.Height,
            Child = content as Control ?? new ContentControl { Content = content },
        };

        List<Avalonia.Styling.IStyle> styles = [.. window.Styles];
        window.Styles.Clear();
        container.Styles.AddRange(styles);

        Avalonia.Controls.IResourceDictionary resources = window.Resources;
        window.Resources = new ResourceDictionary();
        container.Resources = resources;

        return container;
    }

    // An editor can hold the file open for a short time while it saves.
    private static string ReadWithRetry(string path)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException) when (attempt < ReadAttempts)
            {
                Thread.Sleep(readRetryDelay);
            }
        }
    }
}
