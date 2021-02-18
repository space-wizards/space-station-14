#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
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

            if (playerEnt == null || !playerEnt.TryGetComponent(out IMoverComponent? mover) || !playerEnt.TryGetComponent(out IPhysicsComponent? physics))
            {
                return;
            }

            physics.Predict = true;

            UpdateKinematics(playerEnt.Transform, mover, physics);
        }

        public override void Update(float frameTime)
        {
            FrameUpdate(frameTime);
        }
    }
}
