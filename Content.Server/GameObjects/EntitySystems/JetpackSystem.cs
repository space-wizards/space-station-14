using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Jetpack;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class JetpackSystem : EntitySystem
    {
        private float _timer = 0f;
        private const float Interval = 0.5f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MoveEvent>(MoveEventHandler);
        }

        private void MoveEventHandler(MoveEvent moveEvent)
        {
            // If the jetpack is not equipped it does not matter to us
            if (!moveEvent.Sender.TryGetComponent<InventoryComponent>(out var inventoryComponent))
                return;
            foreach (var ent in inventoryComponent.GetAllHeldItems())
            {
                if (ent.TryGetComponent<JetpackComponent>(out var jetpackComponent))
                {
                    jetpackComponent.HandleMoveEvent(moveEvent);
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Same as GasTankSystem
            _timer += frameTime;
            if (_timer < Interval) return;
            _timer = 0f;

            foreach (var jetpackComponent in EntityManager.ComponentManager.EntityQuery<JetpackComponent>())
            {
                jetpackComponent.Update();
            }
        }
    }
}
