using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.Doors.Systems;
using Content.Server.MachineLinking.System;
using Content.Shared.Doors.Components;
using JetBrains.Annotations;

namespace Content.Server.DeviceLinking.Systems
{
    [UsedImplicitly]
    public sealed class DoorSignalControlSystem : EntitySystem
    {
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DoorSignalControlComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DoorSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        private void OnInit(EntityUid uid, DoorSignalControlComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSinkPorts(uid, component.OpenPort, component.ClosePort, component.TogglePort);
        }

        private void OnSignalReceived(EntityUid uid, DoorSignalControlComponent component, ref SignalReceivedEvent args)
        {
            if (!TryComp(uid, out DoorComponent? door))
                return;

            if (args.Port == component.OpenPort)
            {
                if (door.State != DoorState.Open)
                    _doorSystem.TryOpen(uid, door);
            }
            else if (args.Port == component.ClosePort)
            {
                if (door.State != DoorState.Closed)
                    _doorSystem.TryClose(uid, door);
            }
            else if (args.Port == component.TogglePort)
            {
                _doorSystem.TryToggleDoor(uid, door);
            }
        }
    }
}
