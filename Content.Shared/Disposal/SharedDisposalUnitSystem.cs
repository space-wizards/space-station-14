using System;
using Content.Shared.Body.Components;
using Content.Shared.Disposal.Components;
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
            SubscribeLocalEvent<SharedDisposalUnitComponent, PreventCollideEvent>(HandlePreventCollide);
        }

        private void HandlePreventCollide(EntityUid uid, SharedDisposalUnitComponent component, PreventCollideEvent args)
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

        public virtual bool CanInsert(SharedDisposalUnitComponent component, IEntity entity)
        {
            if (!IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(component.Owner).Anchored)
                return false;

            // TODO: Probably just need a disposable tag.
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out SharedItemComponent? storable) &&
                !IoCManager.Resolve<IEntityManager>().HasComponent<SharedBodyComponent>(entity))
            {
                return false;
            }


            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out IPhysBody? physics) ||
                !physics.CanCollide && storable == null)
            {
                if (!(IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out MobStateComponent? damageState) && damageState.IsDead()))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanInsert(SharedDisposalUnitComponent component, EntityUid entityId)
        {
            var entity = EntityManager.GetEntity(entityId);
            return CanInsert(component, entity);
        }
    }
}
