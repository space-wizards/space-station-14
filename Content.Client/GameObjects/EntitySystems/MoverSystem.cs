using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

#nullable enable

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class MoverSystem : SharedMoverSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesBefore.Add(typeof(PhysicsSystem));
        }

        public override void FrameUpdate(float frameTime)
        {
            var playerEnt = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEnt == null || !playerEnt.TryGetComponent(out IMoverComponent mover))
            {
                return;
            }

            var physics = playerEnt.GetComponent<PhysicsComponent>();
            playerEnt.TryGetComponent(out CollidableComponent? collidable);

            UpdateKinematics(playerEnt.Transform, mover, physics, collidable);
        }

        public override void Update(float frameTime)
        {
            FrameUpdate(frameTime);
        }

        protected override void SetController(SharedPhysicsComponent physics)
        {
            ((PhysicsComponent)physics).SetController<MoverController>();
        }
    }
}
