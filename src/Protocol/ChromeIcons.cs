using Avalonia.Media;

namespace AxView.Protocol;

/// <summary>The vector icons of the AxView chrome. Each icon is a filled path in a square view box.</summary>
/// <remarks>
/// The path data uses the even-odd fill rule. Thus an inner figure cuts a hole, for example the inside of a ring.
/// The code keeps the icons as path data. Thus the host needs no icon package in the target runtime.
/// </remarks>
/// <example>
/// <code>
/// PathIcon icon = new() { Data = ChromeIcons.CreateGeometry(ChromeIcon.Open) };
/// </code>
/// </example>
internal static class ChromeIcons
{
    private const string OpenData = "M1,3 L6,3 L7.5,4.5 L15,4.5 L15,13 L1,13 Z M2.5,6 L13.5,6 L13.5,11.5 L2.5,11.5 Z";

    private const string AboutData =
        "M8,1 A7,7 0 1 1 8,15 A7,7 0 1 1 8,1 Z M8,2.5 A5.5,5.5 0 1 0 8,13.5 A5.5,5.5 0 1 0 8,2.5 Z "
        + "M7.25,4.5 L8.75,4.5 L8.75,6 L7.25,6 Z M7.25,7 L8.75,7 L8.75,11.5 L7.25,11.5 Z";

    private const string ThemeData = "M8,1 A7,7 0 1 1 8,15 A7,7 0 1 1 8,1 Z M8,2.5 A5.5,5.5 0 0 1 8,13.5 Z";

    private const string SizeData =
        "M1,1 L6,1 L6,2.5 L2.5,2.5 L2.5,6 L1,6 Z M15,1 L15,6 L13.5,6 L13.5,2.5 L10,2.5 L10,1 Z "
        + "M15,15 L10,15 L10,13.5 L13.5,13.5 L13.5,10 L15,10 Z M1,15 L1,10 L2.5,10 L2.5,13.5 L6,13.5 L6,15 Z";

    private const string CopyData =
        "M5,1 L14,1 L14,11 L5,11 Z M6.5,2.5 L6.5,9.5 L12.5,9.5 L12.5,2.5 Z M2,5 L3.5,5 L3.5,13.5 L10,13.5 L10,15 L2,15 Z";

    private const string CollapseData = "M2,4 L8,10 L14,4 L15,5 L8,12 L1,5 Z";

    private const string SearchData =
        "M6.5,1 A5.5,5.5 0 1 1 6.5,12 A5.5,5.5 0 1 1 6.5,1 Z M6.5,2.5 A4,4 0 1 0 6.5,10.5 A4,4 0 1 0 6.5,2.5 Z "
        + "M9.6,10.7 L10.7,9.6 L15,13.9 L13.9,15 Z";

    private const string ViewData = "M1,2 L15,2 L15,14 L1,14 Z M2.5,5.5 L2.5,12.5 L13.5,12.5 L13.5,5.5 Z";

    private const string StylesData =
        "M8,1 A7,7 0 1 0 8,15 C9,15 9.5,14.2 9,13.4 C8.5,12.6 9,11.5 10,11.5 L12,11.5 C13.7,11.5 15,10.2 15,8.5 "
        + "C15,4.4 11.9,1 8,1 Z M3.3,7 A1.2,1.2 0 1 0 5.7,7 A1.2,1.2 0 1 0 3.3,7 Z "
        + "M5.8,4.3 A1.2,1.2 0 1 0 8.2,4.3 A1.2,1.2 0 1 0 5.8,4.3 Z M9.3,4.8 A1.2,1.2 0 1 0 11.7,4.8 A1.2,1.2 0 1 0 9.3,4.8 Z";

    private const string ResourcesData = "M2,2 L14,2 L14,5 L2,5 Z M2,6.5 L14,6.5 L14,9.5 L2,9.5 Z M2,11 L14,11 L14,14 L2,14 Z";

    private const string GalleryData =
        "M1.5,1.5 L7,1.5 L7,7 L1.5,7 Z M9,1.5 L14.5,1.5 L14.5,7 L9,7 Z "
        + "M1.5,9 L7,9 L7,14.5 L1.5,14.5 Z M9,9 L14.5,9 L14.5,14.5 L9,14.5 Z";

    private const string WarningData =
        "M8,1 L15.5,14.5 L0.5,14.5 Z M7.25,5.5 L8.75,5.5 L8.75,10 L7.25,10 Z M7.25,11 L8.75,11 L8.75,12.5 L7.25,12.5 Z";

    private const string ErrorData =
        "M8,1 A7,7 0 1 1 8,15 A7,7 0 1 1 8,1 Z "
        + "M5,4 L8,7 L11,4 L12,5 L9,8 L12,11 L11,12 L8,9 L5,12 L4,11 L7,8 L4,5 Z";

    /// <summary>The width and the height of the view box of each icon.</summary>
    public const double Size = 16;

    /// <summary>Creates the geometry of an icon. Each call gives a new geometry. Thus a change to one icon has no effect on other icons.</summary>
    /// <param name="icon">The icon.</param>
    /// <returns>A new geometry in the view box from 0 to <see cref="Size"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The icon is not a defined value.</exception>
    public static Geometry CreateGeometry(ChromeIcon icon)
    {
        string data = icon switch
        {
            ChromeIcon.Open => OpenData,
            ChromeIcon.About => AboutData,
            ChromeIcon.Theme => ThemeData,
            ChromeIcon.Size => SizeData,
            ChromeIcon.Copy => CopyData,
            ChromeIcon.Collapse => CollapseData,
            ChromeIcon.Search => SearchData,
            ChromeIcon.View => ViewData,
            ChromeIcon.Styles => StylesData,
            ChromeIcon.Resources => ResourcesData,
            ChromeIcon.Gallery => GalleryData,
            ChromeIcon.Warning => WarningData,
            ChromeIcon.Error => ErrorData,
            _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, "The icon is not a defined value."),
        };
        return Geometry.Parse(data);
    }
}
