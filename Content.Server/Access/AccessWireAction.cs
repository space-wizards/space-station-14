using Content.Server.Wires;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Wires;

namespace Content.Server.Access;

[DataDefinition]
public sealed class AccessWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Green;

    [DataField("name")]
    private string _text = "ACC";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        StatusLightState lightState = StatusLightState.Off;
        if (IsPowered(wire.Owner)
            && EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
        {
            if (access.Enabled)
            {
                lightState = StatusLightState.On;
            }
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    public override object StatusKey { get; } = AccessWireActionKey.Status;

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
        {
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
            access.Enabled = false;
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
        {
            access.Enabled = true;
        }

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
        {
            access.Enabled = false;
            WiresSystem.StartWireAction(wire.Owner, _pulseTimeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitPulseCancel, wire));
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

    private void AwaitPulseCancel(Wire wire)
    {
        if (!wire.IsCut)
        {
            if (EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
            {
                access.Enabled = true;
            }
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
