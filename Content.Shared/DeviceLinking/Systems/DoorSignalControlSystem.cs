using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Payloads;
using Content.Shared.Doors.Components;
using Content.Shared.Doors;
using Content.Shared.Doors.Systems;
using JetBrains.Annotations;

namespace Content.Shared.DeviceLinking.Systems;

[UsedImplicitly]
public sealed partial class DoorSignalControlSystem : EntitySystem
{
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private DeviceLinkSystem _signal = default!;

    [Dependency] private EntityQuery<DoorComponent> _doorQuery;
    [Dependency] private EntityQuery<DoorBoltComponent> _doorBoltQuery;

    [SubscribeLocalEvent]
    private void OnInit(Entity<DoorSignalControlComponent> ent, ref ComponentInit args)
    {
        _signal.EnsureSinkPorts(ent.Owner, ent.Comp.OpenPort, ent.Comp.ClosePort, ent.Comp.TogglePort);

        if (HasComp<DoorBoltComponent>(ent.Owner))
            _signal.EnsureSinkPort(ent.Owner, ent.Comp.InBolt);

        _signal.EnsureSourcePort(ent.Owner, ent.Comp.OutOpen);

        if (HasComp<DoorBoltComponent>(ent.Owner))
            _signal.EnsureSourcePort(ent.Owner, ent.Comp.OutBolt);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<DoorSignalControlComponent> ent, ref SignalReceivedEvent args)
    {
        if (!_doorQuery.TryComp(ent.Owner, out var door))
            return;

        if (args.Port == ent.Comp.OpenPort)
        {
            if (door.State == DoorState.Closed)
                _door.TryOpen(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.ClosePort)
        {
            if (door.State == DoorState.Open)
                _door.TryClose(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.TogglePort)
        {
            _door.TryToggleDoor(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.InBolt)
        {
            if (!_doorBoltQuery.TryComp(ent.Owner, out var bolts))
                return;

            // If it's a pulse toggle, otherwise set bolts to high/low.
            _door.SetBoltsDown((ent.Owner, bolts), !bolts.BoltsDown);
        }
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<DoorSignalControlComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        if (!_doorQuery.TryComp(ent.Owner, out var door))
            return;

        var state = args.Data.State;
        if (args.Port == ent.Comp.OpenPort)
        {
            if (state == SignalState.Low)
                return;

            if (door.State == DoorState.Closed)
                _door.TryOpen(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.ClosePort)
        {
            if (state == SignalState.Low)
                return;

            if (door.State == DoorState.Open)
                _door.TryClose(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.TogglePort)
        {
            if (state != SignalState.Low)
                _door.TryToggleDoor(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.InBolt)
        {
            if (!_doorBoltQuery.TryComp(ent.Owner, out var bolts))
                return;

            // If it's a pulse toggle, otherwise set bolts to high/low.
            bool bolt;
            if (state == SignalState.Momentary)
                bolt = !bolts.BoltsDown;
            else
                bolt = state == SignalState.High;

            _door.SetBoltsDown((ent.Owner, bolts), bolt);
        }
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<DoorSignalControlComponent> ent, ref DoorStateChangedEvent args)
    {
        switch (args.State)
        {
            case DoorState.Closed:
                // only ever say the door is closed when it is completely airtight
                _signal.SendSignal(ent.Owner, ent.Comp.OutOpen, false);
                break;
            case DoorState.Open:
            case DoorState.Opening:
            case DoorState.Closing:
            case DoorState.Emagging:
                // say the door is open whenever it would be letting air pass
                _signal.SendSignal(ent.Owner, ent.Comp.OutOpen, true);
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnBoltsChanged(Entity<DoorSignalControlComponent> ent, ref DoorBoltsChangedEvent args)
    {
        _signal.SendSignal(ent.Owner, ent.Comp.OutBolt, args.BoltsDown);
    }
}
