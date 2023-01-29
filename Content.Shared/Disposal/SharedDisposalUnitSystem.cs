using Content.Shared.Body.Components;
using Content.Shared.Disposal.Components;
using Content.Shared.DragDrop;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Disposal
{
    [UsedImplicitly]
    public abstract class SharedDisposalUnitSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;

        protected static TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        // Percentage
        public const float PressurePerSecond = 0.05f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedDisposalUnitComponent, PreventCollideEvent>(OnPreventCollide);
            SubscribeLocalEvent<SharedDisposalUnitComponent, CanDragDropOnEvent>(OnCanDragDropOn);
        }

        private void OnPreventCollide(EntityUid uid, SharedDisposalUnitComponent component, ref PreventCollideEvent args)
        {
            var otherBody = args.BodyB.Owner;

            // Items dropped shouldn't collide but items thrown should
            if (EntityManager.HasComponent<ItemComponent>(otherBody) &&
                !EntityManager.HasComponent<ThrownItemComponent>(otherBody))
            {
                args.Cancelled = true;
                return;
            }

            if (component.RecentlyEjected.Contains(otherBody))
            {
                args.Cancelled = true;
            }
        }

        private void OnCanDragDropOn(EntityUid uid, SharedDisposalUnitComponent component, CanDragDropOnEvent args)
        {
            if (args.Handled) return;

            args.CanDrop = CanInsert(component, args.Dragged);
            args.Handled = true;
        }

        public virtual bool CanInsert(SharedDisposalUnitComponent component, EntityUid entity)
        {
            if (!EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored)
                return false;

            // TODO: Probably just need a disposable tag.
            if (!EntityManager.TryGetComponent(entity, out ItemComponent? storable) &&
                !EntityManager.HasComponent<BodyComponent>(entity))
            {
                return false;
            }

            //Check if the entity is a mob and if mobs can be inserted
            if (TryComp<MobStateComponent>(entity, out var damageState) && !component.MobsCanEnter)
                return false;

            if (EntityManager.TryGetComponent(entity, out PhysicsComponent? physics) &&
                (physics.CanCollide || storable != null))
            {
                return true;
            }

            return damageState != null && (!component.MobsCanEnter || _mobState.IsDead(entity, damageState));
        }
    }
}
