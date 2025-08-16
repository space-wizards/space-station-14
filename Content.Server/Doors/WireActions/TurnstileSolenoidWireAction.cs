using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Wires;

namespace Content.Server.Doors.WireActions;

public sealed partial class TurnstileSolenoidWireAction : ComponentWireAction<TurnstileComponent>
{
    public override Color Color { get; set; } = Color.Orange;
    public override string Name { get; set; } = "wire-name-turnstile-solenoid";

    [DataField("timeout")]
    private int _timeout = 30;

    public override object StatusKey => AirlockWireStatus.SolenoidIndicator;

    public override StatusLightState? GetLightState(Wire wire, TurnstileComponent component)
    {
        return component.SolenoidBypassed ? StatusLightState.BlinkingSlow : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, TurnstileComponent component)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        EntityManager.System<SharedTurnstileSystem>().SetSolenoidBypassed((wire.Owner, component), true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, TurnstileComponent component)
    {
        EntityManager.System<SharedTurnstileSystem>().SetSolenoidBypassed((wire.Owner, component), false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, TurnstileComponent component)
    {
        EntityManager.System<SharedTurnstileSystem>().SetSolenoidBypassed((wire.Owner, component), true);
        WiresSystem.StartWireAction(wire.Owner, _timeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitSolenoidTimerFinish, wire));
    }

    private void AwaitSolenoidTimerFinish(Wire wire)
    {
        if (wire.IsCut)
            return;
        if (EntityManager.TryGetComponent<TurnstileComponent>(wire.Owner, out var turnstile))
        {
            EntityManager.System<SharedTurnstileSystem>().SetSolenoidBypassed((wire.Owner, turnstile), false);
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key,
    }
}
