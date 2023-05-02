using System.Globalization;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.Serialization;

public sealed class TileAtmosCollectionSerializer : ITypeSerializer<Dictionary<Vector2i, TileAtmosphere>, MappingDataNode>, ITypeCopier<Dictionary<Vector2i, TileAtmosphere>>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<TileAtmosData>(node, context);
    }

    public Dictionary<Vector2i, TileAtmosphere> Read(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Dictionary<Vector2i, TileAtmosphere>>? instanceProvider = null)
    {
        node.TryGetValue(new ValueDataNode("version"), out var versionNode);
        var version = ((ValueDataNode?) versionNode)?.AsInt() ?? 1;
        Dictionary<Vector2i, TileAtmosphere> tiles;

        // Backwards compatability
        if (version == 1)
        {
            var tile2 = node["tiles"];

            var mixies = serializationManager.Read<Dictionary<Vector2i, int>?>(tile2, hookCtx, context);
            var unique = serializationManager.Read<List<GasMixture>?>(node["uniqueMixes"], hookCtx, context);

            tiles = new Dictionary<Vector2i, TileAtmosphere>();

            if (unique != null && mixies != null)
            {
                foreach (var (indices, mix) in mixies)
                {
                    try
                    {
                        tiles.Add(indices, new TileAtmosphere(EntityUid.Invalid, indices,
                            unique[mix].Clone()));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Logger.Error(
                            $"Error during atmos serialization! Tile at {indices} points to an unique mix ({mix}) out of range!");
                    }
                }
            }
        }
        else
        {
            var dataNode = (MappingDataNode) node["data"];
            var tileNode = (MappingDataNode) dataNode["tiles"];
            var chunkSize = serializationManager.Read<int>(dataNode["chunkSize"], hookCtx, context);

            var unique = serializationManager.Read<List<GasMixture>?>(dataNode["uniqueMixes"], hookCtx, context);

            tiles = new Dictionary<Vector2i, TileAtmosphere>();

            if (unique != null)
            {
                foreach (var (chunkNode, valueNode) in tileNode)
                {
                    var chunkOrigin = serializationManager.Read<Vector2i>(chunkNode, hookCtx, context);
                    var chunk = serializationManager.Read<TileAtmosChunk>(valueNode, hookCtx, context);

                    foreach (var (mix, data) in chunk.Data)
                    {
                        for (var x = 0; x < chunkSize; x++)
                        {
                            for (var y = 0; y < chunkSize; y++)
                            {
                                var flag = data & (uint) (1 << (x + y * chunkSize));

                                if (flag == 0)
                                    continue;

                                var indices = new Vector2i(x + chunkOrigin.X * chunkSize,
                                    y + chunkOrigin.Y * chunkSize);

                                try
                                {
                                    tiles.Add(indices, new TileAtmosphere(EntityUid.Invalid, indices,
                                        unique[mix].Clone()));
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    Logger.Error(
                                        $"Error during atmos serialization! Tile at {indices} points to an unique mix ({mix}) out of range!");
                                }
                            }
                        }
                    }
                }
            }
        }

        return tiles;
    }

    public DataNode Write(ISerializationManager serializationManager, Dictionary<Vector2i, TileAtmosphere> value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        var uniqueMixes = new List<GasMixture>();
        var tileChunks = new Dictionary<Vector2i, TileAtmosChunk>();
        var chunkSize = 4;

        foreach (var (gridIndices, tile) in value)
        {
            if (tile.Air == null) continue;

            var mixIndex = uniqueMixes.IndexOf(tile.Air);

            if (mixIndex == -1)
            {
                mixIndex = uniqueMixes.Count;
                uniqueMixes.Add(tile.Air);
            }

            var chunkOrigin = SharedMapSystem.GetChunkIndices(gridIndices, chunkSize);
            var tileChunk = tileChunks.GetOrNew(chunkOrigin);
            var indices = SharedMapSystem.GetChunkRelative(gridIndices, chunkSize);

            var mixFlag = tileChunk.Data.GetOrNew(mixIndex);
            mixFlag |= (uint) 1 << (indices.X + indices.Y * chunkSize);
            tileChunk.Data[mixIndex] = mixFlag;
        }

        if (uniqueMixes.Count == 0)
            uniqueMixes = null;
        if (tileChunks.Count == 0)
            tileChunks = null;

        var map = new MappingDataNode
        {
            { "version", 2.ToString(CultureInfo.InvariantCulture) },
            {
                "data", serializationManager.WriteValue(new TileAtmosData
                {
                    ChunkSize = chunkSize,
                    UniqueMixes = uniqueMixes,
                    TilesUniqueMixes = tileChunks,
                }, alwaysWrite, context)
            }
        };

        return map;
    }

    [DataDefinition]
    private struct TileAtmosData
    {
        [DataField("chunkSize")] public int ChunkSize;

        [DataField("uniqueMixes")] public List<GasMixture>? UniqueMixes;

        [DataField("tiles")] public Dictionary<Vector2i, TileAtmosChunk>? TilesUniqueMixes;
    }

    [DataDefinition]
    private record struct TileAtmosChunk()
    {
        /// <summary>
        /// Key is unique mix and value is bitflag of the affected tiles.
        /// </summary>
        [IncludeDataField(customTypeSerializer: typeof(DictionarySerializer<int, uint>))]
        public readonly Dictionary<int, uint> Data = new();
    }

    public void CopyTo(ISerializationManager serializationManager, Dictionary<Vector2i, TileAtmosphere> source, ref Dictionary<Vector2i, TileAtmosphere> target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        target.Clear();
        foreach (var (key, val) in source)
        {
            target.Add(key,
                new TileAtmosphere(
                    val.GridIndex,
                    val.GridIndices,
                    val.Air?.Clone(),
                    val.Air?.Immutable ?? false,
                    val.Space));
        }
    }
}
