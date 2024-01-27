using System.Numerics;
using Content.Shared.Decals;
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
    /// Gets a random dungeon room matching the specified area and whitelist.
    /// </summary>
    public DungeonRoomPrototype? GetRoomPrototype(Vector2i size, Random random, EntityWhitelist? whitelist = null)
    {
        // Can never be true.
        if (whitelist is { Tags: null })
        {
            return null;
        }

        _availableRooms.Clear();

        foreach (var proto in _prototype.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            if (proto.Size != size)
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
        bool clearExisting = false,
        bool rotation = false)
    {
        var originTransform = Matrix3.CreateTranslation(origin);
        SpawnRoom(gridUid, grid, originTransform, room, random, clearExisting, rotation);
    }

    public void SpawnRoom(
        EntityUid gridUid,
        MapGridComponent grid,
        Matrix3 transform,
        DungeonRoomPrototype room,
        Random random,
        bool clearExisting = false,
        bool rotation = false)
    {
        // Ensure the underlying template exists.
        var roomMap = GetOrCreateTemplate(room);
        var templateMapUid = _mapManager.GetMapEntityId(roomMap);
        var templateGrid = Comp<MapGridComponent>(templateMapUid);
        var roomRotation = Angle.Zero;
        var roomDimensions = room.Size;

        if (rotation)
        {
            if (roomDimensions.X == roomDimensions.Y)
            {
                // Give it a random rotation
                roomRotation = random.Next(4) * Math.PI / 2;
            }
            else if (random.Next(2) == 1)
            {
                roomRotation += Math.PI;
            }
        }

        var roomTransform = Matrix3.CreateTransform((Vector2) room.Size / 2f, roomRotation);
        Matrix3.Multiply(roomTransform, transform, out var finalTransform);
        var finalRoomRotation = finalTransform.Rotation();

        // go BRRNNTTT on existing stuff
        if (clearExisting)
        {
            var gridBounds = new Box2(transform.Transform(Vector2.Zero), transform.Transform(room.Size));
            _entitySet.Clear();
            // Polygon skin moment
            gridBounds = gridBounds.Enlarged(-0.05f);
            _lookup.GetLocalEntitiesIntersecting(gridUid, gridBounds, _entitySet, LookupFlags.Uncontained);

            foreach (var templateEnt in _entitySet)
            {
                Del(templateEnt);
            }

            if (TryComp(gridUid, out DecalGridComponent? decalGrid))
            {
                foreach (var decal in _decals.GetDecalsIntersecting(gridUid, gridBounds, decalGrid))
                {
                    _decals.RemoveDecal(gridUid, decal.Index, decalGrid);
                }
            }
        }

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

                var tilePos = finalTransform.Transform(indices + tileOffset);
                var rounded = tilePos.Floored();
                _tiles.Add((rounded, tileRef.Tile));
            }
        }

        var bounds = new Box2(room.Offset, room.Offset + room.Size);

        _maps.SetTiles(gridUid, grid, _tiles);

        // Load entities
        // TODO: I don't think engine supports full entity copying so we do this piece of shit.

        foreach (var templateEnt in _lookup.GetEntitiesIntersecting(templateMapUid, bounds, LookupFlags.Uncontained))
        {
            var templateXform = _xformQuery.GetComponent(templateEnt);
            var childPos = finalTransform.Transform(templateXform.LocalPosition - roomCenter);
            var childRot = templateXform.LocalRotation + finalRoomRotation;
            var protoId = _metaQuery.GetComponent(templateEnt).EntityPrototype?.ID;

            // TODO: Copy the templated entity as is with serv
            var ent = Spawn(protoId, new EntityCoordinates(gridUid, childPos));

            var childXform = _xformQuery.GetComponent(ent);
            var anchored = templateXform.Anchored;
            _transform.SetLocalRotation(ent, childRot, childXform);

            // If the templated entity was anchored then anchor us too.
            if (anchored && !childXform.Anchored)
                _transform.AnchorEntity(ent, childXform, grid);
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
                var position = finalTransform.Transform(decal.Coordinates + Vector2Helpers.Half - roomCenter);
                position -= Vector2Helpers.Half;

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
                    _maps.SetTile(gridUid, grid, tilePos, _tileDefManager.GetVariantTile(FallbackTileId, _random));
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
