using System.Threading;

namespace Content.Server.Medical.Components
{
    /// <summary>
    /// Used to let doctors use the stethoscope on people.
    /// </summary>
    [RegisterComponent]
    public sealed partial class WearingStethoscopeComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        [DataField("delay")]
        public float Delay = 2.5f;

        public EntityUid Stethoscope = default!;
    }
}
