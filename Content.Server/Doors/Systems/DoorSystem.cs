using Content.Server.Access;
using Content.Server.Forensics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Server.Doors.Systems;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AirtightSystem _airtightSystem = default!;
    [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorBoltComponent, PowerChangedEvent>(OnBoltPowerChanged);
    }

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

    private void OnBoltPowerChanged(Entity<DoorBoltComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            if (ent.Comp.BoltWireCut)
                SetBoltsDown(ent, true);
        }

        UpdateBoltLightStatus(ent);
        ent.Comp.Powered = args.Powered;
        Dirty(ent, ent.Comp);
    }

    public override void StartOpening(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        base.StartOpening(uid, door, user, predicted);

        if (user.HasValue)
            _forensicsSystem.ApplyEvidence(user.Value, uid);
    }

    public override void StartClosing(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        base.StartClosing(uid, door, user, predicted);

        if (user.HasValue)
            _forensicsSystem.ApplyEvidence(user.Value, uid);
    }
}
