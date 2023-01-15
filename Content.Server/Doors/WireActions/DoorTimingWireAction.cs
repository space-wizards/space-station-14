using Content.Server.Doors.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Wires;

namespace Content.Server.Doors;

public sealed class DoorTimingWireAction : ComponentWireAction<AirlockComponent>
{
    public override Color Color { get; set; } = Color.Orange;
    public override string Name { get; set; } = "TIMR";
    
    [DataField("timeout")]
    private int _timeout = 30;

    public override StatusLightState? GetLightState(Wire wire, AirlockComponent comp)
    {
        switch (comp.AutoCloseDelayModifier)
        {
            case 0.01f:
                return StatusLightState.Off;
            case <= 0.5f:
                return StatusLightState.BlinkingSlow;
            default:
                return StatusLightState.On;
        }
    }

    public override object StatusKey { get; } = AirlockWireStatus.TimingIndicator;

    public override bool Cut(EntityUid user, Wire wire, AirlockComponent door)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        door.AutoCloseDelayModifier = 0.01f;
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AirlockComponent door)
    {
        door.AutoCloseDelayModifier = 1f;
        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire, AirlockComponent door)
    {
        door.AutoCloseDelayModifier = 0.5f;
        WiresSystem.StartWireAction(wire.Owner, _timeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitTimingTimerFinish, wire));
        return true;
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
        {
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        }
    }

    // timing timer??? ???
    private void AwaitTimingTimerFinish(Wire wire)
    {
        if (!wire.IsCut)
        {
            if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
            {
                door.AutoCloseDelayModifier = 1f;
            }
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
