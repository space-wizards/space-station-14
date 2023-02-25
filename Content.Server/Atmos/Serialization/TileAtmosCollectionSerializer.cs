using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
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
        var tileChunks = new Dictionary<Vector2i, Dictionary<Vector2i, int>>();
        var chunkSize = 4;

        foreach (var (gridIndices, tile) in value)
        {
            if (tile.Air == null) continue;

            var chunkOrigin = SharedMapSystem.GetChunkIndices(gridIndices, chunkSize);
            var tiles = tileChunks.GetOrNew(chunkOrigin);
            var indices = SharedMapSystem.GetChunkRelative(gridIndices, chunkSize);

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

        if (uniqueMixes.Count == 0)
            uniqueMixes = null;
        if (tileChunks.Count == 0)
            tileChunks = null;

        return serializationManager.WriteValue(new TileAtmosData
        {
            ChunkSize = chunkSize,
            UniqueMixes = uniqueMixes,
            TilesUniqueMixes = null,
            TileChunksUniqueMixes = tileChunks
        }, alwaysWrite, context);
    }

    [DataDefinition]
    private struct TileAtmosData
    {
        [DataField("chunkSize")] public int ChunkSize;

        [DataField("uniqueMixes")] public List<GasMixture>? UniqueMixes;

        [DataField("tiles")] public Dictionary<Vector2i, int>? TilesUniqueMixes;

        [DataField("tileChunks")] public HashSet<Vector2i, Dictionary<Vector2i, int>>? TileChunksUniqueMixes;
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
