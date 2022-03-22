using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.System
{
    [UsedImplicitly]
    public sealed class DoorSignalControlSystem : EntitySystem
    {
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DoorSignalControlComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DoorSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
        }
        
        private void OnInit(EntityUid uid, DoorSignalControlComponent component, ComponentInit args)
        {
            var receiver = EnsureComp<SignalReceiverComponent>(uid);
            foreach (string port in new[] { "Open", "Close", "Toggle" })
                if (!receiver.Inputs.ContainsKey(port))
                    receiver.AddPort(port);
        }

        private void OnSignalReceived(EntityUid uid, DoorSignalControlComponent component, SignalReceivedEvent args)
        {
            if (!TryComp(uid, out DoorComponent? door)) return;
            switch (args.Port)
            {
                case "Open": if (door.State != DoorState.Open) _doorSystem.TryOpen(uid, door); break;
                case "Close": if (door.State != DoorState.Closed) _doorSystem.TryClose(uid, door); break;
                case "Toggle": _doorSystem.TryToggleDoor(uid); break;
            }
        }
    }
}
