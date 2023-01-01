using System.Linq;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeOverlay : Overlay
{
    /*
     * Similar to ParallaxOverlay except it renders fake tiles for planetmap purposes.
     */

    private readonly IClientTileDefinitionManager _tileDefinitionManager;
    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototype;
    private readonly IResourceCache _resource;
    private readonly BiomeSystem _biome;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    private readonly Dictionary<Type, HashSet<Vector2i>> _handled = new();

    public BiomeOverlay(
        IClientTileDefinitionManager tileDefinitionManager,
        IEntityManager entManager,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        IResourceCache resource,
        BiomeSystem biome)
    {
        _tileDefinitionManager = tileDefinitionManager;
        _entManager = entManager;
        _mapManager = mapManager;
        _prototype = protoManager;
        _resource = resource;
        _biome = biome;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var mapUid = _mapManager.GetMapEntityId(args.MapId);

        if (!_entManager.TryGetComponent<BiomeComponent>(mapUid, out var biome) ||
            !_prototype.TryIndex<BiomePrototype>(biome.BiomePrototype, out var prototype))
        {
            return;
        }

        var worldHandle = args.WorldHandle;
        var seed = new FastNoise(biome.Seed);
        var tileSize = 1;

        if (_entManager.TryGetComponent<MapGridComponent>(mapUid, out var grid))
        {
            tileSize = grid.TileSize;
        }

        var tileDimensions = new Vector2(tileSize, tileSize);

        // Remove offset so we can floor.
        var flooredBL = args.WorldAABB.BottomLeft;

        // Floor to background size.
        flooredBL = (args.WorldAABB.BottomLeft / tileSize).Floored() * tileSize;
        var ceilingTR = (args.WorldAABB.TopRight / tileSize).Ceiled() * tileSize;

        // Setup for per-tile drawing

        for (var i = prototype.Layers.Count - 1; i >= 0; i--)
        {
            var layer = prototype.Layers[i];
            var hands = _handled.GetOrNew(layer.GetType());

            switch (layer)
            {
                case BiomeTileLayer tileLayer:
                    DrawTileLayer(worldHandle, tileDimensions, tileLayer, flooredBL, ceilingTR, grid, seed, hands);
                    break;
                case BiomeDecalLayer decalLayer:
                    DrawDecalLayer(prototype, worldHandle, tileDimensions, decalLayer, flooredBL, ceilingTR, grid, seed, hands);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        // Render tile edges after all previous tiles done
        DrawTileEdges(worldHandle, prototype, flooredBL, ceilingTR, grid, seed);

        foreach (var handled in _handled.Values)
        {
            handled.Clear();
        }
    }

    private void DrawTileLayer(DrawingHandleWorld screenHandle,
        Vector2 tileSize,
        BiomeTileLayer tileLayer,
        Vector2 flooredBL,
        Vector2 ceilingTR,
        MapGridComponent? grid,
        FastNoise seed,
        HashSet<Vector2i> handledTiles)
    {
        seed.SetFrequency(tileLayer.Frequency);
        var groups = tileLayer.Tiles.Select(o => _prototype.Index<ContentTileDefinition>(o)).ToList();

        for (var x = flooredBL.X; x < ceilingTR.X; x++)
        {
            for (var y = flooredBL.Y; y < ceilingTR.Y; y++)
            {
                var indices = new Vector2i((int) x, (int) y);

                // If there's a tile there then skip drawing.
                if (grid?.TryGetTileRef(indices, out var tileRef) == true && !tileRef.Tile.IsEmpty || handledTiles.Contains(indices))
                    continue;

                if (!_biome.TryGetTile(indices, seed, tileLayer.Threshold, groups, out var tile))
                    continue;

                handledTiles.Add(indices);

                var tex = _tileDefinitionManager.GetTexture(tile.Value);
                screenHandle.DrawTextureRect(tex, Box2.FromDimensions(indices, tileSize));
            }
        }
    }

    private void DrawTileEdges(DrawingHandleWorld screenHandle, BiomePrototype prototype, Vector2 flooredBL, Vector2 ceilingTR, MapGridComponent? grid, FastNoise seed)
    {
        for (var x = flooredBL.X; x < ceilingTR.X; x++)
        {
            for (var y = flooredBL.Y; y < ceilingTR.Y; y++)
            {
                var indices = new Vector2i((int) x, (int) y);

                if (!_biome.TryGetBiomeTile(indices, prototype, seed, grid, out var tile))
                    continue;

                var tileDef = _tileDefinitionManager[tile.Value.TypeId];

                if (tileDef.CardinalSprites.Count == 0 && tileDef.CornerSprites.Count == 0)
                    continue;

                var tileDimensions = new Vector2(1f, 1f);

                // Get what tiles border us to determine what sprites we need to draw.
                for (var i = -1; i <= 1; i++)
                {
                    for (var j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        var neighborIndices = new Vector2i(indices.X + i, indices.Y + j);

                        // If it's the same tile then no edge to be drawn.
                        if (!_biome.TryGetBiomeTile(neighborIndices, prototype, seed, grid, out var neighborTile) ||
                            neighborTile.Value.TypeId.Equals(tile.Value.TypeId))
                            continue;

                        var direction = new Vector2i(i, j).AsDirection();
                        var intDirection = (int) direction;
                        var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                        var variants = tileDef.CornerSprites.Count;
                        var variant = (neighborIndices.X + neighborIndices.Y * 4 + intDirection) % variants;

                        Angle angle = Angle.Zero;
                        Texture? texture = null;

                        switch (direction)
                        {
                            // Corner sprites
                            case Direction.SouthEast:
                                if (tileDef.CornerSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CornerSprites[variant])
                                        .Texture;
                                }

                                break;
                            case Direction.NorthEast:
                                if (tileDef.CornerSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CornerSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI / 2f);
                                }

                                break;
                            case Direction.NorthWest:
                                if (tileDef.CornerSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CornerSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI);
                                }

                                break;
                            case Direction.SouthWest:
                                if (tileDef.CornerSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CornerSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI * 1.5f);
                                }

                                break;
                            // Edge sprites
                            case Direction.South:
                                if (tileDef.CardinalSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CardinalSprites[variant])
                                        .Texture;
                                }

                                break;
                            case Direction.East:
                                if (tileDef.CardinalSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CardinalSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI / 2f);
                                }

                                break;
                            case Direction.North:
                                if (tileDef.CardinalSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CardinalSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI);
                                }

                                break;
                            case Direction.West:
                                if (tileDef.CardinalSprites.Count > 0)
                                {
                                    texture = _resource.GetResource<TextureResource>(tileDef.CardinalSprites[variant])
                                        .Texture;

                                    angle = new Angle(MathF.PI * 1.5f);
                                }

                                break;
                        }

                        if (texture == null)
                            continue;

                        if (angle == Angle.Zero)
                            screenHandle.DrawTextureRect(texture, box);
                        else
                            screenHandle.DrawTextureRect(texture, new Box2Rotated(box, angle, box.Center));
                    }
                }
            }
        }
    }

    private void DrawDecalLayer(
        BiomePrototype prototype,
        DrawingHandleWorld screenHandle,
        Vector2 tileSize,
        BiomeDecalLayer decalLayer,
        Vector2 flooredBL,
        Vector2 ceilingTR,
        MapGridComponent? grid,
        FastNoise seed,
        HashSet<Vector2i> handled)
    {
        seed.SetFrequency(decalLayer.Frequency);

        for (var x = flooredBL.X - 1f; x < ceilingTR.X; x++)
        {
            for (var y = flooredBL.Y - 1f; y < ceilingTR.Y; y++)
            {
                var indices = new Vector2i((int) x, (int) y);

                // If there's a tile there then skip drawing.
                if (handled.Contains(indices))
                    continue;

                if (!_biome.TryGetBiomeTile(indices, prototype, seed, grid, out var indexTile) ||
                    !decalLayer.AllowedTiles.Contains(_tileDefinitionManager[indexTile.Value.TypeId].ID))
                {
                    continue;
                }

                var drawn = false;
                seed.SetSeed(seed.GetSeed() + decalLayer.SeedOffset);

                for (var i = 0; i < decalLayer.Divisions; i++)
                {
                    for (var j = 0; j < decalLayer.Divisions; j++)
                    {
                        var index = new Vector2(x + i * 1f / decalLayer.Divisions, y + j * 1f / decalLayer.Divisions);

                        if (!_biome.TryGetDecal(index, seed, decalLayer.Threshold, decalLayer.Decals, out var tex))
                            continue;

                        drawn = true;
                        screenHandle.DrawTextureRect(tex, Box2.FromDimensions(index, tileSize));
                    }
                }

                if (drawn)
                    handled.Add(indices);

                seed.SetSeed(seed.GetSeed() - decalLayer.SeedOffset);
            }
        }
    }
}
