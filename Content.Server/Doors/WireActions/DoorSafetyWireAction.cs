using Content.Server.Doors.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Wires;

namespace Content.Server.Doors;

[DataDefinition]
public sealed class DoorSafetyWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Red;

    [DataField("name")]
    private string _text = "SAFE";

    [DataField("timeout")]
    private int _timeout = 30;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;
        if (IsPowered(wire.Owner)
            && EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            lightState = door.Safety
                ? StatusLightState.On
                : StatusLightState.Off;
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    public override object StatusKey { get; } = AirlockWireStatus.SafetyIndicator;

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
            door.Safety = false;
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            door.Safety = true;
        }

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            door.Safety = false;
            WiresSystem.StartWireAction(wire.Owner, _timeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitSafetyTimerFinish, wire));
        }

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
