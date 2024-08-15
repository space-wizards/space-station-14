using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared.Atmos;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Content.Client.Pinpointer;

public sealed class NavMapData
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    // Default colors
    public Color WallColor = Color.ToSrgb(new Color(102, 217, 102));
    public Color TileColor = new(30, 67, 30);

    /// <summary>
    /// Offset for the data to be drawn at.
    /// </summary>
    public Vector2 Offset;

    public List<(Vector2, Vector2)> TileLines = new();
    public List<(Vector2, Vector2)> TileRects = new();

    public Dictionary<Vector2i, Vector2[]> TilePolygons = new();

    private Dictionary<Vector2i, Vector2i> _horizLines = new();
    private Dictionary<Vector2i, Vector2i> _horizLinesReversed = new();
    private Dictionary<Vector2i, Vector2i> _vertLines = new();
    private Dictionary<Vector2i, Vector2i> _vertLinesReversed = new();

    protected float FullWallInstep = 0.165f;
    protected float ThinWallThickness = 0.165f;
    protected float ThinDoorThickness = 0.30f;

    // TODO: Power should be updating it on its own.
    /// <summary>
    /// Called if navmap updates
    /// </summary>
    public event Action? OnUpdate;

    public NavMapData()
    {
        IoCManager.InjectDependencies(this);
    }

    public void Draw(DrawingHandleBase handle, Func<Vector2, Vector2> scale, Box2 localAABB)
    {
        var verts = new ValueList<Vector2>(TileLines.Count * 2);

        // Draw floor tiles
        if (TilePolygons.Count != 0)
        {
            foreach (var tri in TilePolygons.Values)
            {
                verts.Clear();

                foreach (var vert in tri)
                {
                    verts.Add(new Vector2(vert.X, -vert.Y));
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts.Span, TileColor);
            }
        }

        // Draw map lines
        if (TileLines.Count != 0)
        {
            verts.Clear();

            foreach (var (o, t) in TileLines)
            {
                var origin = scale.Invoke(o - Offset);
                var terminus = scale.Invoke(t - Offset);

                verts.Add(origin);
                verts.Add(terminus);
            }

            if (verts.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, verts.Span, WallColor);
        }

        // Draw map rects
        if (TileRects.Count != 0)
        {
            var rects = new ValueList<Vector2>(TileRects.Count * 8);

            foreach (var (lt, rb) in TileRects)
            {
                var leftTop = scale.Invoke(lt - Offset);
                var rightBottom = scale.Invoke(rb - Offset);

                var rightTop = new Vector2(rightBottom.X, leftTop.Y);
                var leftBottom = new Vector2(leftTop.X, rightBottom.Y);

                rects.Add(leftTop);
                rects.Add(rightTop);
                rects.Add(rightTop);
                rects.Add(rightBottom);
                rects.Add(rightBottom);
                rects.Add(leftBottom);
                rects.Add(leftBottom);
                rects.Add(leftTop);
            }

            if (rects.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, rects.Span, WallColor);
        }
    }

    public void UpdateNavMap(EntityUid entity)
    {
        // Clear stale values
        TilePolygons.Clear();
        TileLines.Clear();
        TileRects.Clear();

        UpdateNavMapFloorTiles(entity);
        UpdateNavMapWallLines(entity);
        UpdateNavMapAirlocks(entity);
    }

    private void UpdateNavMapFloorTiles(Entity<MapGridComponent?> entity)
    {
        if (!_entManager.TryGetComponent(entity.Owner, out entity.Comp))
        {
            return;
        }

        var lookup = _entManager.System<EntityLookupSystem>();
        var tiles = _entManager.System<SharedMapSystem>().GetAllTilesEnumerator(entity.Owner, entity.Comp);

        while (tiles.MoveNext(out var tile))
        {
            var box = lookup.GetLocalBounds(tile.Value.GridIndices, entity.Comp.TileSize);
            box = new Box2(box.Left, -box.Bottom, box.Right, -box.Top);
            var arr = new Vector2[4];

            arr[0] = box.BottomLeft;
            arr[1] = box.BottomRight;
            arr[2] = box.TopRight;
            arr[3] = box.TopLeft;

            TilePolygons[tile.Value.GridIndices] = arr;
        }
    }

    private void UpdateNavMapWallLines(Entity<MapGridComponent?, NavMapComponent?> entity)
    {
        if (!_entManager.TryGetComponent(entity.Owner, out entity.Comp1) ||
            !_entManager.TryGetComponent(entity.Owner, out entity.Comp2))
        {
            return;
        }

        // We'll use the following dictionaries to combine collinear wall lines
        _horizLines.Clear();
        _horizLinesReversed.Clear();
        _vertLines.Clear();
        _vertLinesReversed.Clear();

        const int southMask = (int) AtmosDirection.South << (int) NavMapChunkType.Wall;
        const int eastMask = (int) AtmosDirection.East << (int) NavMapChunkType.Wall;
        const int westMask = (int) AtmosDirection.West << (int) NavMapChunkType.Wall;
        const int northMask = (int) AtmosDirection.North << (int) NavMapChunkType.Wall;

        foreach (var (chunkOrigin, chunk) in entity.Comp2.Chunks)
        {
            for (var i = 0; i < SharedNavMapSystem.ArraySize; i++)
            {
                var tileData = chunk.TileData[i] & SharedNavMapSystem.WallMask;
                if (tileData == 0)
                    continue;

                tileData >>= (int) NavMapChunkType.Wall;

                var relativeTile = SharedNavMapSystem.GetTileFromIndex(i);
                var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * entity.Comp1.TileSize;

                if (tileData != SharedNavMapSystem.AllDirMask)
                {
                    AddRectForThinWall(tileData, tile);
                    continue;
                }

                tile = tile with { Y = -tile.Y };
                NavMapChunk? neighborChunk;

                // North edge
                var neighborData = 0;
                if (relativeTile.Y != SharedNavMapSystem.ChunkSize - 1)
                    neighborData = chunk.TileData[i+1];
                else if (entity.Comp2.Chunks.TryGetValue(chunkOrigin + Vector2i.Up, out neighborChunk))
                    neighborData = neighborChunk.TileData[i + 1 - SharedNavMapSystem.ChunkSize];

                if ((neighborData & southMask) == 0)
                {
                    AddOrUpdateNavMapLine(tile + new Vector2i(0, -entity.Comp1.TileSize),
                        tile + new Vector2i(entity.Comp1.TileSize, -entity.Comp1.TileSize), _horizLines,
                        _horizLinesReversed);
                }

                // East edge
                neighborData = 0;
                if (relativeTile.X != SharedNavMapSystem.ChunkSize - 1)
                    neighborData = chunk.TileData[i + SharedNavMapSystem.ChunkSize];
                else if (entity.Comp2.Chunks.TryGetValue(chunkOrigin + Vector2i.Right, out neighborChunk))
                    neighborData = neighborChunk.TileData[i + SharedNavMapSystem.ChunkSize - SharedNavMapSystem.ArraySize];

                if ((neighborData & westMask) == 0)
                {
                    AddOrUpdateNavMapLine(tile + new Vector2i(entity.Comp1.TileSize, -entity.Comp1.TileSize),
                        tile + new Vector2i(entity.Comp1.TileSize, 0), _vertLines, _vertLinesReversed);
                }

                // South edge
                neighborData = 0;
                if (relativeTile.Y != 0)
                    neighborData = chunk.TileData[i - 1];
                else if (entity.Comp2.Chunks.TryGetValue(chunkOrigin + Vector2i.Down, out neighborChunk))
                    neighborData = neighborChunk.TileData[i - 1 + SharedNavMapSystem.ChunkSize];

                if ((neighborData & northMask) == 0)
                {
                    AddOrUpdateNavMapLine(tile, tile + new Vector2i(entity.Comp1.TileSize, 0), _horizLines,
                        _horizLinesReversed);
                }

                // West edge
                neighborData = 0;
                if (relativeTile.X != 0)
                    neighborData = chunk.TileData[i - SharedNavMapSystem.ChunkSize];
                else if (entity.Comp2.Chunks.TryGetValue(chunkOrigin + Vector2i.Left, out neighborChunk))
                    neighborData = neighborChunk.TileData[i - SharedNavMapSystem.ChunkSize + SharedNavMapSystem.ArraySize];

                if ((neighborData & eastMask) == 0)
                {
                    AddOrUpdateNavMapLine(tile + new Vector2i(0, -entity.Comp1.TileSize), tile, _vertLines,
                        _vertLinesReversed);
                }

                // Add a diagonal line for interiors. Unless there are a lot of double walls, there is no point combining these
                TileLines.Add((tile + new Vector2(0, -entity.Comp1.TileSize), tile + new Vector2(entity.Comp1.TileSize, 0)));
            }
        }

        // Record the combined lines
        foreach (var (origin, terminal) in _horizLines)
        {
            TileLines.Add((origin, terminal));
        }

        foreach (var (origin, terminal) in _vertLines)
        {
            TileLines.Add((origin, terminal));
        }
    }

    private void UpdateNavMapAirlocks(Entity<MapGridComponent?, NavMapComponent?> entity)
    {
        if (!_entManager.TryGetComponent(entity.Owner, out entity.Comp1) ||
            !_entManager.TryGetComponent(entity.Owner, out entity.Comp2))
        {
            return;
        }

        foreach (var chunk in entity.Comp2.Chunks.Values)
        {
            for (var i = 0; i < SharedNavMapSystem.ArraySize; i++)
            {
                var tileData = chunk.TileData[i] & SharedNavMapSystem.AirlockMask;
                if (tileData == 0)
                    continue;

                tileData >>= (int) NavMapChunkType.Airlock;

                var relative = SharedNavMapSystem.GetTileFromIndex(i);
                var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relative) * entity.Comp1.TileSize;

                // If the edges of an airlock tile are not all occupied, draw a thin airlock for each edge
                if (tileData != SharedNavMapSystem.AllDirMask)
                {
                    AddRectForThinAirlock(tileData, tile);
                    continue;
                }

                // Otherwise add a single full tile airlock
                TileRects.Add((new Vector2(tile.X + FullWallInstep, -tile.Y - FullWallInstep),
                    new Vector2(tile.X - FullWallInstep + 1f, -tile.Y + FullWallInstep - 1)));

                TileLines.Add((new Vector2(tile.X + 0.5f, -tile.Y - FullWallInstep),
                    new Vector2(tile.X + 0.5f, -tile.Y + FullWallInstep - 1)));
            }
        }
    }

    private void AddRectForThinWall(int tileData, Vector2i tile)
    {
        var leftTop = new Vector2(-0.5f, 0.5f - ThinWallThickness);
        var rightBottom = new Vector2(0.5f, 0.5f);

        for (var i = 0; i < SharedNavMapSystem.Directions; i++)
        {
            var dirMask = 1 << i;
            if ((tileData & dirMask) == 0)
                continue;

            var tilePosition = new Vector2(tile.X + 0.5f, -tile.Y - 0.5f);

            // TODO NAVMAP
            // Consider using faster rotation operations, given that these are always 90 degree increments
            var angle = -((AtmosDirection) dirMask).ToAngle();
            TileRects.Add((angle.RotateVec(leftTop) + tilePosition, angle.RotateVec(rightBottom) + tilePosition));
        }
    }

    private void AddRectForThinAirlock(int tileData, Vector2i tile)
    {
        var leftTop = new Vector2(-0.5f + FullWallInstep, 0.5f - FullWallInstep - ThinDoorThickness);
        var rightBottom = new Vector2(0.5f - FullWallInstep, 0.5f - FullWallInstep);
        var centreTop = new Vector2(0f, 0.5f - FullWallInstep - ThinDoorThickness);
        var centreBottom = new Vector2(0f, 0.5f - FullWallInstep);

        for (var i = 0; i < SharedNavMapSystem.Directions; i++)
        {
            var dirMask = 1 << i;
            if ((tileData & dirMask) == 0)
                continue;

            var tilePosition = new Vector2(tile.X + 0.5f, -tile.Y - 0.5f);
            var angle = -((AtmosDirection) dirMask).ToAngle();
            TileRects.Add((angle.RotateVec(leftTop) + tilePosition, angle.RotateVec(rightBottom) + tilePosition));
            TileLines.Add((angle.RotateVec(centreTop) + tilePosition, angle.RotateVec(centreBottom) + tilePosition));
        }
    }

    public void AddOrUpdateNavMapLine(
        Vector2i origin,
        Vector2i terminus,
        Dictionary<Vector2i, Vector2i> lookup,
        Dictionary<Vector2i, Vector2i> lookupReversed)
    {
        Vector2i foundTermius;
        Vector2i foundOrigin;

        // Does our new line end at the beginning of an existing line?
        if (lookup.Remove(terminus, out foundTermius))
        {
            DebugTools.Assert(lookupReversed[foundTermius] == terminus);

            // Does our new line start at the end of an existing line?
            if (lookupReversed.Remove(origin, out foundOrigin))
            {
                // Our new line just connects two existing lines
                DebugTools.Assert(lookup[foundOrigin] == origin);
                lookup[foundOrigin] = foundTermius;
                lookupReversed[foundTermius] = foundOrigin;
            }
            else
            {
                // Our new line precedes an existing line, extending it further to the left
                lookup[origin] = foundTermius;
                lookupReversed[foundTermius] = origin;
            }
            return;
        }

        // Does our new line start at the end of an existing line?
        if (lookupReversed.Remove(origin, out foundOrigin))
        {
            // Our new line just extends an existing line further to the right
            DebugTools.Assert(lookup[foundOrigin] == origin);
            lookup[foundOrigin] = terminus;
            lookupReversed[terminus] = foundOrigin;
            return;
        }

        // Completely disconnected line segment.
        lookup.Add(origin, terminus);
        lookupReversed.Add(terminus, origin);
    }
}
