using System.Threading.Tasks;
using Content.Shared.Decals;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class DungeonJob
{
    private async Task<Dungeon> GeneratePrefabDungeon(PrefabDunGen prefab, EntityUid gridUid, MapGridComponent grid, int seed)
    {
        var random = new Random(seed);
        var preset = prefab.Presets[random.Next(prefab.Presets.Count)];
        var gen = _prototype.Index<DungeonPresetPrototype>(preset);

        var dungeonRotation = _dungeon.GetDungeonRotation(seed);
        var dungeonTransform = Matrix3.CreateTransform(_position, dungeonRotation);
        var roomPackProtos = new Dictionary<Vector2i, List<DungeonRoomPackPrototype>>();
        var externalNodes = new Dictionary<DungeonRoomPackPrototype, HashSet<Vector2i>>();
        var fallbackTile = new Tile(_tileDefManager[prefab.Tile].TileId);

        foreach (var pack in _prototype.EnumeratePrototypes<DungeonRoomPackPrototype>())
        {
            var size = pack.Size;
            var sizePacks = roomPackProtos.GetOrNew(size);
            sizePacks.Add(pack);

            // Determine external connections; these are only valid when adjacent to a room node.
            // We use this later to determine which room packs connect to each other
            var nodes = new HashSet<Vector2i>();
            externalNodes.Add(pack, nodes);

            foreach (var room in pack.Rooms)
            {
                var rator = new Box2iEdgeEnumerator(room, false);

                while (rator.MoveNext(out var index))
                {
                    nodes.Add(index);
                }
            }
        }

        // Need to sort to make the RNG deterministic (at least without prototype changes).
        foreach (var roomA in roomPackProtos.Values)
        {
            roomA.Sort((x, y) =>
                string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        }

        var roomProtos = new Dictionary<Vector2i, List<DungeonRoomPrototype>>();

        foreach (var proto in _prototype.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            var whitelisted = false;

            foreach (var tag in prefab.RoomWhitelist)
            {
                if (proto.Tags.Contains(tag))
                {
                    whitelisted = true;
                    break;
                }
            }

            if (!whitelisted)
                continue;

            var size = proto.Size;
            var sizeRooms = roomProtos.GetOrNew(size);
            sizeRooms.Add(proto);
        }

        foreach (var roomA in roomProtos.Values)
        {
            roomA.Sort((x, y) =>
                string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        }

        // First we gather all of the edges for each roompack in the preset
        // This allows us to determine which ones should connect from being adjacent
        var edges = new HashSet<Vector2i>[gen.RoomPacks.Count];

        for (var i = 0; i < gen.RoomPacks.Count; i++)
        {
            var pack = gen.RoomPacks[i];
            var nodes = new HashSet<Vector2i>(pack.Width + 2 + pack.Height);

            var rator = new Box2iEdgeEnumerator(pack, false);

            while (rator.MoveNext(out var index))
            {
                nodes.Add(index);
            }

            edges[i] = nodes;
        }

        // Build up edge groups between each pack.
        var connections = new Dictionary<int, Dictionary<int, HashSet<Vector2i>>>();

        for (var i = 0; i < edges.Length; i++)
        {
            var nodes = edges[i];
            var nodeConnections = connections.GetOrNew(i);

            for (var j = i + 1; j < edges.Length; j++)
            {
                var otherNodes = edges[j];
                var intersect = new HashSet<Vector2i>(nodes);

                intersect.IntersectWith(otherNodes);

                if (intersect.Count == 0)
                    continue;

                nodeConnections[j] = intersect;
                var otherNodeConnections = connections.GetOrNew(j);
                otherNodeConnections[i] = intersect;
            }
        }

        var tiles = new List<(Vector2i, Tile)>();
        var dungeon = new Dungeon()
        {
            Position = _position
        };
        var availablePacks = new List<DungeonRoomPackPrototype>();
        var chosenPacks = new DungeonRoomPackPrototype?[gen.RoomPacks.Count];
        var packTransforms = new Matrix3[gen.RoomPacks.Count];
        var packRotations = new Angle[gen.RoomPacks.Count];
        var rotatedPackNodes = new HashSet<Vector2i>[gen.RoomPacks.Count];

        // Actually pick the room packs and rooms
        for (var i = 0; i < gen.RoomPacks.Count; i++)
        {
            var bounds = gen.RoomPacks[i];
            var dimensions = new Vector2i(bounds.Width, bounds.Height);

            // Try every pack rotation
            if (roomPackProtos.TryGetValue(dimensions, out var roomPacks))
            {
                availablePacks.AddRange(roomPacks);
            }

            // Try rotated versions if there are any.
            if (dimensions.X != dimensions.Y)
            {
                var rotatedDimensions = new Vector2i(dimensions.Y, dimensions.X);

                if (roomPackProtos.TryGetValue(rotatedDimensions, out roomPacks))
                {
                    availablePacks.AddRange(roomPacks);
                }
            }

            // Iterate every pack
            // To be valid it needs its edge nodes to overlap with every edge group
            var external = connections[i];

            random.Shuffle(availablePacks);
            Matrix3 packTransform = default!;
            var found = false;
            DungeonRoomPackPrototype pack = default!;

            foreach (var aPack in availablePacks)
            {
                var aExternal = externalNodes[aPack];

                for (var j = 0; j < 4; j++)
                {
                    var dir = (DirectionFlag) Math.Pow(2, j);
                    Vector2i aPackDimensions;

                    if ((dir & (DirectionFlag.East | DirectionFlag.West)) != 0x0)
                    {
                        aPackDimensions = new Vector2i(aPack.Size.Y, aPack.Size.X);
                    }
                    else
                    {
                        aPackDimensions = aPack.Size;
                    }

                    // Rotation doesn't match.
                    if (aPackDimensions != bounds.Size)
                        continue;

                    found = true;
                    var rotatedNodes = new HashSet<Vector2i>(aExternal.Count);
                    var aRotation = dir.AsDir().ToAngle();

                    // Get the external nodes in terms of the dungeon layout
                    // (i.e. rotated if necessary + translated to the room position)
                    foreach (var node in aExternal)
                    {
                        // Get the node in pack terms (offset from center), then rotate it
                        // Afterwards we offset it by where the pack is supposed to be in world terms.
                        var rotated = aRotation.RotateVec((Vector2) node + grid.TileSize / 2f - aPack.Size / 2f);
                        rotatedNodes.Add((rotated + bounds.Center).Floored());
                    }

                    foreach (var group in external.Values)
                    {
                        if (rotatedNodes.Overlaps(group))
                            continue;

                        found = false;
                        break;
                    }

                    if (!found)
                    {
                        continue;
                    }

                    // Use this pack
                    packTransform = Matrix3.CreateTransform(bounds.Center, aRotation);
                    packRotations[i] = aRotation;
                    rotatedPackNodes[i] = rotatedNodes;
                    pack = aPack;
                    break;
                }

                if (found)
                    break;
            }

            availablePacks.Clear();

            // Oop
            if (!found)
            {
                continue;
            }

            // If we're not the first pack then connect to our edges.
            chosenPacks[i] = pack;
            packTransforms[i] = packTransform;
        }

        // Then for overlaps choose either 1x1 / 3x1
        // Pick a random tile for it and then expand outwards as relevant (weighted towards middle?)

        for (var i = 0; i < chosenPacks.Length; i++)
        {
            var pack = chosenPacks[i]!;
            var packTransform = packTransforms[i];
            var packRotation = packRotations[i];

            // Actual spawn cud here.
            // Pickout the room pack template to get the room dimensions we need.
            // TODO: Need to be able to load entities on top of other entities but das a lot of effo
            var packCenter = (Vector2) pack.Size / 2;

            foreach (var roomSize in pack.Rooms)
            {
                var roomDimensions = new Vector2i(roomSize.Width, roomSize.Height);
                Angle roomRotation = Angle.Zero;
                Matrix3 matty;

                if (!roomProtos.TryGetValue(roomDimensions, out var roomProto))
                {
                    roomDimensions = new Vector2i(roomDimensions.Y, roomDimensions.X);

                    if (!roomProtos.TryGetValue(roomDimensions, out roomProto))
                    {
                        Matrix3.Multiply(packTransform, dungeonTransform, out matty);

                        for (var x = roomSize.Left; x < roomSize.Right; x++)
                        {
                            for (var y = roomSize.Bottom; y < roomSize.Top; y++)
                            {
                                var index = matty.Transform(new Vector2(x, y) + grid.TileSize / 2f - packCenter).Floored();
                                tiles.Add((index, new Tile(_tileDefManager["FloorPlanetGrass"].TileId)));
                            }
                        }

                        grid.SetTiles(tiles);
                        tiles.Clear();
                        Logger.Error($"Unable to find room variant for {roomDimensions}, leaving empty.");
                        continue;
                    }

                    roomRotation = new Angle(Math.PI / 2);
                    Logger.Debug($"Using rotated variant for room");
                }

                if (roomDimensions.X == roomDimensions.Y)
                {
                    // Give it a random rotation
                    roomRotation = random.Next(4) * Math.PI / 2;
                }
                else if (random.Next(2) == 1)
                {
                    roomRotation += Math.PI;
                }

                var roomTransform = Matrix3.CreateTransform(roomSize.Center - packCenter, roomRotation);
                var finalRoomRotation = roomRotation + packRotation + dungeonRotation;

                Matrix3.Multiply(roomTransform, packTransform, out matty);
                Matrix3.Multiply(matty, dungeonTransform, out var dungeonMatty);

                var room = roomProto[random.Next(roomProto.Count)];
                var roomMap = _dungeon.GetOrCreateTemplate(room);
                var templateMapUid = _mapManager.GetMapEntityId(roomMap);
                var templateGrid = _entManager.GetComponent<MapGridComponent>(templateMapUid);
                var roomCenter = (room.Offset + room.Size / 2f) * grid.TileSize;
                var roomTiles = new HashSet<Vector2i>(room.Size.X * room.Size.Y);

                // Load tiles
                for (var x = 0; x < room.Size.X; x++)
                {
                    for (var y = 0; y < room.Size.Y; y++)
                    {
                        var indices = new Vector2i(x + room.Offset.X, y + room.Offset.Y);
                        var tileRef = templateGrid.GetTileRef(indices);

                        var tilePos = dungeonMatty.Transform((Vector2) indices + grid.TileSize / 2f - roomCenter);
                        var rounded = tilePos.Floored();
                        tiles.Add((rounded, tileRef.Tile));
                        roomTiles.Add(rounded);
                    }
                }

                var center = Vector2.Zero;

                foreach (var tile in roomTiles)
                {
                    center += ((Vector2) tile + grid.TileSize / 2f);
                }

                center /= roomTiles.Count;

                dungeon.Rooms.Add(new DungeonRoom(roomTiles, center));
                grid.SetTiles(tiles);
                tiles.Clear();
                var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
                var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();

                // Load entities
                // TODO: I don't think engine supports full entity copying so we do this piece of shit.
                var bounds = new Box2(room.Offset, room.Offset + room.Size);

                foreach (var templateEnt in _lookup.GetEntitiesIntersecting(templateMapUid, bounds, LookupFlags.Uncontained))
                {
                    var templateXform = xformQuery.GetComponent(templateEnt);
                    var childPos = dungeonMatty.Transform(templateXform.LocalPosition - roomCenter);
                    var childRot = templateXform.LocalRotation + finalRoomRotation;
                    var protoId = metaQuery.GetComponent(templateEnt).EntityPrototype?.ID;

                    // TODO: Copy the templated entity as is with serv
                    var ent = _entManager.SpawnEntity(protoId,
                        new EntityCoordinates(gridUid, childPos));

                    var childXform = xformQuery.GetComponent(ent);
                    var anchored = templateXform.Anchored;
                    _transform.SetLocalRotation(ent, childRot, childXform);

                    // If the templated entity was anchored then anchor us too.
                    if (anchored && !childXform.Anchored)
                        _transform.AnchorEntity(ent, childXform, grid);
                    else if (!anchored && childXform.Anchored)
                        _transform.Unanchor(ent, childXform);
                }

                // Load decals
                if (_entManager.TryGetComponent<DecalGridComponent>(templateMapUid, out var loadedDecals))
                {
                    _entManager.EnsureComponent<DecalGridComponent>(gridUid);

                    foreach (var (_, decal) in _decals.GetDecalsIntersecting(templateMapUid, bounds, loadedDecals))
                    {
                        // Offset by 0.5 because decals are offset from bot-left corner
                        // So we convert it to center of tile then convert it back again after transform.
                        // Do these shenanigans because 32x32 decals assume as they are centered on bottom-left of tiles.
                        var position = dungeonMatty.Transform(decal.Coordinates + 0.5f - roomCenter);
                        position -= 0.5f;

                        // Umm uhh I love decals so uhhhh idk what to do about this
                        var angle = (decal.Angle + finalRoomRotation).Reduced();

                        // Adjust because 32x32 so we can't rotate cleanly
                        // Yeah idk about the uhh vectors here but it looked visually okay but they may still be off by 1.
                        // Also EyeManager.PixelsPerMeter should really be in shared.
                        if (angle.Equals(Math.PI))
                        {
                            position += new Vector2(-1f / 32f, 1f / 32f);
                        }
                        else if (angle.Equals(Math.PI * 1.5))
                        {
                            position += new Vector2(-1f / 32f, 0f);
                        }
                        else if (angle.Equals(Math.PI / 2f))
                        {
                            position += new Vector2(0f, 1f / 32f);
                        }

                        var tilePos = position.Floored();

                        // Fallback because uhhhhhhhh yeah, a corner tile might look valid on the original
                        // but place 1 nanometre off grid and fail the add.
                        if (!grid.TryGetTileRef(tilePos, out var tileRef) || tileRef.Tile.IsEmpty)
                        {
                            grid.SetTile(tilePos, fallbackTile);
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

                await SuspendIfOutOfTime();
                ValidateResume();
            }
        }

        // Calculate center
        var dungeonCenter = Vector2.Zero;

        foreach (var room in dungeon.Rooms)
        {
            dungeonCenter += room.Center;
        }

        dungeon.Center = (Vector2i) (dungeonCenter / dungeon.Rooms.Count);

        return dungeon;
    }
}
