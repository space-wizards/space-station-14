using System.Numerics;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Random.Helpers;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    // Temporary caches.
    private readonly HashSet<EntityUid> _entitySet = new();
    private readonly List<DungeonRoomPrototype> _availableRooms = new();

    /// <summary>
    /// Gets a random dungeon room matching the specified area, whitelist and size.
    /// </summary>
    public DungeonRoomPrototype? GetRoomPrototype(Vector2i size, Random random, EntityWhitelist? whitelist = null)
    {
        return GetRoomPrototype(random, whitelist, minSize: size, maxSize: size);
    }

    /// <summary>
    /// Gets a random dungeon room matching the specified area and whitelist and size range
    /// </summary>
    public DungeonRoomPrototype? GetRoomPrototype(Random random,
        EntityWhitelist? whitelist = null,
        Vector2i? minSize = null,
        Vector2i? maxSize = null)
    {
        // Can never be true.
        if (whitelist is { Tags: null })
        {
            return null;
        }

        _availableRooms.Clear();

        foreach (var proto in _prototype.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            if (minSize is not null && (proto.Size.X < minSize.Value.X || proto.Size.Y < minSize.Value.Y))
                continue;

            if (maxSize is not null && (proto.Size.X > maxSize.Value.X || proto.Size.Y > maxSize.Value.Y))
                continue;

            if (whitelist == null)
            {
                _availableRooms.Add(proto);
                continue;
            }

            foreach (var tag in whitelist.Tags)
            {
                if (!proto.Tags.Contains(tag))
                    continue;

                _availableRooms.Add(proto);
                break;
            }
        }

        if (_availableRooms.Count == 0)
            return null;

        var room = _availableRooms[random.Next(_availableRooms.Count)];

        return room;
    }

    public void SpawnRoom(
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i origin,
        DungeonRoomPrototype room,
        Random random,
        HashSet<Vector2i>? reservedTiles,
        bool clearExisting = false,
        bool rotation = false)
    {
        var originTransform = Matrix3Helpers.CreateTranslation(origin.X, origin.Y);
        var roomRotation = Angle.Zero;

        if (rotation)
        {
            roomRotation = GetRoomRotation(room, random);
        }

        var roomTransform = Matrix3Helpers.CreateTransform((Vector2) room.Size / 2f, roomRotation);
        var finalTransform = Matrix3x2.Multiply(roomTransform, originTransform);

        SpawnRoom(gridUid, grid, finalTransform, room, reservedTiles, clearExisting);
    }

    public Angle GetRoomRotation(DungeonRoomPrototype room, Random random)
    {
        var roomRotation = Angle.Zero;

        if (room.Size.X == room.Size.Y)
        {
            // Give it a random rotation
            roomRotation = random.Next(4) * Math.PI / 2;
        }
        else if (random.Next(2) == 1)
        {
            roomRotation += Math.PI;
        }

        return roomRotation;
    }

    public void SpawnRoom(
        EntityUid gridUid,
        MapGridComponent grid,
        Matrix3x2 roomTransform,
        DungeonRoomPrototype room,
        HashSet<Vector2i>? reservedTiles = null,
        bool clearExisting = false)
    {
        // Ensure the underlying template exists.
        var roomMap = GetOrCreateTemplate(room);
        var templateMapUid = _mapManager.GetMapEntityId(roomMap);
        var templateGrid = Comp<MapGridComponent>(templateMapUid);
        var roomDimensions = room.Size;

        var finalRoomRotation = roomTransform.Rotation();

        var roomCenter = (room.Offset + room.Size / 2f) * grid.TileSize;
        var tileOffset = -roomCenter + grid.TileSizeHalfVector;
        _tiles.Clear();

        // Load tiles
        for (var x = 0; x < roomDimensions.X; x++)
        {
            for (var y = 0; y < roomDimensions.Y; y++)
            {
                var indices = new Vector2i(x + room.Offset.X, y + room.Offset.Y);
                var tileRef = _maps.GetTileRef(templateMapUid, templateGrid, indices);

                var tilePos = Vector2.Transform(indices + tileOffset, roomTransform);
                var rounded = tilePos.Floored();

                if (!clearExisting && reservedTiles?.Contains(rounded) == true)
                    continue;

                if (room.IgnoreTile is not null)
                {
                    if (_maps.TryGetTileDef(templateGrid, indices, out var tileDef) && room.IgnoreTile == tileDef.ID)
                        continue;
                }

                _tiles.Add((rounded, tileRef.Tile));

                if (clearExisting)
                {
                    var anchored = _maps.GetAnchoredEntities((gridUid, grid), rounded);
                    foreach (var ent in anchored)
                    {
                        QueueDel(ent);
                    }
                }
            }
        }

        var bounds = new Box2(room.Offset, room.Offset + room.Size);

        _maps.SetTiles(gridUid, grid, _tiles);

        // Load entities
        // TODO: I don't think engine supports full entity copying so we do this piece of shit.

        foreach (var templateEnt in _lookup.GetEntitiesIntersecting(templateMapUid, bounds, LookupFlags.Uncontained))
        {
            var templateXform = _xformQuery.GetComponent(templateEnt);
            var childPos = Vector2.Transform(templateXform.LocalPosition - roomCenter, roomTransform);

            if (!clearExisting && reservedTiles?.Contains(childPos.Floored()) == true)
                continue;

            var childRot = templateXform.LocalRotation + finalRoomRotation;
            var protoId = _metaQuery.GetComponent(templateEnt).EntityPrototype?.ID;

            // TODO: Copy the templated entity as is with serv
            var ent = Spawn(protoId, new EntityCoordinates(gridUid, childPos));

            var childXform = _xformQuery.GetComponent(ent);
            var anchored = templateXform.Anchored;
            _transform.SetLocalRotation(ent, childRot, childXform);

            // If the templated entity was anchored then anchor us too.
            if (anchored && !childXform.Anchored)
                _transform.AnchorEntity((ent, childXform), (gridUid, grid));
            else if (!anchored && childXform.Anchored)
                _transform.Unanchor(ent, childXform);
        }

        // Load decals
        if (TryComp<DecalGridComponent>(templateMapUid, out var loadedDecals))
        {
            EnsureComp<DecalGridComponent>(gridUid);

            foreach (var (_, decal) in _decals.GetDecalsIntersecting(templateMapUid, bounds, loadedDecals))
            {
                // Offset by 0.5 because decals are offset from bot-left corner
                // So we convert it to center of tile then convert it back again after transform.
                // Do these shenanigans because 32x32 decals assume as they are centered on bottom-left of tiles.
                var position = Vector2.Transform(decal.Coordinates + grid.TileSizeHalfVector - roomCenter, roomTransform);
                position -= grid.TileSizeHalfVector;

                if (!clearExisting && reservedTiles?.Contains(position.Floored()) == true)
                    continue;

                // Umm uhh I love decals so uhhhh idk what to do about this
                var angle = (decal.Angle + finalRoomRotation).Reduced();

                // Adjust because 32x32 so we can't rotate cleanly
                // Yeah idk about the uhh vectors here but it looked visually okay but they may still be off by 1.
                // Also EyeManager.PixelsPerMeter should really be in shared.
                if (angle.Equals(Math.PI))
                {
                    position += new Vector2(-1f / 32f, 1f / 32f);
                }
                else if (angle.Equals(-Math.PI / 2f))
                {
                    position += new Vector2(-1f / 32f, 0f);
                }
                else if (angle.Equals(Math.PI / 2f))
                {
                    position += new Vector2(0f, 1f / 32f);
                }
                else if (angle.Equals(Math.PI * 1.5f))
                {
                    // I hate this but decals are bottom-left rather than center position and doing the
                    // matrix ops is a PITA hence this workaround for now; I also don't want to add a stupid
                    // field for 1 specific op on decals
                    if (decal.Id != "DiagonalCheckerAOverlay" &&
                        decal.Id != "DiagonalCheckerBOverlay")
                    {
                        position += new Vector2(-1f / 32f, 0f);
                    }
                }

                var tilePos = position.Floored();

                // Fallback because uhhhhhhhh yeah, a corner tile might look valid on the original
                // but place 1 nanometre off grid and fail the add.
                if (!_maps.TryGetTileRef(gridUid, grid, tilePos, out var tileRef) || tileRef.Tile.IsEmpty)
                {
                    _maps.SetTile(gridUid, grid, tilePos, _tile.GetVariantTile((ContentTileDefinition) _tileDefManager[FallbackTileId], _random.GetRandom()));
                }

                var result = _decals.TryAddDecal(
                    decal.Id,
                    new EntityCoordinates(gridUid, position),
                    out _,
                    decal.Color,
                    angle,
                    decal.ZIndex,
                    decal.Cleanable);

                DebugTools.Assert(result);
            }
        }
    }
}
