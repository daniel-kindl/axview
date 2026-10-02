using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AxView.Host.Rendering;

/// <summary>Turns an AXAML file of one project into a control that the preview window can show.</summary>
/// <remarks>
/// The renderer reads the file, adds the owning assembly to each CLR namespace, and loads the XAML in design mode.
/// The loader creates the <c>x:Class</c> type, so its code-behind runs. If the loader cannot create it, the root element type is used.
/// Relative URIs in the XAML resolve against the avares URI of the file.
/// The root gets a DataContext from the XAML. See <see cref="DesignDataContext"/>.
/// A <c>Styles</c>, <c>Style</c>, or <c>ResourceDictionary</c> root shows its <c>Design.PreviewWith</c> control.
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
    /// <exception cref="NotSupportedException">
    /// The root is a top-level other than a window, a style or resource root without <c>Design.PreviewWith</c>, or another non-control.
    /// </exception>
    /// <remarks>The user's XAML and its code can throw any other exception.</remarks>
    public Control Render(string axamlPath)
    {
        string xaml = ReadWithRetry(axamlPath);
        Uri baseUri = AvaresUri.For(localAssembly.GetName().Name!, projectDirectory, axamlPath);
        string withClass = CodeBehindClass.RemoveIfNotCreatable(xaml, localAssembly);
        string creatable = DesignDataContext.PrepareForLoader(withClass, FindType);
        string qualified = XmlnsQualifier.Qualify(creatable, namespaces.FindOwner);
        object root = AvaloniaRuntimeXamlLoader.Load(qualified, localAssembly, null, baseUri, designMode: true);
        if (root is Control loaded)
        {
            DesignDataContext.Apply(loaded, xaml, FindType);
        }

        return root switch
        {
            Window window => Detach(window),
            TopLevel => throw new NotSupportedException($"Cannot preview a {root.GetType().Name} root."),
            Control control => control,
            Styles styles => WithStyle(styles),
            Style style => WithStyle(style),
            ResourceDictionary resources => WithResources(resources),
            _ => throw new NotSupportedException($"The root is a {root.GetType().Name}, not a control."),
        };
    }

    private static Control WithStyle<TStyle>(TStyle style)
        where TStyle : AvaloniaObject, IStyle
    {
        Control preview = Design.GetPreviewWith(style) ?? throw NoPreviewWith(style);
        preview.Styles.Add(style);
        return preview;
    }

    private static Control WithResources(ResourceDictionary resources)
    {
        Control preview = Design.GetPreviewWith(resources) ?? throw NoPreviewWith(resources);
        preview.Resources.MergedDictionaries.Add(resources);
        return preview;
    }

    private static NotSupportedException NoPreviewWith(object root) =>
        new($"Add a Design.PreviewWith control to the {root.GetType().Name} root to preview it.");

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

        List<IStyle> styles = [.. window.Styles];
        window.Styles.Clear();
        container.Styles.AddRange(styles);

        IResourceDictionary resources = window.Resources;
        window.Resources = new ResourceDictionary();
        container.Resources = resources;

        return container;
    }

    private Type? FindType(string clrNamespace, string name)
    {
        string fullName = $"{clrNamespace}.{name}";
        string? owner = namespaces.FindOwner(clrNamespace);
        return localAssembly.GetType(fullName) ?? (owner is null ? null : Assembly.Load(owner).GetType(fullName));
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
