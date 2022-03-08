using System;
using System.Threading;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Timing
{
    /// <summary>
    /// Timer that creates a cooldown each time an object is activated/used
    /// </summary>
    [RegisterComponent]
    public sealed class UseDelayComponent : Component
    {
        [ViewVariables]
        public TimeSpan LastUseTime;

        [ViewVariables]
        [DataField("delay")]
        public float Delay = 1;

        [ViewVariables]
        public float Elapsed = 0f;

        public CancellationTokenSource? CancellationTokenSource;

        public bool ActiveDelay => CancellationTokenSource is { Token: { IsCancellationRequested: false } };
    }
}
