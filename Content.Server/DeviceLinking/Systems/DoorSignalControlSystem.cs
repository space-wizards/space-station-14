using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using Content.Server.Doors.Systems;
using Content.Shared.DeviceNetwork;
using Content.Shared.Doors.Components;
using Content.Shared.Doors;
using JetBrains.Annotations;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;

namespace Content.Server.DeviceLinking.Systems
{
    [UsedImplicitly]
    public sealed class DoorSignalControlSystem : EntitySystem
    {
        [Dependency] private readonly DoorBoltSystem _bolts = default!;
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
            _signalSystem.EnsureSourcePorts(uid, component.OutOpen);
        }

        private void OnSignalReceived(EntityUid uid, DoorSignalControlComponent component, ref SignalReceivedEvent args)
        {
            if (!TryComp(uid, out DoorComponent? door))
                return;

            var state = SignalState.Momentary;
            args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);


            if (args.Port == component.OpenPort)
            {
                if (state == SignalState.High || state == SignalState.Momentary)
                {
                    if (door.State == DoorState.Closed)
                        _doorSystem.TryOpen(uid, door);
                }
            }
            else if (args.Port == component.ClosePort)
            {
                if (state == SignalState.High || state == SignalState.Momentary)
                {
                    if (door.State == DoorState.Open)
                        _doorSystem.TryClose(uid, door);
                }
            }
            else if (args.Port == component.TogglePort)
            {
                if (state == SignalState.High || state == SignalState.Momentary)
                {
                    _doorSystem.TryToggleDoor(uid, door);
                }
            }
            else if (args.Port == component.InBolt)
            {
                if (!TryComp<DoorBoltComponent>(uid, out var bolts))
                    return;

                // if its a pulse toggle, otherwise set bolts to high/low
                bool bolt;
                if (state == SignalState.Momentary)
                {
                    bolt = !bolts.BoltsDown;
                }
                else
                {
                    bolt = state == SignalState.High;
                }

                _bolts.SetBoltsWithAudio(uid, bolts, bolt);
            }
        }

        private void OnStateChanged(EntityUid uid, DoorSignalControlComponent door, DoorStateChangedEvent args)
        {
            var data = new NetworkPayload()
            {
                { DeviceNetworkConstants.LogicState, SignalState.Momentary }
            };

            if (args.State == DoorState.Closed)
            {
                data[DeviceNetworkConstants.LogicState] = SignalState.Low;
                _signalSystem.InvokePort(uid, door.OutOpen, data);
            }
            else if (args.State == DoorState.Open
                  || args.State == DoorState.Opening
                  || args.State == DoorState.Closing
                  || args.State == DoorState.Emagging)
            {
                data[DeviceNetworkConstants.LogicState] = SignalState.High;
                _signalSystem.InvokePort(uid, door.OutOpen, data);
            }
        }
    }
}
