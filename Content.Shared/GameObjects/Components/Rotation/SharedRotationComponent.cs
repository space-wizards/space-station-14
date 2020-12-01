using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Rotation
{
    [Serializable, NetSerializable]
    public enum RotationVisuals : byte
    {
        RotationState
    }

    [Serializable, NetSerializable]
    public enum RotationState : byte
    {
        /// <summary>
        ///     Standing up
        /// </summary>
        Vertical,

        /// <summary>
        ///     Laying down
        /// </summary>
        Horizontal,
    }
}
