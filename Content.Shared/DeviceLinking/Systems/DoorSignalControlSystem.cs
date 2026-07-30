using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Doors.Components;
using Content.Shared.Doors;
using Content.Shared.Doors.Systems;
using JetBrains.Annotations;

namespace Content.Shared.DeviceLinking.Systems;

[UsedImplicitly]
public sealed partial class DoorSignalControlSystem : EntitySystem
{
    [Dependency] private SharedDoorSystem _doorSystem = default!;
    [Dependency] private DeviceLinkSystem _signalSystem = default!;

    [Dependency] private EntityQuery<DoorComponent> _doorQuery = default!;
    [Dependency] private EntityQuery<DoorBoltComponent> _doorBoltQuery = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<DoorSignalControlComponent> ent, ref ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(ent.Owner, ent.Comp.OpenPort, ent.Comp.ClosePort, ent.Comp.TogglePort);
        _signalSystem.EnsureSourcePorts(ent.Owner, ent.Comp.OutOpen);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<DoorSignalControlComponent> ent, ref SignalReceivedEvent args)
    {
        if (!_doorQuery.TryComp(ent.Owner, out var door))
            return;

        if (args.Port == ent.Comp.OpenPort)
        {
            if (door.State == DoorState.Closed)
                _doorSystem.TryOpen(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.ClosePort)
        {
            if (door.State == DoorState.Open)
                _doorSystem.TryClose(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.TogglePort)
        {
            _doorSystem.TryToggleDoor(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.InBolt)
        {
            if (!_doorBoltQuery.TryComp(ent.Owner, out var bolts))
                return;

            // If it's a pulse toggle, otherwise set bolts to high/low.
            _doorSystem.SetBoltsDown((ent.Owner, bolts), !bolts.BoltsDown);
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
                _doorSystem.TryOpen(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.ClosePort)
        {
            if (state == SignalState.Low)
                return;

            if (door.State == DoorState.Open)
                _doorSystem.TryClose(ent.Owner, door);
        }
        else if (args.Port == ent.Comp.TogglePort)
        {
            if (state != SignalState.Low)
            {
                _doorSystem.TryToggleDoor(ent.Owner, door);
            }
        }
        else if (args.Port == ent.Comp.InBolt)
        {
            if (!_doorBoltQuery.TryComp(ent.Owner, out var bolts))
                return;

            // If it's a pulse toggle, otherwise set bolts to high/low.
            bool bolt;
            if (state == SignalState.Momentary)
            {
                bolt = !bolts.BoltsDown;
            }
            else
            {
                bolt = state == SignalState.High;
            }

            _doorSystem.SetBoltsDown((ent.Owner, bolts), bolt);
        }
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<DoorSignalControlComponent> ent, ref DoorStateChangedEvent args)
    {
        switch (args.State)
        {
            case DoorState.Closed:
                // only ever say the door is closed when it is completely airtight
                _signalSystem.SendSignal(ent.Owner, ent.Comp.OutOpen, false);
                break;
            case DoorState.Open:
            case DoorState.Opening:
            case DoorState.Closing:
            case DoorState.Emagging:
                // say the door is open whenever it would be letting air pass
                _signalSystem.SendSignal(ent.Owner, ent.Comp.OutOpen, true);
                break;
        }
    }
}
