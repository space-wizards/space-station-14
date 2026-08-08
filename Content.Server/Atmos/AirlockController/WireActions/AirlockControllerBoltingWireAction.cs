using Content.Server.Atmos.AirlockController.Systems;
using Content.Server.Wires;
using Content.Shared.Atmos.AirlockController;
using Content.Shared.Wires;

namespace Content.Server.Atmos.AirlockController.WireActions;

/// <summary>
///     Cutting this stops the controller checking bolting and unbolting entirely, but doesn't disable cycling
/// </summary>
public sealed partial class AirlockControllerBoltingWireAction : ComponentWireAction<AirlockControllerComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-airlock-controller-bolting";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    public override object StatusKey { get; } = AirlockControllerWireStatus.BoltingIndicator;

    public override StatusLightState? GetLightState(Wire wire, AirlockControllerComponent comp)
        => comp.BoltingEnabled ? StatusLightState.On : StatusLightState.Off;

    public override bool Cut(EntityUid user, Wire wire, AirlockControllerComponent comp)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        EntityManager.System<AirlockControllerSystem>().SetBoltingEnabled((wire.Owner, comp), false);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AirlockControllerComponent comp)
    {
        EntityManager.System<AirlockControllerSystem>().SetBoltingEnabled((wire.Owner, comp), true);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AirlockControllerComponent comp)
    {
        EntityManager.System<AirlockControllerSystem>().SetBoltingEnabled((wire.Owner, comp), false);
        WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitPulseCancel, wire));
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
    }

    private void AwaitPulseCancel(Wire wire)
    {
        if (wire.IsCut)
            return;

        if (EntityManager.TryGetComponent<AirlockControllerComponent>(wire.Owner, out var comp))
            EntityManager.System<AirlockControllerSystem>().SetBoltingEnabled((wire.Owner, comp), true);
    }

    private enum PulseTimeoutKey : byte
    {
        Key,
    }
}
