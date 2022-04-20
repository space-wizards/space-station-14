using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Doors;

[DataDefinition]
public class DoorBoltLightWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Red;

    [DataField("name")]
    private string _text = "BLIT";

    public override StatusLightData GetStatusLightData(Wire wire)
    {
        StatusLightState lightState = StatusLightState.Off;
        if (WiresSystem.TryGetData(wire.Owner, DoorVisuals.BoltLights, out var lightStatus))
        {
            lightState = (bool) lightStatus
                ? StatusLightState.On
                : StatusLightState.Off;
        }
        else
        {
            lightState = StatusLightState.On;
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    public override object Identifier { get; } = AirlockWireIdentifier.BoltLight;

    public override object StatusKey { get; } = AirlockWireStatus.BoltLightIndicator;

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (!WiresSystem.TryGetData(wire.Owner, DoorVisuals.BoltLights, out var lightStatus));
        {
            WiresSystem.SetData(wire.Owner, DoorVisuals.BoltLights, false);
            lightStatus = false;
        }

        if (EntityManager.TryGetComponent<AppearanceComponent>(wire.Owner, out var appearance))
        {
            appearance.SetData(DoorVisuals.BoltLights, false);
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (!WiresSystem.TryGetData(wire.Owner, DoorVisuals.BoltLights, out var lightStatus))
        {
            lightStatus = true;
        }

        if (EntityManager.TryGetComponent<AppearanceComponent>(wire.Owner, out var appearance))
        {
            appearance.SetData(DoorVisuals.BoltLights, lightStatus);
        }


        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        // TODO: GENERICS, IMMEDIATELY
        bool lightStatus = true;
        if (WiresSystem.TryGetData(wire.Owner, DoorVisuals.BoltLights, out var lightObject))
        {
            lightStatus = (bool) lightObject;
        }

        WiresSystem.SetData(wire.Owner, DoorVisuals.BoltLights, !lightStatus);
        if (EntityManager.TryGetComponent<AppearanceComponent>(wire.Owner, out var appearance))
        {
            appearance.SetData(DoorVisuals.BoltLights, !lightStatus);
        }

        return true;
    }
}
