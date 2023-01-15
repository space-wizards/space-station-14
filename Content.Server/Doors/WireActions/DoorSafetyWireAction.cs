using Content.Server.Doors.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Wires;

namespace Content.Server.Doors;

public sealed class DoorSafetyWireAction : ComponentWireAction<AirlockComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "SAFE";
    

    [DataField("timeout")]
    private int _timeout = 30;

    public override StatusLightState? GetLightState(Wire wire, AirlockComponent comp)
        => comp.Safety ? StatusLightState.On : StatusLightState.Off;

    public override object StatusKey { get; } = AirlockWireStatus.SafetyIndicator;

    public override bool Cut(EntityUid user, Wire wire, AirlockComponent door)
    {
        door.Safety = false;
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AirlockComponent door)
    {
        door.Safety = true;
        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire, AirlockComponent door)
    {
        door.Safety = false;
        WiresSystem.StartWireAction(wire.Owner, _timeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitSafetyTimerFinish, wire));
        return true;
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
        {
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        }
    }

    private void AwaitSafetyTimerFinish(Wire wire)
    {
        if (!wire.IsCut)
        {
            if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
            {
                door.Safety = true;
            }
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
