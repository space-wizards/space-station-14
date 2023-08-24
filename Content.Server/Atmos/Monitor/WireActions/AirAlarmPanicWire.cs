using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Wires;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Wires;

namespace Content.Server.Atmos.Monitor;

public sealed partial class AirAlarmPanicWire : ComponentWireAction<AirAlarmComponent>
{
    public override string Name { get; set; } = "wire-name-air-alarm-panic";
    public override Color Color { get; set; } = Color.Red;

    private AirAlarmSystem _airAlarmSystem = default!;

    public override object StatusKey { get; } = AirAlarmWireStatus.Panic;

    public override StatusLightState? GetLightState(Wire wire, AirAlarmComponent comp)
        => comp.CurrentMode == AirAlarmMode.Panic
                ? StatusLightState.On
                : StatusLightState.Off;

    public override void Initialize()
    {
        base.Initialize();

        _airAlarmSystem = EntityManager.System<AirAlarmSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire, AirAlarmComponent comp)
    {
        if (EntityManager.TryGetComponent<DeviceNetworkComponent>(wire.Owner, out var devNet))
        {
            _airAlarmSystem.SetMode(wire.Owner, devNet.Address, AirAlarmMode.Panic, false);
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AirAlarmComponent alarm)
    {
        if (EntityManager.TryGetComponent<DeviceNetworkComponent>(wire.Owner, out var devNet)
            && alarm.CurrentMode == AirAlarmMode.Panic)
        {
            _airAlarmSystem.SetMode(wire.Owner, devNet.Address, AirAlarmMode.Filtering, false, alarm);
        }

        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AirAlarmComponent comp)
    {
        if (EntityManager.TryGetComponent<DeviceNetworkComponent>(wire.Owner, out var devNet))
        {
            _airAlarmSystem.SetMode(wire.Owner, devNet.Address, AirAlarmMode.Panic, false);
        }
    }
}
