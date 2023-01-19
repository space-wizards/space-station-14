using System.Threading;

namespace Content.Shared.Storage.Components
{
    /// <summary>
    /// Lets you dump this container on the ground using a verb,
    /// or when interacting with it on a disposal unit or placeable surface.
    /// </summary>
    [RegisterComponent]
    public sealed class DumpableComponent : Component
    {
        /// <summary>
        /// How long each item adds to the doafter.
        /// </summary>
        [DataField("delayPerItem")]
        public TimeSpan DelayPerItem = TimeSpan.FromSeconds(0.2);

        /// <summary>
        /// Cancellation token for the doafter.
        /// </summary>
        public CancellationTokenSource? CancelToken;
    }
}
