namespace AxView.Hosting;

/// <summary>Finds the dotnet muxer on any OS.</summary>
internal static class DotnetHost
{
    public static string Path()
    {
        // The SDK sets DOTNET_HOST_PATH for tools and builds.
        string? fromSdk = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(fromSdk) && File.Exists(fromSdk))
        {
            return fromSdk;
        }

        // 'dotnet axview.dll' runs inside the muxer.
        string? self = Environment.ProcessPath;
        if (self is not null && System.IO.Path.GetFileNameWithoutExtension(self).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return self;
        }

        return "dotnet";
    }
}
