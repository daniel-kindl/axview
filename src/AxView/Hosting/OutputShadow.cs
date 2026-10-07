using System.Security.Cryptography;
using System.Text;

using AxView.Resolution;

namespace AxView.Hosting;

/// <summary>A copy of an app output that one host runs in.</summary>
/// <remarks>
/// <para>
/// The host locks the assemblies that it loads. A build of the app cannot replace a locked file.
/// Thus the host runs in a copy, and the build writes the real output.
/// </para>
/// <para>
/// Each app output has its shadow folders in the shadow root. The name of a folder is a hash of the output path, with an index after the first.
/// The copy keeps each folder. The next copy replaces only the files whose size or write time changed.
/// A lock file in the folder marks a folder in use. Another AxView session then uses the next folder.
/// </para>
/// </remarks>
internal sealed class OutputShadow : IDisposable
{
    private const string LockFileName = ".axview-lock";
    private const int MaxFolders = 16;
    private const int HashLength = 16;

    private readonly FileStream lockFile;

    private OutputShadow(AppOutput output, FileStream lockFile)
    {
        Output = output;
        this.lockFile = lockFile;
    }

    /// <summary>The copy of the app output.</summary>
    public AppOutput Output { get; }

    /// <summary>Gives the shadow root of the current user.</summary>
    public static string DefaultRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxView", "shadow");

    /// <summary>Copies the app output into a free shadow folder.</summary>
    /// <param name="source">The app output to copy.</param>
    /// <param name="shadowRoot">The folder that contains the shadow folders.</param>
    /// <exception cref="PreviewSetupException">All shadow folders of the output are in use, or the copy failed.</exception>
    public static OutputShadow Create(AppOutput source, string shadowRoot)
    {
        string baseName = HashOf(Path.GetFullPath(source.Directory));
        for (int index = 0; index < MaxFolders; index++)
        {
            string directory = Path.Combine(shadowRoot, index == 0 ? baseName : $"{baseName}-{index}");
            FileStream? lockFile = TryLock(directory);
            if (lockFile is null)
            {
                continue;
            }

            try
            {
                Sync(source.Directory, directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lockFile.Dispose();
                throw new PreviewSetupException($"Cannot copy the app output '{source.Directory}' to '{directory}': {ex.Message}");
            }

            AppOutput output = AppOutput.FromRuntimeConfig(Path.Combine(directory, Path.GetFileName(source.RuntimeConfig)));
            return new OutputShadow(output, lockFile);
        }

        throw new PreviewSetupException($"All {MaxFolders} shadow folders of '{source.Directory}' are in use.");
    }

    /// <summary>Frees the shadow folder for the next host.</summary>
    public void Dispose() => lockFile.Dispose();

    // Gives null when another session holds the lock.
    private static FileStream? TryLock(string directory)
    {
        _ = Directory.CreateDirectory(directory);
        try
        {
            return new FileStream(Path.Combine(directory, LockFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void Sync(string sourceDirectory, string targetDirectory)
    {
        foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string targetFile = Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, sourceFile));
            FileInfo source = new(sourceFile);
            FileInfo target = new(targetFile);
            bool isSame = target.Exists && (target.Length == source.Length) && (target.LastWriteTimeUtc == source.LastWriteTimeUtc);
            if (isSame)
            {
                continue;
            }

            _ = Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            _ = source.CopyTo(targetFile, overwrite: true);
            File.SetLastWriteTimeUtc(targetFile, source.LastWriteTimeUtc);
        }
    }

    private static string HashOf(string path)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(path.ToUpperInvariant()));
        return Convert.ToHexString(hash)[..HashLength];
    }
}
