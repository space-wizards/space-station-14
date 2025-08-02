using Content.Server.Access;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Server.Doors.Systems;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AirtightSystem _airtightSystem = default!;

    protected override void SetCollidable(
        EntityUid uid,
        bool collidable,
        DoorComponent? door = null,
        PhysicsComponent? physics = null,
        OccluderComponent? occluder = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (door.ChangeAirtight && TryComp(uid, out AirtightComponent? airtight))
            _airtightSystem.SetAirblocked((uid, airtight), collidable);

        // Pathfinding / AI stuff.
        RaiseLocalEvent(new AccessReaderChangeEvent(uid, collidable));

        base.SetCollidable(uid, collidable, door, physics, occluder);
    }
}
