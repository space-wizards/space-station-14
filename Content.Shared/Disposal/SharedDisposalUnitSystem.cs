using System;
using Content.Shared.Body.Components;
using Content.Shared.Disposal.Components;
using Content.Shared.DragDrop;
using Content.Shared.Item;
using Content.Shared.MobState.Components;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;

namespace Content.Shared.Disposal
{
    [UsedImplicitly]
    public abstract class SharedDisposalUnitSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;

        protected static TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        // Percentage
        public const float PressurePerSecond = 0.05f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedDisposalUnitComponent, PreventCollideEvent>(OnPreventCollide);
            SubscribeLocalEvent<SharedDisposalUnitComponent, CanDragDropOnEvent>(OnCanDragDropOn);
        }

        private void OnPreventCollide(EntityUid uid, SharedDisposalUnitComponent component, PreventCollideEvent args)
        {
            var otherBody = args.BodyB.Owner;

            // Items dropped shouldn't collide but items thrown should
            if (EntityManager.HasComponent<SharedItemComponent>(otherBody) &&
                !EntityManager.HasComponent<ThrownItemComponent>(otherBody))
            {
                args.Cancel();
                return;
            }

            if (component.RecentlyEjected.Contains(otherBody))
            {
                args.Cancel();
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
            if (!EntityManager.TryGetComponent(entity, out SharedItemComponent? storable) &&
                !EntityManager.HasComponent<SharedBodyComponent>(entity))
            {
                return false;
            }


            if (!EntityManager.TryGetComponent(entity, out IPhysBody? physics) ||
                !physics.CanCollide && storable == null)
            {
                if (!(EntityManager.TryGetComponent(entity, out MobStateComponent? damageState) && damageState.IsDead()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
