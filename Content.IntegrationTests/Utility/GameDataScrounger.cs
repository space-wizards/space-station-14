#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Utility;

/// <summary>
///     A helper class for when you need prototype or VFS data particularly early, like for test source lists.
/// </summary>
/// <remarks>
///     This does not include engine prototypes, nor anything generated at runtime, as it's made to be simple and fast
///     for usage during test framework startup where we cannot afford to initialize all of <see cref="ISerializationManager"/>.
/// </remarks>
/// <example>
/// <code>
///     public static readonly string[] Maps = GameDataScrounger.PrototypesOfKind&lt;GameMapPrototype&gt;();
/// </code>
/// </example>
public static partial class GameDataScrounger
{
    // YAML Linter, for Reasons, depends on the entirety of the test suite.
    // As such, scrounging erroring out due to bad YAML can make the linter fail spectacularly.
    // We do not want that, so the linter sets this, and we refuse to do any yaml-ing ourselves so the nicer set of
    // errors get to it.
    //
    // Also, this means obviously bad YAML causes the main test suite to exit early. This is probably a pro, honestly.
    public static bool NoScrounging = false;

    /// <summary>
    ///     Prototype type to ID index.
    /// </summary>
    private static Dictionary<string, List<string>>? _prototypeIndex = null;

    /// <summary>
    ///     Component type to prototype ID index.
    /// </summary>
    private static Dictionary<string, List<string>>? _entitiesWithComponentIndex = null;

    /// <summary>
    ///     Entity proto to metadata index.
    /// </summary>
    private static Dictionary<string, EntityMetadata>? _entitiesMetaIndex = null;

    private sealed class EntityMetadata
    {
        public required string Id;
        public required HashSet<string> Components;
        public required List<string> Parents;
        public required bool Abstract;
    }

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
        if (NoScrounging)
            return Array.Empty<string>();

        lock (DataLock)
        {
            Scrounge();

            return _prototypeIndex[kind].ToArray();
        }
    }

    public static string[] EntitiesWithComponent(string componentId)
    {
        if (NoScrounging)
            return Array.Empty<string>();

        lock (DataLock)
        {
            if (_entitiesWithComponentIndex is { } index)
            {
                return index[componentId].ToArray();
            }
            else
            {
                Scrounge();

                return _entitiesWithComponentIndex[componentId].ToArray();
            }
        }
    }

    [MemberNotNull(nameof(_prototypeIndex))]
    [MemberNotNull(nameof(_entitiesWithComponentIndex))]
    [MemberNotNull(nameof(_entitiesMetaIndex))]
    private static void Scrounge()
    {
        if (_prototypeIndex is not null && _entitiesWithComponentIndex is not null && _entitiesMetaIndex is not null)
            return;

        _prototypeIndex = new();
        _entitiesWithComponentIndex = new();
        _entitiesMetaIndex = new();

        if (NoScrounging)
            return;

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

        PushInheritanceAndIndex();
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
                Assert.That(entry, Is.AssignableTo<YamlMappingNode>());
                var entryMapping = (YamlMappingNode)entry;

                var id = entryMapping[IdNode];
                var type = entryMapping[TypeNode];
                var @abstract = false;
                if (entryMapping.TryGetNode("abstract", out YamlScalarNode? abstractNode))
                {
                    // TODO: This technically will exclude prototypes that use the abstract field for their own stuff,
                    //       and not for parenting. However no such prototype exists in the game as of writing and solving
                    //       this is mildly nontrivial.

                    // We use exact equality to match what serialization does.
                    if (abstractNode.Value == "true")
                        @abstract = true;
                }

                if (!@abstract)
                    yield return (((YamlScalarNode)type).Value!, ((YamlScalarNode)id).Value!);

                // If we're an entity prototype..
                if (type is not YamlScalarNode { Value: "entity" })
                    continue;

                // then do some metadata indexing that's feasible w/o serializationmanager.

                entryMapping.TryGetNode("components", out YamlSequenceNode? components);

                var parents = new List<string>();

                if (entryMapping.TryGetNode("parent", out var parentNode))
                {
                    switch (parentNode)
                    {
                        case YamlScalarNode scalar:
                        {
                            parents.Add(scalar.Value!);
                            break;
                        }
                        case YamlSequenceNode seq:
                        {
                            parents.AddRange(seq.Children.Select(x => x.AsString()));
                            break;
                        }
                    }
                }


                // Assemble metadata for this entity prototype w/o needing serializationmanager.
                var entity = new EntityMetadata()
                {
                    Abstract = @abstract,
                    Components = components?.Children.Select(x => x["type"].ToString()).ToHashSet() ?? new(),
                    Parents = parents,
                    Id = id.AsString(),
                };

                _entitiesMetaIndex![id.AsString()] = entity;
            }
        }
    }

    private static void PushInheritanceAndIndex()
    {
        var visitedEntities = new HashSet<string>();

        foreach (var entity in _entitiesMetaIndex!.Values)
        {
            VisitEntity(entity, visitedEntities);

            if (entity.Abstract)
                continue; // We don't index abstract entities here.

            foreach (var component in entity.Components)
            {
                if (!_entitiesWithComponentIndex!.TryGetValue(component, out var list))
                {
                    list = new();
                    _entitiesWithComponentIndex[component] = list;
                }

                list.Add(entity.Id);
            }
        }
    }

    private static void VisitEntity(EntityMetadata entity, HashSet<string> visitedEntities)
    {
        // Return if we've visited already.
        if (!visitedEntities.Add(entity.Id))
            return;

        foreach (var parent in entity.Parents)
        {
            var parentMeta = _entitiesMetaIndex![parent];
            VisitEntity(parentMeta, visitedEntities);

            entity.Components.UnionWith(parentMeta.Components);
        }
    }

    // Did you know there's no way to find the resources folder in the real filesystem
    // from content? Makes sense, but ough. So this is unfortunately copy-pasted from engine.
    // I don't think it's worth it to add an API for this to engine due to the security implications for sandboxed
    // content.

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
