using System.Threading;

namespace Content.Server.Carrying
{
    [RegisterComponent]
    public sealed class CarriableComponent : Component
    {
        public CancellationTokenSource? CancelToken;
        /// <summary>
        ///     Number of free hands required
        ///     to carry the entity
        /// </summary>
        [DataField("freeHandsRequired")]
        public int FreeHandsRequired = 2;
    }
}
