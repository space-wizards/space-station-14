using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Components;
using Robust.Client.Player;
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

            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} player)
                return;

            if (TryComp<RelayInputMoverComponent>(player, out var relayMover))
            {
                if (relayMover.RelayEntity != null)
                    HandleClientsideMovement(relayMover.RelayEntity.Value, frameTime);

                return;
            }

            HandleClientsideMovement(player, frameTime);
        }

        private void HandleClientsideMovement(EntityUid player, float frameTime)
        {
            if (!TryComp(player, out InputMoverComponent? mover) ||
                !TryComp(player, out TransformComponent? xform)) return;

            PhysicsComponent? body = null;

            if (mover.ToParent && HasComp<RelayInputMoverComponent>(xform.ParentUid))
            {
                if (!TryComp(xform.ParentUid, out body)) return;
            }
            else if (!TryComp(player, out body))
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
            HandleMobMovement(mover, body, xform, frameTime);
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
