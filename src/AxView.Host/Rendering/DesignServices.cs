using System.Reflection;

namespace AxView.Host.Rendering;

/// <summary>Creates the ViewModels of a preview. Gives their constructor parameters from the design services of the target app.</summary>
/// <remarks>
/// The target app can supply design services with this convention. It needs no reference to AxView:
/// <code>
/// public static class AxViewDesign
/// {
///     public static IServiceProvider CreateServices() { ... }
/// }
/// </code>
/// The class can be in any namespace of the project assembly.
/// The factory can also initialize static services, for example a logger.
/// Register stub services in it, so that the preview makes no network calls.
/// </remarks>
/// <param name="provider">The design services, or null when the target app has no factory.</param>
internal sealed class DesignServices(IServiceProvider? provider)
{
    private const string FactoryTypeName = "AxViewDesign";
    private const string FactoryMethodName = "CreateServices";

    private readonly IServiceProvider? provider = provider;

    /// <summary>Gets an instance without design services. It creates only types with a parameterless constructor.</summary>
    public static DesignServices None { get; } = new(null);

    /// <summary>Calls the <c>AxViewDesign.CreateServices</c> factory of the project assembly.</summary>
    /// <param name="localAssembly">The assembly that the project builds.</param>
    /// <returns>The design services, or <see cref="None"/> when the assembly has no factory.</returns>
    /// <exception cref="TargetInvocationException">The factory threw an exception.</exception>
    public static DesignServices Load(Assembly localAssembly)
    {
        Type? factory = ProjectTypes.List(localAssembly).First().Types.FirstOrDefault(t => t.Name == FactoryTypeName);
        MethodInfo? method = factory?.GetMethod(FactoryMethodName, BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
        if (method is null || !typeof(IServiceProvider).IsAssignableFrom(method.ReturnType))
        {
            return None;
        }

        IServiceProvider? services = (IServiceProvider?)method.Invoke(null, null);
        return new DesignServices(services);
    }

    /// <summary>Tells if <see cref="Create"/> can create the type.</summary>
    /// <param name="type">A ViewModel type.</param>
    /// <returns>True when the type has a parameterless constructor, or a public constructor whose parameters all have a service.</returns>
    /// <remarks>The check gets the services from the provider. This can create a service.</remarks>
    public bool CanCreate(Type type) => FindArguments(type) is not null;

    /// <summary>Creates an instance of the type.</summary>
    /// <param name="type">A ViewModel type.</param>
    /// <returns>The new instance.</returns>
    /// <exception cref="InvalidOperationException">No constructor of the type has services for all its parameters.</exception>
    /// <remarks>The constructor is user code. It can throw any other exception.</remarks>
    public object Create(Type type)
    {
        (ConstructorInfo constructor, object[] arguments) = FindArguments(type)
            ?? throw new InvalidOperationException(
                $"Cannot create {type.Name}. Add a parameterless constructor, or give its parameters in AxViewDesign.CreateServices.");
        return constructor.Invoke(arguments);
    }

    /// <summary>Creates an instance of the type, or gives null when the type is null or <see cref="CanCreate"/> is false.</summary>
    /// <param name="type">A ViewModel type, or null.</param>
    public object? CreateOrNull(Type? type) => type is not null && CanCreate(type) ? Create(type) : null;

    // The parameterless constructor first. Then the public constructor with the most parameters that all have a service.
    private (ConstructorInfo Constructor, object[] Arguments)? FindArguments(Type type)
    {
        ConstructorInfo? parameterless = type.GetConstructor(Type.EmptyTypes);
        if (parameterless is not null)
        {
            return (parameterless, []);
        }

        if (provider is null || type.IsAbstract)
        {
            return null;
        }

        foreach (ConstructorInfo constructor in type.GetConstructors().OrderByDescending(c => c.GetParameters().Length))
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            object[] arguments = [.. parameters.Select(p => provider.GetService(p.ParameterType)).OfType<object>()];
            if (arguments.Length == parameters.Length)
            {
                return (constructor, arguments);
            }
        }

        return null;
    }
}
