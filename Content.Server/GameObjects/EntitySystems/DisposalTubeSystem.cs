using Content.Server.GameObjects.Components.Disposal;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class DisposalTubeSystem : EntitySystem
    {
        private void MoveEvent(MoveEvent moveEvent)
        {
            if (moveEvent.Sender.TryGetComponent(out IDisposalTubeComponent tube))
            {
                tube.MoveEvent(moveEvent);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MoveEvent>(MoveEvent);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<MoveEvent>();
        }
    }
}
