using System.Diagnostics.CodeAnalysis;

using Avalonia.Markup.Xaml;

namespace AxView.Host.Rendering;

/// <summary>Creates an instance of a design type. <see cref="DesignDataContext"/> writes it in place of a <c>d:DesignInstance</c>.</summary>
/// <remarks>
/// The XAML loader has no <c>d:DesignInstance</c>. The type is public, because the loader creates it.
/// Example: <c>DataContext="{axview:DesignInstance {x:Type vm:MainViewModel}}"</c>.
/// </remarks>
/// <param name="type">A type with a parameterless constructor.</param>
[SuppressMessage("Maintainability", "CA1515", Justification = "The XAML loader creates the type. It must be public.")]
public sealed class DesignInstanceExtension(Type type) : MarkupExtension
{
    private readonly Type type = type;

    /// <summary>Creates the instance.</summary>
    /// <param name="serviceProvider">Not used.</param>
    /// <returns>A new instance of the type.</returns>
    /// <remarks>The constructor of the type is user code. It can throw any exception.</remarks>
    public override object ProvideValue(IServiceProvider serviceProvider) => Activator.CreateInstance(type)!;
}
