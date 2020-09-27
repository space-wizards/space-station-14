using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Rotation
{
    [Serializable, NetSerializable]
    public enum RotationVisuals
    {
        RotationState
    }

    [Serializable, NetSerializable]
    public enum RotationState
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
