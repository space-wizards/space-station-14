using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Physics.Controllers
{
    public sealed class MoverController : SharedMoverController
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RelayInputMoverComponent, PlayerAttachedEvent>(OnRelayPlayerAttached);
            SubscribeLocalEvent<RelayInputMoverComponent, PlayerDetachedEvent>(OnRelayPlayerDetached);
            SubscribeLocalEvent<InputMoverComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<InputMoverComponent, PlayerDetachedEvent>(OnPlayerDetached);
        }

        private void OnRelayPlayerAttached(EntityUid uid, RelayInputMoverComponent component, PlayerAttachedEvent args)
        {
            if (TryComp<InputMoverComponent>(component.RelayEntity, out var inputMover))
                SetMoveInput(inputMover, MoveButtons.None);
        }

        private void OnRelayPlayerDetached(EntityUid uid, RelayInputMoverComponent component, PlayerDetachedEvent args)
        {
            if (TryComp<InputMoverComponent>(component.RelayEntity, out var inputMover))
                SetMoveInput(inputMover, MoveButtons.None);
        }

        private void OnPlayerAttached(EntityUid uid, InputMoverComponent component, PlayerAttachedEvent args)
        {
            SetMoveInput(component, MoveButtons.None);
        }

        private void OnPlayerDetached(EntityUid uid, InputMoverComponent component, PlayerDetachedEvent args)
        {
            SetMoveInput(component, MoveButtons.None);
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} player)
                return;

            if (TryComp<RelayInputMoverComponent>(player, out var relayMover)
                && TryComp(relayMover.RelayEntity, out MovementRelayTargetComponent? targetComp))
            {
                DebugTools.Assert(targetComp.Entities.Count <= 1, "Multiple relayed movers are not supported at the moment");
                HandleClientsideMovement(relayMover.RelayEntity.Value, frameTime);
            }

            HandleClientsideMovement(player, frameTime);
        }

        private void HandleClientsideMovement(EntityUid player, float frameTime)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var moverQuery = GetEntityQuery<InputMoverComponent>();
            var relayTargetQuery = GetEntityQuery<MovementRelayTargetComponent>();

            if (!TryComp(player, out InputMoverComponent? mover) ||
                !xformQuery.TryGetComponent(player, out var xform))
            {
                return;
            }

            PhysicsComponent? body;
            TransformComponent? xformMover = xform;

            if (mover.ToParent && HasComp<RelayInputMoverComponent>(xform.ParentUid))
            {
                if (!TryComp(xform.ParentUid, out body) ||
                    !TryComp(xform.ParentUid, out xformMover))
                {
                    return;
                }
            }
            else if (!TryComp(player, out body))
            {
                return;
            }

            // Essentially we only want to set our mob to predicted so every other entity we just interpolate
            // (i.e. only see what the server has sent us).
            // The exception to this is joints.
            body.Predict = true;

            // We set joints to predicted given these can affect how our mob moves.
            // I would only recommend disabling this if you make pulling not use joints anymore (someday maybe?)

            if (TryComp(player, out JointComponent? jointComponent))
            {
                foreach (var joint in jointComponent.GetJoints.Values)
                {
                    if (TryComp(joint.BodyAUid, out PhysicsComponent? physics))
                        physics.Predict = true;

                    if (TryComp(joint.BodyBUid, out physics))
                        physics.Predict = true;
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
            HandleMobMovement(player, mover, body, xformMover, frameTime, xformQuery, moverQuery, relayTargetQuery);
        }

        protected override bool CanSound()
        {
            return _timing is { IsFirstTimePredicted: true, InSimulation: true };
        }
    }
}
