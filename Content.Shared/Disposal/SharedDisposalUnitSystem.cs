using System;
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
    }
}
