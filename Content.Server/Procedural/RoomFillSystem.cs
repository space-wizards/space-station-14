using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Procedural;

public sealed class RoomFillSystem : EntitySystem
{
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoomFillComponent, MapInitEvent>(OnRoomFillMapInit);
    }

    private void OnRoomFillMapInit(EntityUid uid, RoomFillComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);

        if (xform.GridUid != null)
        {
            var room = _dungeon.GetRoomPrototype(_random, component.RoomWhitelist, component.MinSize, component.MaxSize);

            if (room != null)
            {
                var mapGrid = Comp<MapGridComponent>(xform.GridUid.Value);
                _dungeon.SpawnRoom(
                    xform.GridUid.Value,
                    mapGrid,
                    _maps.LocalToTile(xform.GridUid.Value, mapGrid, xform.Coordinates) - new Vector2i(room.Size.X/2,room.Size.Y/2),
                    room,
                    _random,
                    null,
                    clearExisting: component.ClearExisting,
                    rotation: component.Rotation);
            }
            else
            {
                Log.Error($"Unable to find matching room prototype for {ToPrettyString(uid)}");
            }
        }

        // Final cleanup
        QueueDel(uid);
    }
}
