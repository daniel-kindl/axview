using System.Reflection;

namespace AxView.Host.Rendering;

/// <summary>The place of an error in an AXAML file.</summary>
/// <param name="Line">The line number. The first line is 1.</param>
/// <param name="Column">The column number. The first column is 1.</param>
internal sealed record XamlLocation(int Line, int Column)
{
    private const string LineProperty = "LineNumber";
    private const string ColumnProperty = "LinePosition";

    /// <summary>Finds the line and the column of an exception or of one of its inner exceptions.</summary>
    /// <remarks>
    /// The XML parser and the XAML compilers put them in properties with the names <c>LineNumber</c> and <c>LinePosition</c>.
    /// The exception types are different in each Avalonia version. Thus the search reads the properties by name.
    /// </remarks>
    /// <returns>The place of the first exception with a line number above 0, or null.</returns>
    public static XamlLocation? Find(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            int line = ReadNumber(current, LineProperty);
            if (line > 0)
            {
                return new XamlLocation(line, ReadNumber(current, ColumnProperty));
            }
        }

        return null;
    }

    private static int ReadNumber(Exception ex, string propertyName)
    {
        PropertyInfo? property = ex.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(ex) is int number ? number : 0;
    }
}
