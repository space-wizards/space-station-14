using System.Linq;
using Content.Shared.Parallax.Biomes;
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

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var screenHandle = args.WorldHandle;
        var seed = new FastNoise(0);
        var biome = _prototype.Index<BiomePrototype>("Grasslands");
        var tileSize = 1;

        if (_entManager.TryGetComponent<MapGridComponent>(_mapManager.GetMapEntityId(args.MapId), out var grid))
        {
            tileSize = grid.TileSize;
        }

        // Remove offset so we can floor.
        var flooredBL = args.WorldAABB.BottomLeft;

        // Floor to background size.
        flooredBL = (args.WorldAABB.BottomLeft / tileSize).Floored() * tileSize;
        var ceilingTR = (args.WorldAABB.TopRight / tileSize).Ceiled() * tileSize;

        // Setup for per-tile drawing
        var groups = biome.TileGroups.Select(o => _prototype.Index<BiomeTileGroupPrototype>(o)).ToList();
        var weightSum = groups.Sum(o => o.Weight);

        // TODO: Should have some internal caching to the biome stuff.

        for (var x = flooredBL.X; x < ceilingTR.X; x ++)
        {
            for (var y = flooredBL.Y; y < ceilingTR.Y; y++)
            {
                var indices = new Vector2i((int) x, (int) y);

                // If there's a tile there then skip drawing.
                if (grid?.TryGetTileRef(indices, out _) == true)
                    continue;

                var tile = _biome.GetTile(indices, seed, groups, weightSum);
                var tex = _tileDefinitionManager.GetTexture(tile);
                screenHandle.DrawTextureRect(tex, Box2.FromDimensions(indices, (tileSize, tileSize)));
            }
        }

        // Now work out edge tiles inside of a slightly higher bounds
        if (groups.Count > 1)
        {
            // Store the tile's neighbors and work out what edge sprites we need to draw on ourselves
            // TODO: Pooling
            var neighborDirections = new Dictionary<BiomeTileGroupPrototype, List<Direction>>();

            for (var x = flooredBL.X - 1; x < ceilingTR.X; x++)
            {
                for (var y = flooredBL.Y - 1; y < ceilingTR.Y; y++)
                {
                    var indices = new Vector2i((int) x, (int) y);

                    if (grid?.TryGetTileRef(indices, out _) == true)
                        continue;

                    var value = _biome.GetValue(indices, seed);
                    var group = _biome.GetGroup(groups, value, weightSum);
                    group = _biome.GetAdjustedGroup(group, groups, indices, seed, weightSum);

                    // Iterate through neighbors and work out what edges we need to draw.
                    for (var i = -1; i <= 1; i++)
                    {
                        for (var j = -1; j <= 1; j++)
                        {
                            if (i == 0 && j == 0)
                                continue;

                            var neighborIndices = new Vector2i(indices.X + i, indices.Y + j);
                            var neighborValue = _biome.GetValue(neighborIndices, seed);
                            var neighborGroup = _biome.GetGroup(groups, neighborValue, weightSum);
                            neighborGroup = _biome.GetAdjustedGroup(neighborGroup, groups, neighborIndices, seed,
                                weightSum);

                            if (group == neighborGroup || neighborGroup.Edges.Count == 0)
                                continue;

                            if (!neighborDirections.TryGetValue(neighborGroup, out var directions))
                            {
                                directions = new List<Direction>();
                                neighborDirections[neighborGroup] = directions;
                            }

                            var dir = new Vector2i(i, j).AsDirection();
                            directions.Add(dir);
                        }
                    }

                    if (neighborDirections.Count == 0)
                        continue;

                    foreach (var (neighborGroup, flags) in neighborDirections)
                    {
                        foreach (var dir in flags)
                        {
                            switch (dir)
                            {
                                // Corner sprites
                                case Direction.NorthWest:
                                    if (!flags.Contains(Direction.West) &&
                                        !flags.Contains(Direction.North))
                                    {
                                        var tex = _resource
                                            .GetResource<TextureResource>(neighborGroup.Edges[BiomeEdge.Single]).Texture;

                                        var box = Box2.FromDimensions(indices, (tileSize, tileSize));
                                        screenHandle.DrawTextureRect(tex, box);
                                    }
                                    break;
                                case Direction.SouthWest:
                                    if (!flags.Contains(Direction.West) &&
                                        !flags.Contains(Direction.South))
                                    {
                                        var tex = _resource
                                            .GetResource<TextureResource>(neighborGroup.Edges[BiomeEdge.Single]).Texture;

                                        var box = Box2.FromDimensions(indices, (tileSize, tileSize));
                                        screenHandle.DrawTextureRect(tex, new Box2Rotated(box, new Angle(MathF.PI / 2f), box.Center));
                                    }
                                    break;
                                case Direction.SouthEast:
                                    if (!flags.Contains(Direction.East) &&
                                        !flags.Contains(Direction.South))
                                    {
                                        var tex = _resource
                                            .GetResource<TextureResource>(neighborGroup.Edges[BiomeEdge.Single]).Texture;

                                        var box = Box2.FromDimensions(indices, (tileSize, tileSize));
                                        screenHandle.DrawTextureRect(tex, new Box2Rotated(box, new Angle(MathF.PI), box.Center));
                                    }
                                    break;
                                case Direction.NorthEast:
                                    if (!flags.Contains(Direction.East) &&
                                        !flags.Contains(Direction.North))
                                    {
                                        var tex = _resource
                                            .GetResource<TextureResource>(neighborGroup.Edges[BiomeEdge.Single]).Texture;

                                        var box = Box2.FromDimensions(indices, (tileSize, tileSize));
                                        screenHandle.DrawTextureRect(tex, new Box2Rotated(box, new Angle(MathF.PI * 1.5f), box.Center));
                                    }
                                    break;
                                // Edge sprites

                                case Direction.North:
                                    {
                                        var tex = _resource
                                            .GetResource<TextureResource>(neighborGroup.Edges[BiomeEdge.Double]).Texture;

                                        var box = Box2.FromDimensions(indices, (tileSize, tileSize));
                                        screenHandle.DrawTextureRect(tex, box);
                                    }
                                    break;
                                case Direction.West:
                                    {
                                        var tex = _resource
                                            .GetResource<TextureResource>(neighborGroup.Edges[BiomeEdge.Double]).Texture;

                                        var box = Box2.FromDimensions(indices, (tileSize, tileSize));
                                        screenHandle.DrawTextureRect(tex, new Box2Rotated(box, new Angle(MathF.PI / 2f), box.Center));
                                    }
                                    break;
                                case Direction.South:
                                    {
                                        var tex = _resource
                                            .GetResource<TextureResource>(neighborGroup.Edges[BiomeEdge.Double]).Texture;

                                        var box = Box2.FromDimensions(indices, (tileSize, tileSize));
                                        screenHandle.DrawTextureRect(tex, new Box2Rotated(box, new Angle(MathF.PI), box.Center));
                                    }
                                    break;
                                case Direction.East:
                                    {
                                        var tex = _resource
                                            .GetResource<TextureResource>(neighborGroup.Edges[BiomeEdge.Double]).Texture;

                                        var box = Box2.FromDimensions(indices, (tileSize, tileSize));
                                        screenHandle.DrawTextureRect(tex, new Box2Rotated(box, new Angle(MathF.PI * 1.5f), box.Center));
                                    }
                                    break;
                            }
                        }

                        flags.Clear();
                    }

                    neighborDirections.Clear();
                }
            }
        }
    }
}
