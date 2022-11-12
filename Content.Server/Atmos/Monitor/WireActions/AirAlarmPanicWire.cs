using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Wires;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Wires;

namespace Content.Server.Atmos.Monitor;

[DataDefinition]
public sealed class AirAlarmPanicWire : BaseWireAction
{
    private string _text = "PANC";
    private Color _color = Color.Red;

    private AirAlarmSystem _airAlarmSystem = default!;

    public override object StatusKey { get; } = AirAlarmWireStatus.Panic;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;
        if (IsPowered(wire.Owner) && EntityManager.TryGetComponent<AirAlarmComponent>(wire.Owner, out var alarm))
        {
            lightState = alarm.CurrentMode == AirAlarmMode.Panic
                ? StatusLightState.On
                : StatusLightState.Off;
        }

        return new StatusLightData(
            _color,
            lightState,
            _text);
    }

    public override void Initialize()
    {
        base.Initialize();

        _airAlarmSystem = EntityManager.System<AirAlarmSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<DeviceNetworkComponent>(wire.Owner, out var devNet))
        {
            _airAlarmSystem.SetMode(wire.Owner, devNet.Address, AirAlarmMode.Panic, false);
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<DeviceNetworkComponent>(wire.Owner, out var devNet)
            && EntityManager.TryGetComponent<AirAlarmComponent>(wire.Owner, out var alarm)
            && alarm.CurrentMode == AirAlarmMode.Panic)
        {
            _airAlarmSystem.SetMode(wire.Owner, devNet.Address, AirAlarmMode.Filtering, false, alarm);
        }


        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<DeviceNetworkComponent>(wire.Owner, out var devNet))
        {
            _airAlarmSystem.SetMode(wire.Owner, devNet.Address, AirAlarmMode.Panic, false);
        }

        return true;
    }
}
