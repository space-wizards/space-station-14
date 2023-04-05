using Content.Shared.ActionBlocker;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pulling.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Pulling.Systems
{
    public sealed class SharedPullableSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPullingSystem _pullSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedPullableComponent, MoveInputEvent>(OnRelayMoveInput);
        }

        private void OnRelayMoveInput(EntityUid uid, SharedPullableComponent component, ref MoveInputEvent args)
        {
            var entity = args.Entity;
            if (_mobState.IsIncapacitated(entity) || !_blocker.CanMove(entity)) return;

            _pullSystem.TryStopPull(component);
        }
    }
}
