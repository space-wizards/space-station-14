#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Physics;
using Robust.Client.Player;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Content.Client.Physics.Controllers
{
    public sealed class MovementController : SharedMovementController
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void FrameUpdate(float frameTime)
        {
            var playerEnt = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEnt == null || !playerEnt.TryGetComponent(out IMoverComponent? mover) || !playerEnt.TryGetComponent(out PhysicsComponent? physics))
            {
                return;
            }

            physics.Predict = true;

            UpdateKinematics(mover, physics, playerEnt.Transform);
        }
    }
}
