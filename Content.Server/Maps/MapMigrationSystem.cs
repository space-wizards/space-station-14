using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Server.Maps;

/// <summary>
/// Performs basic map migration operations by listening for engine <see cref="MapLoaderSystem"/> events.
/// </summary>
public sealed partial class MapMigrationSystem : EntitySystem
{
    [Dependency] private IResourceManager _resMan = default!;

    private const string MigrationDirectory = "/Migrations/";

    private readonly Dictionary<string, string?> _migrations = new();

    public override void Initialize()
    {
        base.Initialize();

        LoadMigrations();

#if DEBUG
        // Verify that all the entries map to valid entity prototypes.
        foreach (var newId in _migrations.Values.OfType<string>())
        {
            DebugTools.Assert(ProtoMan.HasIndex<EntityPrototype>(newId), $"{newId} is not an entity prototype.");
        }
#endif
    }

    private void LoadMigrations()
    {
        var paths = _resMan.ContentFindFiles(MigrationDirectory)
            .Where(path => path.Extension == "yml");

        foreach (var path in paths)
        {
            using var stream = _resMan.ContentFileRead(path);
            using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
            var document = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();

            if (document == null)
                continue;

            var mappings = (MappingDataNode) document.Root;

            foreach (var (oldId, node) in mappings)
            {
                if (node is not ValueDataNode valueNode)
                    continue;

                var newId = string.IsNullOrWhiteSpace(valueNode.Value) || valueNode.Value == "null"
                    ? null
                    : valueNode.Value;

                if (!_migrations.TryAdd(oldId, newId))
                    throw new InvalidDataException($"Duplicate map migration for '{oldId}' in '{path}'.");
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnBeforeReadEvent(BeforeEntityReadEvent ev)
    {
        foreach (var (oldId, newId) in _migrations)
        {
            if (newId == null)
                ev.DeletedPrototypes.Add(oldId);
            else
                ev.RenamedPrototypes.Add(oldId, newId);
        }
    }
}
