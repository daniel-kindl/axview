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
/// A selected ViewModel type replaces the DataContext from the XAML.
/// The design services create the ViewModels.
/// A <c>Styles</c>, <c>Style</c>, or <c>ResourceDictionary</c> root shows its <c>Design.PreviewWith</c> control.
/// </remarks>
/// <param name="localAssembly">The assembly that the project builds.</param>
/// <param name="projectDirectory">The folder of the project.</param>
/// <param name="services">The design services that create the ViewModels.</param>
internal sealed class PreviewRenderer(Assembly localAssembly, string projectDirectory, DesignServices services)
{
    private const int ReadAttempts = 5;
    private static readonly TimeSpan readRetryDelay = TimeSpan.FromMilliseconds(40);

    private readonly Assembly localAssembly = localAssembly;
    private readonly string projectDirectory = projectDirectory;
    private readonly DesignServices services = services;
    private readonly NamespaceIndex namespaces = new(localAssembly);

    /// <summary>Gets the name of the assembly that the project builds.</summary>
    public string AssemblyName { get; } = localAssembly.GetName().Name!;

    /// <summary>Gets the folder of the project.</summary>
    public string ProjectDirectory { get; } = projectDirectory;

    /// <summary>Loads the file and returns its root control and the kind of its root.</summary>
    /// <param name="axamlPath">An AXAML file of the project.</param>
    /// <param name="viewModelType">
    /// The type of the root DataContext, or null to use the DataContext from the XAML. The design services must create it.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// The root is a top-level other than a window, a style or resource root without <c>Design.PreviewWith</c>, or another non-control.
    /// </exception>
    /// <remarks>The user's XAML and its code can throw any other exception.</remarks>
    public RenderedView Render(string axamlPath, Type? viewModelType = null)
    {
        string xaml = ReadWithRetry(axamlPath);
        object root = Load(axamlPath, xaml);
        if (root is Control loaded)
        {
            DesignDataContext.Apply(loaded, xaml, FindType, services);
            if (viewModelType is not null)
            {
                loaded.DataContext = services.Create(viewModelType);
            }
        }

        return root switch
        {
            Window window => new RenderedView(Detach(window), IsStylePreview: false),
            TopLevel => throw new NotSupportedException($"Cannot preview a {root.GetType().Name} root."),
            Control control => new RenderedView(control, IsStylePreview: false),
            Styles styles => new RenderedView(WithStyle(styles), IsStylePreview: true),
            Style style => new RenderedView(WithStyle(style), IsStylePreview: true),
            ResourceDictionary resources => new RenderedView(WithResources(resources), IsStylePreview: true),
            _ => throw new NotSupportedException($"The root is a {root.GetType().Name}, not a control."),
        };
    }

    /// <summary>Loads the XAML of a project file in design mode, and returns its root object.</summary>
    /// <param name="axamlPath">The file of the XAML. Relative URIs in the XAML resolve against its avares URI.</param>
    /// <param name="xaml">The content of the file.</param>
    /// <param name="rootInstance">The object that the XAML fills, or null to create a new root object.</param>
    /// <remarks>The user's XAML and its code can throw any exception.</remarks>
    public object Load(string axamlPath, string xaml, object? rootInstance = null)
    {
        Uri baseUri = AvaresUri.For(AssemblyName, projectDirectory, axamlPath);
        string withClass = CodeBehindClass.RemoveIfNotCreatable(xaml, localAssembly);
        string creatable = DesignDataContext.PrepareForLoader(withClass, FindType);
        string qualified = XmlnsQualifier.Qualify(creatable, namespaces.FindOwner);
        return AvaloniaRuntimeXamlLoader.Load(qualified, localAssembly, rootInstance, baseUri, designMode: true);
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
        Control preview = FindPreviewWith(resources) ?? throw NoPreviewWith(resources);
        preview.Resources.MergedDictionaries.Add(resources);
        return preview;
    }

    // Avalonia reads a dictionary with an indexer. It throws when another dictionary already has a preview control.
    private static Control? FindPreviewWith(ResourceDictionary resources)
    {
        try
        {
            return Design.GetPreviewWith(resources);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
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

    /// <summary>Reads a text file. Tries again when an editor holds the file open for a short time while it saves.</summary>
    /// <param name="path">The file to read.</param>
    /// <exception cref="IOException">The file does not exist, or stays locked.</exception>
    public static string ReadWithRetry(string path)
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
