using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
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
            SubscribeLocalEvent<RelayInputMoverComponent, LocalPlayerAttachedEvent>(OnRelayPlayerAttached);
            SubscribeLocalEvent<RelayInputMoverComponent, LocalPlayerDetachedEvent>(OnRelayPlayerDetached);
            SubscribeLocalEvent<InputMoverComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<InputMoverComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

            SubscribeLocalEvent<InputMoverComponent, UpdateIsPredictedEvent>(OnUpdatePredicted);
            SubscribeLocalEvent<MovementRelayTargetComponent, UpdateIsPredictedEvent>(OnUpdateRelayTargetPredicted);
            SubscribeLocalEvent<PullableComponent, UpdateIsPredictedEvent>(OnUpdatePullablePredicted);
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

        private void OnUpdatePullablePredicted(EntityUid uid, PullableComponent component, ref UpdateIsPredictedEvent args)
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

        private void OnRelayPlayerAttached(EntityUid uid, RelayInputMoverComponent component, LocalPlayerAttachedEvent args)
        {
            Physics.UpdateIsPredicted(uid);
            Physics.UpdateIsPredicted(component.RelayEntity);
            if (MoverQuery.TryGetComponent(component.RelayEntity, out var inputMover))
                SetMoveInput(inputMover, MoveButtons.None);
        }

        private void OnRelayPlayerDetached(EntityUid uid, RelayInputMoverComponent component, LocalPlayerDetachedEvent args)
        {
            Physics.UpdateIsPredicted(uid);
            Physics.UpdateIsPredicted(component.RelayEntity);
            if (MoverQuery.TryGetComponent(component.RelayEntity, out var inputMover))
                SetMoveInput(inputMover, MoveButtons.None);
        }

        private void OnPlayerAttached(EntityUid uid, InputMoverComponent component, LocalPlayerAttachedEvent args)
        {
            SetMoveInput(component, MoveButtons.None);
        }

        private void OnPlayerDetached(EntityUid uid, InputMoverComponent component, LocalPlayerDetachedEvent args)
        {
            SetMoveInput(component, MoveButtons.None);
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} player)
                return;

            if (RelayQuery.TryGetComponent(player, out var relayMover))
                HandleClientsideMovement(relayMover.RelayEntity, frameTime);

            HandleClientsideMovement(player, frameTime);
        }

        private void HandleClientsideMovement(EntityUid player, float frameTime)
        {
            if (!MoverQuery.TryGetComponent(player, out var mover) ||
                !XformQuery.TryGetComponent(player, out var xform))
            {
                return;
            }

            var physicsUid = player;
            PhysicsComponent? body;
            var xformMover = xform;

            if (mover.ToParent && RelayQuery.HasComponent(xform.ParentUid))
            {
                if (!PhysicsQuery.TryGetComponent(xform.ParentUid, out body) ||
                    !XformQuery.TryGetComponent(xform.ParentUid, out xformMover))
                {
                    return;
                }

                physicsUid = xform.ParentUid;
            }
            else if (!PhysicsQuery.TryGetComponent(player, out body))
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
                frameTime);
        }

        protected override bool CanSound()
        {
            return _timing is { IsFirstTimePredicted: true, InSimulation: true };
        }
    }
}
