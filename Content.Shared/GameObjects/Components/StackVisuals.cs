#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum StackVisuals : byte
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
