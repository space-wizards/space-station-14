using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Components;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

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

            SubscribeLocalEvent<InputMoverComponent, UpdateIsPredictedEvent>(OnUpdatePredicted);
            SubscribeLocalEvent<MovementRelayTargetComponent, UpdateIsPredictedEvent>(OnUpdateRelayTargetPredicted);
            SubscribeLocalEvent<SharedPullableComponent, UpdateIsPredictedEvent>(OnUpdatePullablePredicted);
        }

        private void OnUpdatePredicted(EntityUid uid, InputMoverComponent component, ref UpdateIsPredictedEvent args)
        {
            // Enable prediction if an entity is controlled by the player
            if (uid == _playerManager.LocalPlayer?.ControlledEntity)
                args.IsPredicted = true;
        }

        private void OnUpdateRelayTargetPredicted(EntityUid uid, MovementRelayTargetComponent component, ref UpdateIsPredictedEvent args)
        {
            if (component.Source == _playerManager.LocalPlayer?.ControlledEntity)
                args.IsPredicted = true;
        }

        private void OnUpdatePullablePredicted(EntityUid uid, SharedPullableComponent component, ref UpdateIsPredictedEvent args)
        {
            // Enable prediction if an entity is being pulled by the player.
            // Disable prediction if an entity is being pulled by some non-player entity.

            if (component.Puller == _playerManager.LocalPlayer?.ControlledEntity)
                args.IsPredicted = true;
            else if (component.Puller != null)
                args.BlockPrediction = true;

            // TODO recursive pulling checks?
            // What if the entity is being pulled by a vehicle controlled by the player?
        }

        private void OnRelayPlayerAttached(EntityUid uid, RelayInputMoverComponent component, PlayerAttachedEvent args)
        {
            Physics.UpdateIsPredicted(uid);
            Physics.UpdateIsPredicted(component.RelayEntity);
            if (TryComp<InputMoverComponent>(component.RelayEntity, out var inputMover))
                SetMoveInput(inputMover, MoveButtons.None);
        }

        private void OnRelayPlayerDetached(EntityUid uid, RelayInputMoverComponent component, PlayerDetachedEvent args)
        {
            Physics.UpdateIsPredicted(uid);
            Physics.UpdateIsPredicted(component.RelayEntity);
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

            if (TryComp<RelayInputMoverComponent>(player, out var relayMover))
                HandleClientsideMovement(relayMover.RelayEntity, frameTime);

            HandleClientsideMovement(player, frameTime);
        }

        private void HandleClientsideMovement(EntityUid player, float frameTime)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var moverQuery = GetEntityQuery<InputMoverComponent>();
            var relayTargetQuery = GetEntityQuery<MovementRelayTargetComponent>();
            var mobMoverQuery = GetEntityQuery<MobMoverComponent>();
            var pullableQuery = GetEntityQuery<SharedPullableComponent>();
            var modifierQuery = GetEntityQuery<MovementSpeedModifierComponent>();

            if (!moverQuery.TryGetComponent(player, out var mover) ||
                !xformQuery.TryGetComponent(player, out var xform))
            {
                return;
            }

            var physicsUid = player;
            PhysicsComponent? body;
            var xformMover = xform;

            if (mover.ToParent && HasComp<RelayInputMoverComponent>(xform.ParentUid))
            {
                if (!TryComp(xform.ParentUid, out body) ||
                    !TryComp(xform.ParentUid, out xformMover))
                {
                    return;
                }

                physicsUid = xform.ParentUid;
            }
            else if (!TryComp(player, out body))
            {
                return;
            }

            // Server-side should just be handled on its own so we'll just do this shizznit
            HandleMobMovement(
                player,
                mover,
                physicsUid,
                body,
                xformMover,
                frameTime,
                xformQuery,
                moverQuery,
                mobMoverQuery,
                relayTargetQuery,
                pullableQuery,
                modifierQuery);
        }

        protected override bool CanSound()
        {
            return _timing is { IsFirstTimePredicted: true, InSimulation: true };
        }
    }
}
