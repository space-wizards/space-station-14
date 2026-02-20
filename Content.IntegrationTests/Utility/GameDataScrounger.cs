#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Utility;

/// <summary>
///     A helper class for when you need prototype data particularly early, like for test lists.
/// </summary>
/// <remarks>
///     This does not include engine prototypes, nor anything generated at runtime, as it's made to be simple and fast
///     for usage during test framework startup where we cannot afford to initialize all of <see cref="ISerializationManager"/>.
///
///     Similarly, this does not respect ignored prototypes.
/// </remarks>
/// <example>
/// <code>
///     public static readonly string[]
/// </code>
/// </example>
public static class GameDataScrounger
{
    /// <summary>
    ///     Prototype type to ID index.
    /// </summary>
    private static Dictionary<string, List<string>>? _prototypeIndex = null;

    /// <summary>
    ///     Lock used to synchronize access to the prototype index.
    /// </summary>
    private static readonly Lock DataLock = new();

    /// <summary>
    ///     Gets all prototypes of the given type kind.
    /// </summary>
    public static string[] PrototypesOfKind<T>()
        where T : IPrototype
    {
        if (typeof(T).GetCustomAttribute<PrototypeAttribute>() is { Type: { } ty })
            return PrototypesOfKind(ty);

        return PrototypesOfKind(PrototypeUtility.CalculatePrototypeName(typeof(T).Name));
    }

    /// <summary>
    ///     Gets all prototypes of the given string kind.
    /// </summary>
    public static string[] PrototypesOfKind(string kind)
    {
        lock (DataLock)
        {
            if (_prototypeIndex is { } index)
            {
                return index[kind].ToArray();
            }
            else
            {
                Scrounge();

                return _prototypeIndex[kind].ToArray();
            }
        }
    }

    /// <summary>
    ///     Returns all files in a given content location that match a pattern.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="pattern"></param>
    /// <param name="recursive"></param>
    /// <returns></returns>
    public static string[] FilesInDirectory(string location, string? pattern, bool recursive = true)
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
    public static ResPath[] FilesInDirectoryInVfs(string location, string? pattern, bool recursive = true)
    {
        var path = GetContentPathOnDisk(location.TrimEnd('/'));

        return Directory.EnumerateFiles(path,
                pattern ?? "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(x => new ResPath(x.Remove(0, path.Length)))
            .ToArray();
    }


    [MemberNotNull(nameof(_prototypeIndex))]
    private static void Scrounge()
    {
        _prototypeIndex = new();
        var resDir = ContentResources();
        Assert.That(Directory.Exists($"{resDir}/Prototypes"));

        var ignoreList = GetIgnoredPrototypes(resDir);

        var explorationList = new List<string>() { $"{resDir}/Prototypes" };

        while (explorationList.Count > 0)
        {
            var dir = explorationList.Pop();

            if (ignoreList.Contains(dir))
                continue; // It's all abstract anyway.

            explorationList.AddRange(Directory.EnumerateDirectories(dir));

            foreach (var file in Directory.EnumerateFiles(dir, "*.yml"))
            {
                if (ignoreList.Contains(file))
                    continue; // It's all abstract anyway.

                foreach (var (kind, id) in IndexPrototypesIn(file))
                {
                    // alternate universe where .net has rust's Entry api.
                    if (!_prototypeIndex.TryGetValue(kind, out var list))
                    {
                        _prototypeIndex[kind] = new();
                        list = _prototypeIndex[kind];
                    }

                    list.Add(id);
                }
            }
        }
    }

    private static readonly YamlScalarNode IdNode = new("id");
    private static readonly YamlScalarNode TypeNode = new("type");

    private static IEnumerable<(string, string)> IndexPrototypesIn(string file)
    {
        var stream = new YamlStream();

        stream.Load(File.OpenText(file));

        foreach (var document in stream)
        {
            Assert.That(document.RootNode, Is.AssignableTo<YamlSequenceNode>());
            var node = (YamlSequenceNode)document.RootNode;

            foreach (var entry in node.Children)
            {
                Assert.That(entry, Is.AssignableTo<YamlScalarNode>());
                var entryMapping = (YamlMappingNode)entry;

                var id = entryMapping[IdNode];
                var type = entryMapping[TypeNode];
                if (entryMapping.TryGetNode("abstract", out YamlScalarNode? @abstract))
                {
                    // TODO: This technically will exclude prototypes that use the abstract field for their own stuff,
                    //       and not for parenting. However no such prototype exists in the game as of writing and solving
                    //       this is mildly nontrivial.

                    // We use exact equality to match what serialization does.
                    if (@abstract.Value == "true")
                        continue;
                }


                yield return (((YamlScalarNode)type).Value!, ((YamlScalarNode)id).Value!);
            }
        }
    }

    // Did you know there's no way to find the resources folder in the real filesystem
    // from content? Makes sense, but ough. So this is unfortunately copy-pasted from engine.
    // I don't think it's worth it to add an API for this to engine due to the security implications for sandboxed
    // content.

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

    // This is indeed, unfortunately, a replica of Content.Shared/Entry/EntryPoint.cs:129
    // That code relies on engine tools we can't use here, because we can't even spin up engine.
    private static HashSet<string> GetIgnoredPrototypes(string resDir)
    {
        var ignores = new HashSet<string>();
        var ignoredProtosPath = $"{resDir}/IgnoredPrototypes";

        if (!Directory.Exists(ignoredProtosPath))
            return ignores; // Nothing to do.

        foreach (var path in Directory.EnumerateFiles($"{resDir}/IgnoredPrototypes"))
        {
            var stream = new YamlStream();

            stream.Load(File.OpenText(path));

            foreach (var document in stream)
            {
                if (document.RootNode is not YamlSequenceNode seq)
                    throw new Exception($"The ignored prototypes file at {path} isn't a valid yaml sequence/list.");

                foreach (var entry in seq)
                {
                    if (entry is not YamlScalarNode { Value: {} value })
                        throw new Exception($"An entry in {path} is not a valid YAML scalar/string literal. Entry: {entry}");

                    ignores.Add(value);
                }
            }
        }

        return ignores;
    }
}
