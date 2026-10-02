using AxView.Host.Rendering;

namespace AxView.Host.Startup;

/// <summary>A live copy of the app styles and the include reload of the copy.</summary>
/// <param name="Styles">The copy that an owner holds.</param>
/// <param name="Includes">The include reload of the copy.</param>
internal sealed record LiveCopy(TargetStyles Styles, AppIncludes Includes);
