using Content.Server.Doors.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Wires;

namespace Content.Server.Doors;

public sealed class DoorBoltLightWireAction : ComponentWireAction<AirlockComponent>
{
    public override Color Color { get; set; } = Color.Lime;
    public override string Name { get; set; } = "wire-name-bolt-light";

    public override StatusLightState? GetLightState(Wire wire, AirlockComponent comp)
        => comp.BoltLightsEnabled ? StatusLightState.On : StatusLightState.Off;

    public override object StatusKey { get; } = AirlockWireStatus.BoltLightIndicator;

    public override bool Cut(EntityUid user, Wire wire, AirlockComponent comp)
    {
        comp.BoltLightsVisible = false;
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AirlockComponent comp)
    {
        comp.BoltLightsVisible = true;
        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire, AirlockComponent comp)
    {
        comp.BoltLightsVisible = !comp.BoltLightsEnabled;
        return true;
    }
}
