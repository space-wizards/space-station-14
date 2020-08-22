#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.IoC;

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

            if (playerEnt == null || !playerEnt.TryGetComponent(out IMoverComponent? mover))
            {
                return;
            }

            var collidable = playerEnt.GetComponent<ICollidableComponent>();
            collidable.Predict = true;

            UpdateKinematics(playerEnt.Transform, mover, collidable);
        }

        public override void Update(float frameTime)
        {
            FrameUpdate(frameTime);
        }
    }
}
