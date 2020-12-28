#nullable enable
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal class MoverSystem : SharedMoverSystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlayerAttachSystemMessage>(PlayerAttached);
            SubscribeLocalEvent<PlayerDetachedSystemMessage>(PlayerDetached);

            UpdatesBefore.Add(typeof(SharedPhysicsSystem));
        }

        private static void PlayerAttached(PlayerAttachSystemMessage ev)
        {
            if (!ev.Entity.HasComponent<IMoverComponent>())
            {
                ev.Entity.AddComponent<PlayerInputMoverComponent>();
            }
        }

        private void PlayerDetached(PlayerDetachedSystemMessage ev)
        {
            if (ev.Entity.HasComponent<PlayerInputMoverComponent>())
            {
                ev.Entity.RemoveComponent<PlayerInputMoverComponent>();
            }

            /*
            if (ev.Entity.TryGetComponent(out PhysicsComponent? physics) &&
                physics.TryGetController(out MoverController controller) &&
                !ev.Entity.IsWeightless())
            {
                controller.StopMoving();
            }
            */
        }
    }
}
