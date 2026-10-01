using System.Reflection;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AxView.Host;

/// <summary>Turns AXAML text into a control that the preview window can show.</summary>
internal static class PreviewRenderer
{
    public static Control Render(string xaml, Assembly localAssembly, NamespaceIndex namespaces, Uri baseUri)
    {
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
    private static Control Detach(Window window)
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
}
