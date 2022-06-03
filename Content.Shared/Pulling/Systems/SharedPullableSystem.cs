using Content.Shared.ActionBlocker;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Pulling.Components;
using Content.Shared.MobState.Components;

namespace Content.Shared.Pulling.Systems
{
    public sealed class SharedPullableSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly SharedPullingSystem _pullSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedPullableComponent, RelayMoveInputEvent>(OnRelayMoveInput);
        }

        private void OnRelayMoveInput(EntityUid uid, SharedPullableComponent component, RelayMoveInputEvent args)
        {
            var entity = args.Session.AttachedEntity;
            if (entity == null || !_blocker.CanMove(entity.Value)) return;
            if (!component.BeingPulled || (TryComp<MobStateComponent>(component.Owner, out var mobState) && mobState.IsDead())) return;
            _pullSystem.TryStopPull(component);
        }
    }
}
