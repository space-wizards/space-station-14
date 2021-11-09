using Content.Shared.MobState.Components;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Pulling.Components;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

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

            if (player.TryGetComponent(out JointComponent? jointComponent))
            {
                foreach (var joint in jointComponent.GetJoints)
                {
                    joint.BodyA.Predict = true;
                    joint.BodyB.Predict = true;
                }
            }

            // If we're being pulled then we won't predict anything and will receive server lerps so it looks way smoother.
            if (player.TryGetComponent(out SharedPullableComponent? pullableComp))
            {
                var puller = pullableComp.Puller;
                if (puller != null && puller.TryGetComponent<PhysicsComponent>(out var pullerBody))
                {
                    pullerBody.Predict = false;
                    body.Predict = false;
                }
            }

            // If we're pulling a mob then make sure that isn't predicted so it doesn't fuck our velocity up.
            if (player.TryGetComponent(out SharedPullerComponent? pullerComp))
            {
                var pulling = pullerComp.Pulling;

                if (pulling != null &&
                    pulling.HasComponent<MobStateComponent>() &&
                    pulling.TryGetComponent(out PhysicsComponent? pullingBody))
                {
                    pullingBody.Predict = false;
                }
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
