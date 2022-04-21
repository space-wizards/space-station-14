using Content.Server.Doors.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;

namespace Content.Server.Doors;

[DataDefinition]
public class DoorBoltLightWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Lime;

    [DataField("name")]
    private string _text = "BLIT";

    public override StatusLightData GetStatusLightData(Wire wire)
    {
        StatusLightState lightState = StatusLightState.Off;
        if (IsPowered(wire.Owner) && EntityManager.HasComponent<AirlockComponent>(wire.Owner))
        {
            if (WiresSystem.TryGetData(wire.Owner, DoorVisuals.BoltLights, out var lightStatus))
            {
                lightState = (bool) lightStatus
                    ? StatusLightState.On
                    : StatusLightState.Off;
            }
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    public override void Initialize(Wire wire)
    {
        base.Initialize(wire);

        /*
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            WiresSystem.SetData(wire.Owner, DoorVisuals.BoltLights, door.BoltLightsVisible);
        }
        */
    }

    public override object Identifier { get; } = AirlockWireIdentifier.BoltLight;

    public override object StatusKey { get; } = AirlockWireStatus.BoltLightIndicator;

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            if (!WiresSystem.TryGetData(wire.Owner, DoorVisuals.BoltLights, out var lightStatus))
            {
                WiresSystem.SetData(wire.Owner, DoorVisuals.BoltLights, false);
                lightStatus = false;
            }

            door.BoltLightsVisible = false;
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            if (!WiresSystem.TryGetData(wire.Owner, DoorVisuals.BoltLights, out var lightStatus))
            {
                lightStatus = true;
            }

            door.BoltLightsVisible = true;
        }

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        // TODO: GENERICS, IMMEDIATELY
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            bool lightStatus = true;
            if (WiresSystem.TryGetData(wire.Owner, DoorVisuals.BoltLights, out var lightObject))
            {
                lightStatus = (bool) lightObject;
            }

            WiresSystem.SetData(wire.Owner, DoorVisuals.BoltLights, !lightStatus);

            door.BoltLightsVisible = !lightStatus;
        }

        return true;
    }
}
