using Content.Server.Doors.Systems;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Wires;

namespace Content.Server.Doors;

public sealed partial class DoorAlarmWireAction : ComponentWireAction<DoorAlarmComponent>
{
    public override Color Color { get; set; } = Color.Yellow;
    public override string Name { get; set; } = "wire-name-door-alarm";

    [DataField("timeout")]
    private int _timeout = 10;


    public override StatusLightState? GetLightState(Wire wire, DoorAlarmComponent comp)
    {
        if (comp.AlarmTripped)
        {
            return StatusLightState.BlinkingFast;
        }
        else
        {
            return StatusLightState.Off;
        }
    }

    public override object StatusKey { get; } = AirlockWireStatus.AlarmIndicator;

    public override bool Cut(EntityUid user, Wire wire, DoorAlarmComponent door)
    {
        WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        EntityManager.System<DoorSystem>().SetAlarmTripped((wire.Owner, door),false);
        EntityManager.System<DoorSystem>().DisableAlarmSound((wire.Owner, door));
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, DoorAlarmComponent door)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, DoorAlarmComponent door)
    {
        EntityManager.System<DoorSystem>().SetAlarmTripped((wire.Owner, door),true);
        EntityManager.System<DoorSystem>().EnableAlarmSound((wire.Owner, door));
        WiresSystem.StartWireAction(wire.Owner, _timeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitAlarmTimerFinish, wire));
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
        {
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);

        }
    }

    private void AwaitAlarmTimerFinish(Wire wire)
    {
        if (!wire.IsCut)
        {
            if (EntityManager.TryGetComponent<DoorAlarmComponent>(wire.Owner, out var door))
            {
                EntityManager.System<DoorSystem>().SetAlarmTripped((wire.Owner, door),false);
                EntityManager.System<DoorSystem>().DisableAlarmSound((wire.Owner, door));
            }
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }

}
