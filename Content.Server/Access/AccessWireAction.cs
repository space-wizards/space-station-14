using Content.Server.Wires;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Wires;

namespace Content.Server.Access;

[DataDefinition]
public class AccessWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Green;

    [DataField("name")]
    private string _text = "ACC";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    public override StatusLightData GetStatusLightData(Wire wire)
    {
        StatusLightState lightState = StatusLightState.Off;
        if (EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
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

    public override object Identifier { get; } = AccessWireActionKey.Key;

    public override object StatusKey { get; } = AccessWireActionKey.Status;

    public override bool Cut(EntityUid used, EntityUid user, Wire wire)
    {
        if (!EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
        {
            return false;
        }

        access.Enabled = false;
        wire.IsCut = true;

        return true;
    }

    public override bool Mend(EntityUid used, EntityUid user, Wire wire)
    {
        if (!EntityManager.TryGetComponent<AccessReaderComponent>(wire.Owner, out var access))
        {
            return false;
        }

        access.Enabled = true;
        wire.IsCut = false;

        return true;
    }

    public override bool Pulse(EntityUid used, EntityUid user, Wire wire)
    {

        return true;
    }
}
