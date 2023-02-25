using System.Linq;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

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
        var data = serializationManager.Read<TileAtmosData>(node, hookCtx, context);
        var tiles = new Dictionary<Vector2i, TileAtmosphere>();
        if (data.TilesUniqueMixes != null)
        {
            foreach (var (indices, mix) in data.TilesUniqueMixes)
            {
                try
                {
                    tiles.Add(indices, new TileAtmosphere(EntityUid.Invalid, indices,
                        data.UniqueMixes![mix].Clone()));
                }
                catch (ArgumentOutOfRangeException)
                {
                    Logger.Error(
                        $"Error during atmos serialization! Tile at {indices} points to an unique mix ({mix}) out of range!");
                }
            }
        }

        return tiles;
    }

    public DataNode Write(ISerializationManager serializationManager, Dictionary<Vector2i, TileAtmosphere> value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        var uniqueMixes = new List<GasMixture>();
        var uniqueMixHash = new Dictionary<GasMixture, int>();
        var tiles = new Dictionary<Vector2i, int>();

        foreach (var (indices, tile) in value)
        {
            if (tile.Air == null) continue;

            if (uniqueMixHash.TryGetValue(tile.Air, out var index))
            {
                tiles[indices] = index;
                continue;
            }

            uniqueMixes.Add(tile.Air);
            var newIndex = uniqueMixes.Count - 1;
            uniqueMixHash[tile.Air] = newIndex;
            tiles[indices] = newIndex;
        }

        var tileChunks = new Dictionary<Vector2i, int>();
        var checkedChunks = new HashSet<Vector2i>();
        var chunkSize = 2;

        // If there's ChunkSize x ChunkSize area of common tiles then compress it
        foreach (var chunk in tiles.Keys)
        {
            var origin = SharedMapSystem.GetChunkIndices(chunk, chunkSize);

            if (!checkedChunks.Add(origin))
                continue;

            var matches = true;
            int chunkData = -1;

            for (var x = 0; x < chunkSize; x++)
            {
                for (var y = 0; y < chunkSize; y++)
                {
                    var indices = new Vector2i(x + chunkSize * origin.X, y + chunkSize * origin.Y);

                    if (!tiles.TryGetValue(indices, out var data))
                    {
                        matches = false;
                        break;
                    }

                    if (chunkData != -1 && chunkData != data)
                    {
                        matches = false;
                        break;
                    }

                    chunkData = data;
                }

                if (!matches)
                {
                    break;
                }
            }

            if (!matches || chunkData == -1)
                continue;

            tileChunks.Add(origin, chunkData);
        }

        // Remove tile data
        foreach (var origin in tileChunks.Keys)
        {
            var chunkOrigin = origin * chunkSize;

            for (var x = 0; x < chunkSize; x++)
            {
                for (var y = 0; y < chunkSize; y++)
                {
                    var indices = new Vector2i(x + chunkOrigin.X, y + chunkOrigin.Y);
                    tiles.Remove(indices);
                }
            }
        }

        if (tileChunks.Count == 0)
            tileChunks = null;
        if (uniqueMixes.Count == 0)
            uniqueMixes = null;
        if (tiles.Count == 0)
            tiles = null;

        return serializationManager.WriteValue(new TileAtmosData
        {
            ChunkSize = chunkSize,
            UniqueMixes = uniqueMixes,
            TilesUniqueMixes = tiles,
            TileChunksUniqueMixes = tileChunks,
        }, alwaysWrite, context);
    }

    [DataDefinition]
    private struct TileAtmosData
    {
        [DataField("chunkSize")] public int ChunkSize;

        [DataField("uniqueMixes")] public List<GasMixture>? UniqueMixes;

        [DataField("tiles")] public Dictionary<Vector2i, int>? TilesUniqueMixes;

        /// <summary>
        /// Stores bulk tile data, saves (ChunkSize x ChunkSize) - 1 lines.
        /// </summary>
        [DataField("tileChunks")] public Dictionary<Vector2i, int>? TileChunksUniqueMixes;
    }

    public void CopyTo(ISerializationManager serializationManager, Dictionary<Vector2i, TileAtmosphere> source, ref Dictionary<Vector2i, TileAtmosphere> target, SerializationHookContext hookCtx,
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
