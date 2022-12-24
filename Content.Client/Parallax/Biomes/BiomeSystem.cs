using System.Linq;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IClientTileDefinitionManager _tileDefManager = default!;

    public const int ChunkSize = 8;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new BiomeOverlay(EntityManager, _mapManager, _protoManager, this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<BiomeOverlay>();
    }

    public Tile GetTile(Vector2i indices, BiomePrototype prototype, int seed)
    {
        return Tile.Empty;
    }

    private float GetValue(Vector2i indices, int seed)
    {
        var chunkOrigin = SharedMapSystem.GetChunkIndices(indices, ChunkSize);
        var chunkValue = (OpenSimplex2.Noise2(seed, chunkOrigin.X, chunkOrigin.Y) + 1f) / 2f;
        var tileValue = (OpenSimplex2.Noise2(seed, indices.X, indices.Y) + 1f) / 2f;
        return (chunkValue / 2f + tileValue) / 1.5f;
    }

    private BiomeTileGroupPrototype GetGroup(List<BiomeTileGroupPrototype> groups, float value, float weight)
    {
        DebugTools.Assert(groups.Count > 0);

        if (groups.Count == 1)
            return groups[0];

        value *= weight;

        foreach (var group in groups)
        {
            value -= group.Weight;

            if (value <= 0f)
            {
                return group;
            }
        }

        throw new InvalidOperationException();
    }

    public Texture GetTexture(Vector2i indices, int seed, List<BiomeTileGroupPrototype> groups, float weightSum)
    {
        var value = GetValue(indices, seed);
        var group = GetGroup(groups, value, weightSum);

        // If it's a single tile on its own we fall back to the default group
        if (group != groups[0])
        {
            var isValid = false;

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0 ||
                        x != 0 && y != 0)
                    {
                        continue;
                    }

                    var neighborIndices = new Vector2i(indices.X + x, indices.Y + y);
                    var neighborValue = GetValue(neighborIndices, seed);
                    var neighborGroup = GetGroup(groups, neighborValue, weightSum);

                    if (neighborGroup == group)
                    {
                        isValid = true;
                        break;
                    }
                }

                if (isValid)
                    break;
            }

            if (!isValid)
            {
                group = groups[0];
            }
        }

        var sprite = _protoManager.Index<ContentTileDefinition>(group.Tile).Sprite;

        if (sprite == null)
            return Texture.Transparent;

        byte variant = 0;
        return _tileDefManager.GetTexture(new Tile(_protoManager.Index<ContentTileDefinition>(group.Tile).TileId, 0,
            variant));
    }
}
