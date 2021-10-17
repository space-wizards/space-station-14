using System;
using Content.Shared.Body.Components;
using Content.Shared.Disposal.Components;
using Content.Shared.Item;
using Content.Shared.MobState;
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
            var otherBody = args.BodyB.Owner.Uid;

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
            if (!component.Owner.Transform.Anchored)
                return false;

            // TODO: Probably just need a disposable tag.
            if (!entity.TryGetComponent(out SharedItemComponent? storable) &&
                !entity.HasComponent<SharedBodyComponent>())
            {
                return false;
            }


            if (!entity.TryGetComponent(out IPhysBody? physics) ||
                !physics.CanCollide && storable == null)
            {
                if (!(entity.TryGetComponent(out IMobStateComponent? damageState) && damageState.IsDead())) {
                    return false;
                }
            }
            return true;
        }
    }
}
