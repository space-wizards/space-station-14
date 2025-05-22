using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Power;

namespace Content.Server.Doors.Systems;

public sealed class BoltSystem : SharedBoltSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorBoltComponent, PowerChangedEvent>(OnBoltPowerChanged);
    }

    private void OnBoltPowerChanged(Entity<DoorBoltComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered && ent.Comp.BoltWireCut)
            TrySetBoltsDown(ent, true);

        ent.Comp.Powered = args.Powered;
        UpdateBoltLightStatus(ent);
        Dirty(ent, ent.Comp);
    }
}
