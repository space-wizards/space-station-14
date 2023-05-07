using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.Doors.Systems;
using Content.Server.MachineLinking.System;
using Content.Shared.Doors.Components;
using Content.Shared.Doors;
using JetBrains.Annotations;

namespace Content.Server.DeviceLinking.Systems
{
    [UsedImplicitly]
    public sealed class DoorSignalControlSystem : EntitySystem
    {
        [Dependency] private readonly AirlockSystem _airlockSystem = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DoorSignalControlComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DoorSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<DoorSignalControlComponent, DoorStateChangedEvent>(OnStateChanged);
        }

        private void OnInit(EntityUid uid, DoorSignalControlComponent component, ComponentInit args)
        {

            _signalSystem.EnsureSinkPorts(uid, component.OpenPort, component.ClosePort, component.TogglePort);
            _signalSystem.EnsureTransmitterPorts(uid, component.OutOpen);
        }

        private void OnSignalReceived(EntityUid uid, DoorSignalControlComponent component, ref SignalReceivedEvent args)
        {
            if (!TryComp(uid, out DoorComponent? door))
                return;

            if (args.Port == component.OpenPort)
            {
                if (args.State == SignalState.High || args.State == SignalState.Momentary)
                {
                    if (door.State != DoorState.Open)
                        _doorSystem.TryOpen(uid, door);
                }
            }
            else if (args.Port == component.ClosePort)
            {
                if (args.State == SignalState.High || args.State == SignalState.Momentary)
                {
                    if (door.State != DoorState.Closed)
                        _doorSystem.TryClose(uid, door);
                }
            }
            else if (args.Port == component.TogglePort)
            {
                if (args.State == SignalState.High || args.State == SignalState.Momentary)
                {
                    _doorSystem.TryToggleDoor(uid, door);
                }
            }
            else if (args.Port == component.InBolt)
            {
                if (args.State == SignalState.High)
                {
                    if(TryComp<AirlockComponent>(door.Owner, out var airlockComponent))
                        _airlockSystem.SetBoltsWithAudio(door.Owner, airlockComponent, true);
                }
                else
                {
                    if(TryComp<AirlockComponent>(door.Owner, out var airlockComponent))
                        _airlockSystem.SetBoltsWithAudio(door.Owner, airlockComponent, false);
                }
            }
        }

        private void OnStateChanged(EntityUid uid, DoorSignalControlComponent door, DoorStateChangedEvent args)
        {
            if (args.State == DoorState.Closed)
            {
                _signalSystem.InvokePort(uid, door.OutOpen, SignalState.Low);
            }
            else if (args.State == DoorState.Open
                  || args.State == DoorState.Opening
                  || args.State == DoorState.Closing
                  || args.State == DoorState.Emagging)
            {
                _signalSystem.InvokePort(uid, door.OutOpen, SignalState.High);
            }
        }
    }
}
