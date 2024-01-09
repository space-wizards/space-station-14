using System.Numerics;
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
        var fallbackTile = new Tile(_tileDefManager[prefab.Tile].TileId);

        foreach (var pack in _prototype.EnumeratePrototypes<DungeonRoomPackPrototype>())
        {
            var size = pack.Size;
            var sizePacks = roomPackProtos.GetOrNew(size);
            sizePacks.Add(pack);
        }

        // Need to sort to make the RNG deterministic (at least without prototype changes).
        foreach (var roomA in roomPackProtos.Values)
        {
            roomA.Sort((x, y) =>
                string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        }

        var roomProtos = new Dictionary<Vector2i, List<DungeonRoomPrototype>>(_prototype.Count<DungeonRoomPrototype>());

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

        var tiles = new List<(Vector2i, Tile)>();
        var dungeon = new Dungeon();
        var availablePacks = new List<DungeonRoomPackPrototype>();
        var chosenPacks = new DungeonRoomPackPrototype?[gen.RoomPacks.Count];
        var packTransforms = new Matrix3[gen.RoomPacks.Count];
        var packRotations = new Angle[gen.RoomPacks.Count];

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
            random.Shuffle(availablePacks);
            Matrix3 packTransform = default!;
            var found = false;
            DungeonRoomPackPrototype pack = default!;

            foreach (var aPack in availablePacks)
            {
                var startIndex = random.Next(4);

                for (var j = 0; j < 4; j++)
                {
                    var index = (startIndex + j) % 4;
                    var dir = (DirectionFlag) Math.Pow(2, index);
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
                    var aRotation = dir.AsDir().ToAngle();

                    // Use this pack
                    packTransform = Matrix3.CreateTransform(bounds.Center, aRotation);
                    packRotations[i] = aRotation;
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
                                var index = matty.Transform(new Vector2(x, y) + grid.TileSizeHalfVector - packCenter).Floored();
                                tiles.Add((index, new Tile(_tileDefManager["FloorPlanetGrass"].TileId)));
                            }
                        }

                        grid.SetTiles(tiles);
                        tiles.Clear();
                        _sawmill.Error($"Unable to find room variant for {roomDimensions}, leaving empty.");
                        continue;
                    }

                    roomRotation = new Angle(Math.PI / 2);
                    _sawmill.Debug($"Using rotated variant for room");
                }

                var room = roomProto[random.Next(roomProto.Count)];

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

                Matrix3.Multiply(roomTransform, packTransform, out matty);
                Matrix3.Multiply(matty, dungeonTransform, out var dungeonMatty);

                // The expensive bit yippy.
                _dungeon.SpawnRoom(gridUid, grid, dungeonMatty, room);

                var roomCenter = (room.Offset + room.Size / 2f) * grid.TileSize;
                var roomTiles = new HashSet<Vector2i>(room.Size.X * room.Size.Y);
                var exterior = new HashSet<Vector2i>(room.Size.X * 2 + room.Size.Y * 2);
                var tileOffset = -roomCenter + grid.TileSizeHalfVector;
                Box2i? mapBounds = null;

                for (var x = -1; x <= room.Size.X; x++)
                {
                    for (var y = -1; y <= room.Size.Y; y++)
                    {
                        if (x != -1 && y != -1 && x != room.Size.X && y != room.Size.Y)
                        {
                            continue;
                        }

                        var tilePos = dungeonMatty.Transform(new Vector2i(x + room.Offset.X, y + room.Offset.Y) + tileOffset);
                        exterior.Add(tilePos.Floored());
                    }
                }

                var center = Vector2.Zero;

                for (var x = 0; x < room.Size.X; x++)
                {
                    for (var y = 0; y < room.Size.Y; y++)
                    {
                        var roomTile = new Vector2i(x + room.Offset.X, y + room.Offset.Y);
                        var tilePos = dungeonMatty.Transform(roomTile + tileOffset);
                        var tileIndex = tilePos.Floored();
                        roomTiles.Add(tileIndex);

                        mapBounds = mapBounds?.Union(tileIndex) ?? new Box2i(tileIndex, tileIndex);
                        center += tilePos + grid.TileSizeHalfVector;
                    }
                }

                center /= roomTiles.Count;

                dungeon.Rooms.Add(new DungeonRoom(roomTiles, center, mapBounds!.Value, exterior));

                await SuspendIfOutOfTime();
                ValidateResume();
            }
        }

        // Calculate center and do entrances
        var dungeonCenter = Vector2.Zero;

        foreach (var room in dungeon.Rooms)
        {
            dungeon.RoomTiles.UnionWith(room.Tiles);
            dungeon.RoomExteriorTiles.UnionWith(room.Exterior);
        }

        foreach (var room in dungeon.Rooms)
        {
            dungeonCenter += room.Center;
            SetDungeonEntrance(dungeon, room, random);
        }

        return dungeon;
    }

    private void SetDungeonEntrance(Dungeon dungeon, DungeonRoom room, Random random)
    {
        // TODO: Move to dungeonsystem.

        // TODO: Look at markers and use that.

        // Pick midpoints as fallback
        if (room.Entrances.Count == 0)
        {
            var offset = random.Next(4);

            // Pick an entrance that isn't taken.
            for (var i = 0; i < 4; i++)
            {
                var dir = (Direction) ((i + offset) * 2 % 8);
                Vector2i entrancePos;

                switch (dir)
                {
                    case Direction.East:
                        entrancePos = new Vector2i(room.Bounds.Right + 1, room.Bounds.Bottom + room.Bounds.Height / 2);
                        break;
                    case Direction.North:
                        entrancePos = new Vector2i(room.Bounds.Left + room.Bounds.Width / 2, room.Bounds.Top + 1);
                        break;
                    case Direction.West:
                        entrancePos = new Vector2i(room.Bounds.Left - 1, room.Bounds.Bottom + room.Bounds.Height / 2);
                        break;
                    case Direction.South:
                        entrancePos = new Vector2i(room.Bounds.Left + room.Bounds.Width / 2, room.Bounds.Bottom - 1);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                // Check if it's not blocked
                var blockPos = entrancePos + dir.ToIntVec() * 2;

                if (i != 3 && dungeon.RoomTiles.Contains(blockPos))
                {
                    continue;
                }

                room.Entrances.Add(entrancePos);
                break;
            }
        }
    }
}
