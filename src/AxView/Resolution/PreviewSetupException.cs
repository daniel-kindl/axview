namespace AxView.Resolution;

/// <summary>The launcher cannot prepare a preview. The message tells the user what to fix.</summary>
internal sealed class PreviewSetupException(string message) : Exception(message);
