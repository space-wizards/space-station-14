#region

using Content.Server.Atmos.AirlockController.Systems;
using Content.Server.Wires;
using Content.Shared.Atmos.AirlockController;
using Content.Shared.Wires;

#endregion

namespace Content.Server.Atmos.AirlockController.WireActions;

/// <summary>
///     Mutes the cycle warning lights and signal
/// </summary>
public sealed partial class AirlockControllerLightWireAction : ComponentWireAction<AirlockControllerComponent>
{
    public override Color Color { get; set; } = Color.Lime;
    public override string Name { get; set; } = "wire-name-airlock-controller-light";

    public override object StatusKey { get; } = AirlockControllerWireStatus.EmergencyLightIndicator;

    public override StatusLightState? GetLightState(Wire wire, AirlockControllerComponent comp)
        => comp.EmergencyLightsEnabled ? StatusLightState.On : StatusLightState.Off;

    public override bool Cut(EntityUid user, Wire wire, AirlockControllerComponent comp)
    {
        EntityManager.System<AirlockControllerSystem>().SetEmergencyLightsEnabled((wire.Owner, comp), false);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AirlockControllerComponent comp)
    {
        EntityManager.System<AirlockControllerSystem>().SetEmergencyLightsEnabled((wire.Owner, comp), true);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AirlockControllerComponent comp)
    {
        EntityManager.System<AirlockControllerSystem>()
            .SetEmergencyLightsEnabled((wire.Owner, comp), !comp.EmergencyLightsEnabled);
    }
}
