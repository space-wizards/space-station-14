using Content.Shared.MobState.Components;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Components;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Physics.Controllers
{
    public sealed class MoverController : SharedMoverController
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} player ||
                !TryComp(player, out IMoverComponent? mover) ||
                !TryComp(player, out PhysicsComponent? body) ||
                !TryComp(player, out TransformComponent? xform))
            {
                return;
            }

            if (xform.GridUid != null)
                mover.LastGridAngle = GetParentGridAngle(xform, mover);

            // Essentially we only want to set our mob to predicted so every other entity we just interpolate
            // (i.e. only see what the server has sent us).
            // The exception to this is joints.
            body.Predict = true;

            // We set joints to predicted given these can affect how our mob moves.
            // I would only recommend disabling this if you make pulling not use joints anymore (someday maybe?)

            if (TryComp(player, out JointComponent? jointComponent))
            {
                foreach (var joint in jointComponent.GetJoints)
                {
                    joint.BodyA.Predict = true;
                    joint.BodyB.Predict = true;
                }
            }

            // If we're being pulled then we won't predict anything and will receive server lerps so it looks way smoother.
            if (TryComp(player, out SharedPullableComponent? pullableComp))
            {
                if (pullableComp.Puller is {Valid: true} puller && TryComp<PhysicsComponent?>(puller, out var pullerBody))
                {
                    pullerBody.Predict = false;
                    body.Predict = false;

                    if (TryComp<SharedPullerComponent>(player, out var playerPuller) && playerPuller.Pulling != null &&
                        TryComp<PhysicsComponent>(playerPuller.Pulling, out var pulledBody))
                    {
                        pulledBody.Predict = false;
                    }
                }
            }

            // Server-side should just be handled on its own so we'll just do this shizznit
            if (TryComp(player, out IMobMoverComponent? mobMover))
            {
                HandleMobMovement(mover, body, mobMover, xform, frameTime);
                return;
            }

            HandleKinematicMovement(mover, body);
        }

        protected override Filter GetSoundPlayers(EntityUid mover)
        {
            return Filter.Local();
        }

        protected override bool CanSound()
        {
            return _timing.IsFirstTimePredicted && _timing.InSimulation;
        }
    }
}
