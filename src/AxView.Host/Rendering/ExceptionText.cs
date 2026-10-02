using System.Reflection;

namespace AxView.Host.Rendering;

/// <summary>Gives the text that the host shows for an exception of the user's XAML or code.</summary>
internal static class ExceptionText
{
    /// <summary>Gives the type name and the message. A reflection call shows its inner exception.</summary>
    /// <param name="ex">The exception to describe.</param>
    public static string Describe(Exception ex)
    {
        Exception shown = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
        return $"{shown.GetType().Name}: {shown.Message}";
    }
}
