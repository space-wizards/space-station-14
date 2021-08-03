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

            // Essentially we only want to set our mob to predicted so every other entity we just interpolate
            // (i.e. only see what the server has sent us).
            // The exception to this is joints.
            body.Predict = true;

            // We set joints to predicted given these can affect how our mob moves.
            // I would only recommend disabling this if you make pulling not use joints anymore (someday maybe?)
            foreach (var joint in body.Joints)
            {
                joint.BodyA.Predict = true;
                joint.BodyB.Predict = true;
            }

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
