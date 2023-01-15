using Content.Server.Doors.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Wires;

namespace Content.Server.Doors;

public sealed class DoorBoltWireAction : ComponentWireAction<AirlockComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "BOLT";
    
    public override StatusLightState? GetLightState(Wire wire, AirlockComponent comp)
        => comp.BoltsDown ? StatusLightState.On : StatusLightState.Off;

    public override object StatusKey { get; } = AirlockWireStatus.BoltIndicator;

    public override bool Cut(EntityUid user, Wire wire, AirlockComponent door)
    {
        door.BoltWireCut = true;
        if (!door.BoltsDown && IsPowered(wire.Owner))
            door.SetBoltsWithAudio(true);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AirlockComponent door)
    {
        door.BoltWireCut = false;
        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire, AirlockComponent door)
    {
        if (IsPowered(wire.Owner))
            door.SetBoltsWithAudio(!door.BoltsDown);
        else if (!door.BoltsDown)
            door.SetBoltsWithAudio(true);

        return true;
    }
}
