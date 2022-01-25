using Content.Shared.MobState.Components;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Pulling.Components;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Client.Physics.Controllers
{
    public sealed class MoverController : SharedMoverController
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} player ||
                !EntityManager.TryGetComponent(player, out IMoverComponent? mover) ||
                !EntityManager.TryGetComponent(player, out PhysicsComponent? body) ||
                !EntityManager.TryGetComponent(player, out TransformComponent? xform))
            {
                return;
            }

            if (xform.GridID != GridId.Invalid)
                mover.LastGridAngle = GetParentGridAngle(xform, mover);

            // Essentially we only want to set our mob to predicted so every other entity we just interpolate
            // (i.e. only see what the server has sent us).
            // The exception to this is joints.
            body.Predict = true;

            // We set joints to predicted given these can affect how our mob moves.
            // I would only recommend disabling this if you make pulling not use joints anymore (someday maybe?)

            if (EntityManager.TryGetComponent(player, out JointComponent? jointComponent))
            {
                foreach (var joint in jointComponent.GetJoints)
                {
                    joint.BodyA.Predict = true;
                    joint.BodyB.Predict = true;
                }
            }

            // If we're being pulled then we won't predict anything and will receive server lerps so it looks way smoother.
            if (EntityManager.TryGetComponent(player, out SharedPullableComponent? pullableComp))
            {
                if (pullableComp.Puller is {Valid: true} puller && EntityManager.TryGetComponent<PhysicsComponent?>(puller, out var pullerBody))
                {
                    pullerBody.Predict = false;
                    body.Predict = false;
                }
            }

            // If we're pulling a mob then make sure that isn't predicted so it doesn't fuck our velocity up.
            if (EntityManager.TryGetComponent(player, out SharedPullerComponent? pullerComp))
            {
                if (pullerComp.Pulling is {Valid: true} pulling &&
                    EntityManager.HasComponent<MobStateComponent>(pulling) &&
                    EntityManager.TryGetComponent(pulling, out PhysicsComponent? pullingBody))
                {
                    pullingBody.Predict = false;
                }
            }

            // Server-side should just be handled on its own so we'll just do this shizznit
            if (EntityManager.TryGetComponent(player, out IMobMoverComponent? mobMover))
            {
                HandleMobMovement(mover, body, mobMover);
                return;
            }

            HandleKinematicMovement(mover, body);
        }
    }
}
