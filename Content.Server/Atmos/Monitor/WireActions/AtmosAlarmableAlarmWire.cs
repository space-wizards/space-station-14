using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Wires;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Wires;

namespace Content.Server.Atmos.Monitor;

[DataDefinition]
public sealed class AtmosMonitorDeviceNetWire : BaseWireAction
{
    // whether or not this wire will send out an alarm upon
    // being pulsed
    [DataField("alarmOnPulse")]
    private bool _alarmOnPulse = false;

    private string _text = "NETW";
    private Color _color = Color.Orange;

    private AtmosAlarmableSystem _atmosAlarmableSystem = default!;

    public override object StatusKey { get; } = AtmosMonitorAlarmWireActionKeys.Network;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;

        if (IsPowered(wire.Owner) && EntityManager.TryGetComponent<AtmosMonitorComponent>(wire.Owner, out var monitor))
        {
            if (!_atmosAlarmableSystem.TryGetHighestAlert(wire.Owner, out var alarm))
            {
                alarm = AtmosAlarmType.Normal;
            }

            lightState = alarm == AtmosAlarmType.Danger
                ? StatusLightState.BlinkingFast
                : StatusLightState.On;
        }

        return new StatusLightData(
            _color,
            lightState,
            _text);
    }

    public override void Initialize()
    {
        base.Initialize();

        _atmosAlarmableSystem = EntityManager.System<AtmosAlarmableSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AtmosAlarmableComponent>(wire.Owner, out var monitor))
        {
            monitor.IgnoreAlarms = true;
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        if (EntityManager.TryGetComponent<AtmosAlarmableComponent>(wire.Owner, out var monitor))
        {
            monitor.IgnoreAlarms = false;
        }

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        if (_alarmOnPulse)
        {
            _atmosAlarmableSystem.ForceAlert(wire.Owner, AtmosAlarmType.Danger);
        }

        return true;
    }
}
