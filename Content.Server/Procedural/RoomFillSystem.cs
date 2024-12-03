using Robust.Shared.Map.Components;

namespace Content.Server.Procedural;

public sealed class RoomFillSystem : EntitySystem
{
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    // Track wreck types (or room types) that have already been spawned in this round
    private HashSet<string> _spawnedRoomTypes = new();
    private HashSet<string> _attemptedRoomTypes = new(); // To track attempted room types for this round

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoomFillComponent, MapInitEvent>(OnRoomFillMapInit);
    }

    private void OnRoomFillMapInit(EntityUid uid, RoomFillComponent component, MapInitEvent args)
    {
        // Early exit if the room size is invalid (e.g., zero or invalid dimensions)
        if (component.Size == Vector2i.Zero)
            return;

        var xform = Transform(uid);

        if (xform.GridUid != null)
        {
            var random = new Random();

            // Try to find a valid room up to 3 times
            int attempts = 0;
            RoomPrototype? room = null;

            while (attempts < 3)
            {
                room = TryGetUniqueRoom(component.Size, component.RoomWhitelist, random);

                if (room != null && !_spawnedRoomTypes.Contains(room.Type))
                {
                    break; // Found a valid room that hasn't been spawned
                }

                attempts++;
            }

            if (room == null || _spawnedRoomTypes.Contains(room.Type))
            {
                // If no valid room is found after 3 attempts, delete the marker
                QueueDel(uid);
                return;
            }

            // Spawn the room (wreck) if we found a valid one
            var mapGrid = Comp<MapGridComponent>(xform.GridUid.Value);
            _dungeon.SpawnRoom(
                xform.GridUid.Value,
                mapGrid,
                _maps.LocalToTile(xform.GridUid.Value, mapGrid, xform.Coordinates),
                room,
                random,
                clearExisting: component.ClearExisting,
                rotation: component.Rotation);

            // Mark this room type as spawned for the round
            _spawnedRoomTypes.Add(room.Type);
            _attemptedRoomTypes.Clear(); // Clear attempted room types after a successful spawn
        }

        // Clean up the marker after processing
        QueueDel(uid);
    }

    private RoomPrototype? TryGetUniqueRoom(Vector2i size, List<string> roomWhitelist, Random random)
    {
        // Shuffle the whitelist to try different room types
        var shuffledWhitelist = new List<string>(roomWhitelist);
        shuffledWhitelist.Shuffle(random);

        foreach (var roomType in shuffledWhitelist)
        {
            if (!_attemptedRoomTypes.Contains(roomType)) // Only try rooms that haven't been attempted yet
            {
                var room = _dungeon.GetRoomPrototype(size, random, roomType);

                if (room != null && !_spawnedRoomTypes.Contains(room.Type))
                {
                    // Mark this room type as attempted to avoid retrying the same one
                    _attemptedRoomTypes.Add(room.Type);
                    return room;
                }
            }
        }

        return null; // Return null if no valid room is found
    }
}
