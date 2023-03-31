using Content.Server.Doors.Systems;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Wires;

namespace Content.Server.Doors;

public sealed class DoorBoltWireAction : ComponentWireAction<AirlockComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-door-bolt";
    
    public override StatusLightState? GetLightState(Wire wire, AirlockComponent comp)
        => comp.BoltsDown ? StatusLightState.On : StatusLightState.Off;

    public override object StatusKey { get; } = AirlockWireStatus.BoltIndicator;

    public override bool Cut(EntityUid user, Wire wire, AirlockComponent airlock)
    {
        EntityManager.System<SharedAirlockSystem>().SetBoltWireCut(airlock, true);
        if (!airlock.BoltsDown && IsPowered(wire.Owner))
            EntityManager.System<AirlockSystem>().SetBoltsWithAudio(wire.Owner, airlock, true);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AirlockComponent door)
    {
        EntityManager.System<SharedAirlockSystem>().SetBoltWireCut(door, true);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AirlockComponent door)
    {
        if (IsPowered(wire.Owner))
            EntityManager.System<AirlockSystem>().SetBoltsWithAudio(wire.Owner, door, !door.BoltsDown);
        else if (!door.BoltsDown)
            EntityManager.System<AirlockSystem>().SetBoltsWithAudio(wire.Owner, door, true);
    }
}
