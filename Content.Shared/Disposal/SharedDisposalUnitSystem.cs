using System;
using Content.Shared.Disposal.Components;
using Content.Shared.Item;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
            if (ComponentManager.HasComponent<SharedItemComponent>(otherBody) &&
                !ComponentManager.HasComponent<ThrownItemComponent>(otherBody))
            {
                args.Cancel();
                return;
            }

            if (component.RecentlyEjected.Contains(otherBody))
            {
                args.Cancel();
            }
        }
    }
}
