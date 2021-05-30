#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Zones the player can target for attacks.
    /// </summary>
    [Serializable, NetSerializable]
    public enum TargetingZone
    {
        /// <summary>
        ///     Torso/arm area.
        /// </summary>
        Middle,

        /// <summary>
        ///     Legs/groin area.
        /// </summary>
        Low,

        /// <summary>
        ///     Go for the head.
        /// </summary>
        High
    }
}
