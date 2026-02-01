using Content.Server.Doors.Systems;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Doors.WireActions;

public sealed partial class DoorEmergencyAccessWireAction : ComponentWireAction<AirlockComponent>
{
    public override Color Color { get; set; } = Color.Yellow;
    public override string Name { get; set; } = "wire-name-door-emergency-access";

    public override StatusLightState? GetLightState(Wire wire, AirlockComponent comp)
    {
        return comp.EmergencyAccess ? StatusLightState.On : StatusLightState.Off;
    }

    public override object StatusKey => AirlockWireStatus.EmergencyAccessIndicator;

    public override bool Cut(EntityUid user, Wire wire, AirlockComponent airlock)
    {
        EntityManager.System<AirlockSystem>().SetEmergencyAccessWireCut((wire.Owner, airlock), true);
        EntityManager.System<AirlockSystem>().SetEmergencyAccess((wire.Owner, airlock), true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AirlockComponent airlock)
    {
        EntityManager.System<AirlockSystem>().SetEmergencyAccessWireCut((wire.Owner, airlock), false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AirlockComponent airlock)
    {
        if (IsPowered(wire.Owner))
            EntityManager.System<AirlockSystem>().SetEmergencyAccess((wire.Owner, airlock), !airlock.EmergencyAccess);
    }
}
