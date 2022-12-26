using System.Linq;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;

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

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        var mapUid = _mapManager.GetMapEntityId(args.MapId);

        if (!_entManager.HasComponent<BiomeComponent>(mapUid))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entManager.TryGetComponent<BiomeComponent>(_mapManager.GetMapEntityId(args.MapId), out var biomeComponent) ||
            !_entManager.TryGetComponent<MapGridComponent>(_mapManager.GetMapEntityId(args.MapId), out var grid))
        {
            return;
        }

        var screenHandle = args.WorldHandle;
        var seed = new FastNoise(biomeComponent.Seed);
        var biome = _prototype.Index<BiomePrototype>(biomeComponent.Prototype);
        var tileSize = grid.TileSize;

        // Remove offset so we can floor.
        var flooredBL = args.WorldAABB.BottomLeft;

        // Floor to background size.
        flooredBL = (args.WorldAABB.BottomLeft / tileSize).Floored() * tileSize;
        var ceilingTR = (args.WorldAABB.TopRight / tileSize).Ceiled() * tileSize;
        var tileDimensions = new Vector2(tileSize, tileSize);

        // Setup for per-tile drawing
        var groups = biome.TileGroups.Select(o => _prototype.Index<BiomeTileGroupPrototype>(o)).ToList();
        var weightSum = groups.Sum(o => o.Weight);

        for (var x = flooredBL.X; x < ceilingTR.X; x ++)
        {
            for (var y = flooredBL.Y; y < ceilingTR.Y; y++)
            {
                var indices = new Vector2i((int) x, (int) y);

                // If there's a tile there then skip drawing.
                if (grid.TryGetTileRef(indices, out var tileRef) && !tileRef.Tile.IsEmpty)
                    continue;

                var tile = _biome.GetTile(indices, seed, groups, weightSum);
                var tex = _tileDefinitionManager.GetTexture(tile);
                screenHandle.DrawTextureRect(tex, Box2.FromDimensions(indices, tileDimensions));
            }
        }

        // Now work out edge tiles inside of a slightly higher bounds
        if (groups.Count > 1)
        {
            for (var x = flooredBL.X - 1; x < ceilingTR.X; x++)
            {
                for (var y = flooredBL.Y - 1; y < ceilingTR.Y; y++)
                {
                    var indices = new Vector2i((int) x, (int) y);

                    if (grid.TryGetTileRef(indices, out var tileRef) && !tileRef.Tile.IsEmpty)
                        continue;

                    var tileDef = _tileDefinitionManager[tileRef.Tile.TypeId];

                    if (tileDef.CardinalSprites.Count == 0 && tileDef.CornerSprites.Count == 0)
                        continue;

                    // Get what tiles border us to determine what sprites we need to draw.
                    for (var i = -1; i <= 1; i++)
                    {
                        for (var j = -1; j <= 1; j++)
                        {
                            if (i == 0 && j == 0)
                                continue;

                            var neighborIndices = new Vector2i(tileRef.GridIndices.X + i, tileRef.GridIndices.Y + j);
                            var neighborTile = grid.GetTileRef(neighborIndices);

                            // If it's the same tile then no edge to be drawn.
                            if (tileRef.Tile.TypeId == neighborTile.Tile.TypeId)
                                continue;

                            var direction = new Vector2i(i, j).AsDirection();
                            var intDirection = (int)direction;
                            var box = Box2.FromDimensions(neighborIndices, tileDimensions);
                            var variants = tileDef.CornerSprites.Count;
                            var variant = (tileRef.GridIndices.X + tileRef.GridIndices.Y * 4 + intDirection) % variants;

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
                                args.WorldHandle.DrawTextureRect(texture, box);
                            else
                                args.WorldHandle.DrawTextureRect(texture, new Box2Rotated(box, angle, box.Center));
                        }
                    }
                }
            }
        }
    }
}
