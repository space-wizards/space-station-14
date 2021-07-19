using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Physics.Controllers
{
    public sealed class MoverController : SharedMoverController
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            if (player == null ||
                !player.TryGetComponent(out IMoverComponent? mover) ||
                !player.TryGetComponent(out PhysicsComponent? body)) return;

            body.Predict = true; // TODO: equal prediction instead of true?

            // Server-side should just be handled on its own so we'll just do this shizznit
            if (player.TryGetComponent(out IMobMoverComponent? mobMover))
            {
                HandleMobMovement(mover, body, mobMover);
                return;
            }

            HandleKinematicMovement(mover, body);
        }
    }
}
