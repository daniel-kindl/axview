using System.Linq.Expressions;
using System.Reflection;

using Avalonia;

namespace AxView.Host.Startup;

/// <summary>Applies well-known AppBuilder extensions when the target app ships them.</summary>
/// <remarks>
/// The host cannot reference these packages: the target app decides which ones exist, and in which version.
/// </remarks>
internal static class BuilderExtensions
{
    private static readonly (string Assembly, string Method)[] known =
    [
        ("Avalonia.Fonts.Inter", "WithInterFont"),
        ("ReactiveUI.Avalonia", "UseReactiveUI"),
        ("Avalonia.ReactiveUI", "UseReactiveUI"),
    ];

    public static AppBuilder ApplyAvailable(AppBuilder builder, List<string> applied)
    {
        foreach ((string assemblyName, string methodName) in known)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException)
            {
                continue;
            }

            MethodInfo? method = FindExtension(assembly, methodName);
            if (method is null)
            {
                continue;
            }

            try
            {
                object?[] arguments = [builder, .. method.GetParameters().Skip(1).Select(DefaultArgument)];
                builder = (AppBuilder)method.Invoke(null, arguments)!;
                applied.Add($"{assemblyName}.{methodName}");
            }
            catch (TargetInvocationException ex)
            {
                applied.Add($"{assemblyName}.{methodName} failed: {ex.InnerException?.Message}");
            }
        }

        return builder;
    }

    private static MethodInfo? FindExtension(Assembly assembly, string methodName)
    {
        Type[] types;
        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException)
        {
            return null;
        }

        return types
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.Name == methodName && !m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters() is [{ ParameterType: Type first }, ..] && first == typeof(AppBuilder))
            .Where(m => m.ReturnType == typeof(AppBuilder))
            .OrderBy(m => m.GetParameters().Length)
            .FirstOrDefault();
    }

    private static object? DefaultArgument(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

        Type type = parameter.ParameterType;
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            MethodInfo invoke = type.GetMethod("Invoke")!;
            if (invoke.ReturnType == typeof(void))
            {
                ParameterExpression[] parameters = [.. invoke.GetParameters().Select(p => Expression.Parameter(p.ParameterType))];
                return Expression.Lambda(type, Expression.Empty(), parameters).Compile();
            }
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
