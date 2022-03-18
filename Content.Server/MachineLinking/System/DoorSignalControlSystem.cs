using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.Doors.Systems;
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

            SubscribeLocalEvent<DoorSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        private void OnSignalReceived(EntityUid uid, DoorSignalControlComponent component, SignalReceivedEvent args)
        {
            switch (args.Port)
            {
                case "Open": _doorSystem.TryOpen(uid); break;
                case "Close": _doorSystem.TryClose(uid); break;
                case "Toggle": _doorSystem.TryToggleDoor(uid); break;
            }
        }
    }
}
