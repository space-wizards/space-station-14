using System;
using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Shared.Disposal
{
    [UsedImplicitly]
    public abstract class SharedDisposalUnitSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;

        protected static TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<SharedDisposalUnitComponent>(true))
            {
                comp.Update(frameTime);
            }
        }
    }
}
