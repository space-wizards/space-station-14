using Content.Server.Doors.Systems;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Doors;

public sealed partial class DoorBoltWireAction : ComponentWireAction<DoorBoltComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-door-bolt";

    public override StatusLightState? GetLightState(Wire wire, DoorBoltComponent comp)
        => comp.BoltsDown ? StatusLightState.On : StatusLightState.Off;

    public override object StatusKey { get; } = AirlockWireStatus.BoltIndicator;

    public override bool Cut(EntityUid user, Wire wire, Entity<DoorBoltComponent> airlock)
    {
        EntityManager.System<DoorBoltSystem>().SetBoltWireCut(airlock, true);
        if (!airlock.Comp.BoltsDown && IsPowered(wire.Owner))
            EntityManager.System<DoorBoltSystem>().SetBoltsWithAudio(wire.Owner, airlock, true);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, Entity<DoorBoltComponent> door)
    {
        EntityManager.System<DoorBoltSystem>().SetBoltWireCut(door, false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, Entity<DoorBoltComponent> door)
    {
        if (IsPowered(wire.Owner))
            EntityManager.System<DoorBoltSystem>().SetBoltsWithAudio(wire.Owner, door, !door.Comp.BoltsDown);
        else if (!door.Comp.BoltsDown)
            EntityManager.System<DoorBoltSystem>().SetBoltsWithAudio(wire.Owner, door, true);
    }
}
