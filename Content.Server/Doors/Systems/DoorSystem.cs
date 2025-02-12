using Content.Server.Access;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Wires;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;

namespace Content.Server.Doors.Systems;

public sealed partial class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AirtightSystem _airtightSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMapSystem _mapping = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly WiresSystem _wiresSystem = default!;

    private const int UpdateInterval = 30;
    private int _accumulatedTicks;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorBoltComponent, PowerChangedEvent>(OnBoltPowerChanged);

        InitializeAirlocksServer();
        InitializeFirelocksServer();
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

        ent.Comp.Powered = args.Powered;
        Dirty(ent, ent.Comp);
        UpdateBoltLightStatus(ent);
    }
}
