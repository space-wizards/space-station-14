using System;
using Robust.Shared.Serialization;

namespace Content.Shared.PlayingCard
{
    [Serializable, NetSerializable]
    public enum PlayingCardHandVisuals : byte
    {
        /// <summary>
        /// The amount of elements in the stack
        /// </summary>
        Actual,
        /// <summary>
        /// The total amount of elements in the stack. If unspecified, the visualizer assumes
        /// its
        /// </summary>
        MaxCount,
        Hide
    }
}
