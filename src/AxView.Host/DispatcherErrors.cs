using System.Linq.Expressions;
using System.Reflection;

using Avalonia.Threading;

namespace AxView.Host;

/// <summary>Catches exceptions that escape the UI thread's dispatcher.</summary>
/// <remarks>
/// Dispatcher.UnhandledException is not in Avalonia 11.0, which the host compiles against.
/// Newer 11.x runtimes have it, so the host binds to it by reflection.
/// </remarks>
internal static class DispatcherErrors
{
    /// <summary>Calls the handler for each unhandled exception and marks the exception handled.</summary>
    /// <returns>An action that unsubscribes. Null when the runtime does not have the event.</returns>
    public static Action? Subscribe(Action<Exception> handler)
    {
        Dispatcher dispatcher = Dispatcher.UIThread;
        EventInfo? info = typeof(Dispatcher).GetEvent("UnhandledException");
        Type? handlerType = info?.EventHandlerType;
        MethodInfo? invoke = handlerType?.GetMethod("Invoke");
        if (info is null || handlerType is null || invoke?.GetParameters() is not [_, { ParameterType: Type argsType }])
        {
            return null;
        }

        PropertyInfo? exception = argsType.GetProperty("Exception");
        PropertyInfo? handled = argsType.GetProperty("Handled");
        if (exception is null || handled is null)
        {
            return null;
        }

        ParameterExpression sender = Expression.Parameter(typeof(object));
        ParameterExpression args = Expression.Parameter(argsType);
        Delegate callback = Expression.Lambda(
            handlerType,
            Expression.Block(
                Expression.Assign(Expression.Property(args, handled), Expression.Constant(true)),
                Expression.Invoke(Expression.Constant(handler), Expression.Property(args, exception))),
            sender,
            args).Compile();

        info.AddEventHandler(dispatcher, callback);
        return () => info.RemoveEventHandler(dispatcher, callback);
    }
}
