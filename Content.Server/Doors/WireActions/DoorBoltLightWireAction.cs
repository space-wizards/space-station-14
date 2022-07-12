using Content.Server.Doors.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Wires;

namespace Content.Server.Doors;

[DataDefinition]
public sealed class DoorBoltLightWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Lime;

    [DataField("name")]
    private string _text = "BLIT";

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        StatusLightState lightState = StatusLightState.Off;
        if (IsPowered(wire.Owner) && EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            lightState = door.BoltLightsEnabled
                ? StatusLightState.On
                : StatusLightState.Off;
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    public override object StatusKey { get; } = AirlockWireStatus.BoltLightIndicator;

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            door.BoltLightsVisible = false;
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            door.BoltLightsVisible = true;
        }

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            door.BoltLightsVisible = !door.BoltLightsEnabled;
        }

        return true;
    }
}
