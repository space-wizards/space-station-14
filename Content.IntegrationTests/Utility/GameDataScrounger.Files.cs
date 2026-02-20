using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Utility;

public static partial class GameDataScrounger
{
    /// <summary>
    ///     Returns all files in a given content location that match a pattern.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="pattern"></param>
    /// <param name="recursive"></param>
    /// <returns></returns>
    public static string[] FilesInDirectory(string location, string pattern, bool recursive = true)
    {
        var path = GetContentPathOnDisk(location);

        return Directory.EnumerateFiles(path,
                pattern ?? "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .ToArray();
    }

    /// <summary>
    ///     Returns all files in a given content location that match a pattern, as their VFS paths.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="pattern"></param>
    /// <param name="recursive"></param>
    /// <returns></returns>
    public static ResPath[] FilesInDirectoryInVfs(string location, string pattern, bool recursive = true)
    {
        var path = GetContentPathOnDisk(location.TrimEnd('/'));
        var resBasePath = ContentResources();

        return Directory.EnumerateFiles(path,
                pattern ?? "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(x => new ResPath(x.Remove(0, resBasePath.Length)))
            .ToArray();
    }

    /// <summary>
    ///     Get the full directory path that the executable is located in.
    /// </summary>
    private static string GetExecutableDirectory()
    {
        // TODO: remove this shitty hack, either through making it less hardcoded into shared,
        //   or by making our file structure less spaghetti somehow.
        var assembly = typeof(IResourceManager).Assembly;
        var location = assembly.Location;
        if (location == string.Empty)
        {
            // See https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.location?view=net-5.0#remarks
            // This doesn't apply to us really because we don't do that kind of publishing, but whatever.
            throw new InvalidOperationException("Cannot find path of executable.");
        }

        return Path.GetDirectoryName(location)!;
    }

    /// <summary>
    ///     Turns a relative path from the executable directory into a full path.
    /// </summary>
    private static string ExecutableRelativeFile(string file)
    {
        return Path.GetFullPath(Path.Combine(GetExecutableDirectory(), file));
    }

    private static string FindContentRootDir()
    {
        return "../../";
    }

    private static string ContentResources()
    {
        return ExecutableRelativeFile($"{FindContentRootDir()}Resources");
    }

    public static string GetContentPathOnDisk(string path)
    {
        Assert.That(path, Does.StartWith("/"), "Path must be rooted.");

        return $"{ContentResources()}{path.ToString()}";
    }
}
