using System.Globalization;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Shared.Decals
{
    [Obsolete("Chunk entities use DecalChunksDecalSerializer instead")]
    [TypeSerializer]
    public sealed partial class DecalGridChunkCollectionTypeSerializer : ITypeSerializer<DecalGridChunkCollection, MappingDataNode>
    {
        private const int VersionUnspecified = 1;
        private const int VersionGroupedByData = 2;
        private const int VersionChunkLocalIds = 3;

        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            node.TryGetValue("version", out var versionNode);
            var version = ((ValueDataNode?) versionNode)?.AsInt() ?? VersionUnspecified;

            return version switch
            {
                VersionUnspecified => serializationManager.ValidateNode<Dictionary<Vector2i, Dictionary<uint, Decal>>>(node, context),
                VersionGroupedByData => new InconclusiveNode(node),
                VersionChunkLocalIds => new InconclusiveNode(node),
                _ => new ErrorNode(node, $"Unsupported decal chunk collection version {version}."),
            };
        }

        public DecalGridChunkCollection Read(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<DecalGridChunkCollection>? _ = default)
        {
            node.TryGetValue("version", out var versionNode);
            var version = ((ValueDataNode?) versionNode)?.AsInt() ?? VersionUnspecified;

            return version switch
            {
                VersionUnspecified => ReadVersionUnspecified(serializationManager, node, hookCtx, context),
                VersionGroupedByData => ReadVersionGroupedByData(serializationManager, node, hookCtx, context),
                // New hotness
                VersionChunkLocalIds => ReadVersionChunkLocalIds(serializationManager, node, hookCtx, context),
                // PRAY WE DON'T HIT IT
                _ => throw new InvalidOperationException($"Unsupported decal chunk collection version {version}."),
            };
        }

        private static DecalGridChunkCollection ReadVersionChunkLocalIds(
            ISerializationManager serializationManager,
            MappingDataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context)
        {
            var nodes = (SequenceDataNode) node["nodes"];
            var dictionary = new Dictionary<Vector2i, DecalChunk>(nodes.Count);
            ushort nextIndex = 0;

            foreach (var dNode in nodes)
            {
                var aNode = (MappingDataNode) dNode;
                var chunkOrigin = serializationManager.Read<Vector2i>(aNode["chunk"], hookCtx, context);
                var decals = serializationManager.Read<Dictionary<ushort, Decal>>(aNode["decals"], hookCtx, context, notNullableOverride: true);
                var chunk = dictionary.GetOrNew(chunkOrigin);

                foreach (var (uid, decal) in decals)
                {
                    var index = new DecalIndex(chunkOrigin, uid);
                    chunk.Decals[index.Id] = decal;

                    if (uid <= DecalChunkComponent.MaxServerDecalId)
                        nextIndex = Math.Max(nextIndex, (ushort) (uid + 1));
                }
            }

            return new DecalGridChunkCollection(dictionary) { NextDecalId = nextIndex };
        }

        private static DecalGridChunkCollection ReadVersionGroupedByData(
            ISerializationManager serializationManager,
            MappingDataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context)
        {
            var nodes = (SequenceDataNode) node["nodes"];
            var dictionary = new Dictionary<Vector2i, DecalChunk>();
            var usedIds = new Dictionary<Vector2i, HashSet<ushort>>();
            ushort nextIndex = 0;

            foreach (var dNode in nodes)
            {
                var aNode = (MappingDataNode) dNode;
                var data = serializationManager.Read<DecalData>(aNode["node"], hookCtx, context);
                var deckNodes = (MappingDataNode) aNode["decals"];

                foreach (var (decalUidNode, decalData) in deckNodes)
                {
                    var coords = serializationManager.Read<Vector2>(decalData, hookCtx, context);
                    // V2 stores decal data once and references it by grid-wide id. Rebuild chunk-local ids from coordinates.
                    var chunkOrigin = ChunkEntitySystem.GetChunkIndices(coords);
                    var index = RemapDecalIndex(uint.Parse(decalUidNode, CultureInfo.InvariantCulture), chunkOrigin, usedIds, ref nextIndex);
                    var decal = new Decal(coords, data.Id, data.Color, data.Angle, data.ZIndex, data.Cleanable);

                    AddDecal(dictionary, index, decal);
                }
            }

            return new DecalGridChunkCollection(dictionary) { NextDecalId = nextIndex };
        }

        private static DecalGridChunkCollection ReadVersionUnspecified(
            ISerializationManager serializationManager,
            MappingDataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context)
        {
            var oldDictionary = serializationManager.Read<Dictionary<Vector2i, Dictionary<uint, Decal>>>(node, hookCtx, context, notNullableOverride: true);
            var dictionary = new Dictionary<Vector2i, DecalChunk>(oldDictionary.Count);
            var usedIds = new Dictionary<Vector2i, HashSet<ushort>>();
            ushort nextIndex = 0;

            foreach (var (_, decals) in oldDictionary)
            {
                foreach (var (uid, decal) in decals)
                {
                    // V1 keys used 32x32 decal-grid buckets; coordinates are authoritative for current chunks.
                    var chunkOrigin = ChunkEntitySystem.GetChunkIndices(decal.Coordinates);
                    var index = RemapDecalIndex(uid, chunkOrigin, usedIds, ref nextIndex);
                    AddDecal(dictionary, index, decal);
                }
            }

            return new DecalGridChunkCollection(dictionary) { NextDecalId = nextIndex };
        }

        public DataNode Write(ISerializationManager serializationManager,
            DecalGridChunkCollection value, IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var allData = new MappingDataNode();
            // Want consistent chunk + decal ordering so diffs aren't mangled
            var nodes = new SequenceDataNode();

            var indices = new List<DecalIndex>();

            foreach (var (chunkIndices, chunk) in value.ChunkCollection)
            {
                foreach (var decalId in chunk.Decals.Keys)
                {
                    indices.Add(new DecalIndex(chunkIndices, decalId));
                }
            }

            indices.Sort(CompareDecalIndex);

            var currentChunk = new Vector2i(int.MinValue, int.MinValue);
            MappingDataNode? decalNodes = null;

            foreach (var index in indices)
            {
                if (index.Chunk != currentChunk)
                {
                    currentChunk = index.Chunk;
                    decalNodes = new MappingDataNode();

                    nodes.Add(new MappingDataNode
                    {
                        { "chunk", serializationManager.WriteValue(index.Chunk, alwaysWrite, context) },
                        { "decals", decalNodes },
                    });
                }

                // Preserve chunk-local ids so adding or removing one decal does not renumber the rest of the chunk.
                var decal = value.ChunkCollection[index.Chunk].Decals[index.Id];
                decalNodes!.Add(index.Id.ToString(CultureInfo.InvariantCulture), serializationManager.WriteValue(decal, alwaysWrite, context, notNullableOverride: true));
            }

            allData.Add("version", VersionChunkLocalIds.ToString(CultureInfo.InvariantCulture));
            allData.Add("nodes", nodes);

            return allData;
        }

        private static int CompareDecalIndex(DecalIndex x, DecalIndex y)
        {
            var chunkCompare = CompareVector2i(x.Chunk, y.Chunk);
            return chunkCompare != 0 ? chunkCompare : x.Id.CompareTo(y.Id);
        }

        private static int CompareVector2i(Vector2i x, Vector2i y)
        {
            var xCompare = x.X.CompareTo(y.X);
            return xCompare != 0 ? xCompare : x.Y.CompareTo(y.Y);
        }

        private static void AddDecal(Dictionary<Vector2i, DecalChunk> dictionary, DecalIndex index, Decal decal)
        {
            var chunk = dictionary.GetOrNew(index.Chunk);
            chunk.Decals[index.Id] = decal;
        }

        private static DecalIndex RemapDecalIndex(
            uint id,
            Vector2i chunk,
            Dictionary<Vector2i, HashSet<ushort>> usedIds,
            ref ushort nextIndex)
        {
            var used = usedIds.GetOrNew(chunk);

            if (id <= DecalChunkComponent.MaxServerDecalId && used.Add((ushort) id))
            {
                nextIndex = Math.Max(nextIndex, (ushort) (id + 1));
                return new DecalIndex(chunk, (ushort) id);
            }

            for (var i = 0; i <= DecalChunkComponent.MaxServerDecalId; i++)
            {
                var remapped = (ushort) i;

                if (!used.Add(remapped))
                    continue;

                nextIndex = Math.Max(nextIndex, (ushort) (remapped + 1));
                return new DecalIndex(chunk, remapped);
            }

            throw new InvalidOperationException("Too many decals to fit in the server decal ID range for a single chunk.");
        }

        [DataDefinition]
        private readonly partial struct DecalData : IEquatable<DecalData>, IComparable<DecalData>
        {
            [DataField("id")]
            public string Id { get; init; } = string.Empty;

            [DataField("color")]
            public Color? Color { get; init; }

            [DataField("angle")]
            public Angle Angle { get; init; } = Angle.Zero;

            [DataField("zIndex")]
            public int ZIndex { get; init; }

            [DataField("cleanable")]
            public bool Cleanable { get; init; }

            public DecalData(string id, Color? color, Angle angle, int zIndex, bool cleanable)
            {
                Id = id;
                Color = color;
                Angle = angle;
                ZIndex = zIndex;
                Cleanable = cleanable;
            }

            public DecalData(Decal decal)
            {
                Id = decal.Id;
                Color = decal.Color;
                Angle = decal.Angle;
                ZIndex = decal.ZIndex;
                Cleanable = decal.Cleanable;
            }

            public bool Equals(DecalData other)
            {
                return Id == other.Id &&
                       Nullable.Equals(Color, other.Color) &&
                       Angle.Equals(other.Angle) &&
                       ZIndex == other.ZIndex &&
                       Cleanable == other.Cleanable;
            }

            public override bool Equals(object? obj)
            {
                return obj is DecalData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id, Color, Angle, ZIndex, Cleanable);
            }

            public int CompareTo(DecalData other)
            {
                var idComparison = string.Compare(Id, other.Id, StringComparison.Ordinal);
                if (idComparison != 0)
                    return idComparison;

                var colorComparison = string.Compare(Color?.ToHex(), other.Color?.ToHex(), StringComparison.Ordinal);

                if (colorComparison != 0)
                    return colorComparison;

                var angleComparison = Angle.Theta.CompareTo(other.Angle.Theta);

                if (angleComparison != 0)
                    return angleComparison;

                var zIndexComparison = ZIndex.CompareTo(other.ZIndex);
                if (zIndexComparison != 0)
                    return zIndexComparison;

                return Cleanable.CompareTo(other.Cleanable);
            }
        }
    }
}
