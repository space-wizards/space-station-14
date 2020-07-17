using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Strap
{
    public enum StrapPosition
    {
        /// <summary>
        /// (Default) Makes no change to the buckled mob
        /// </summary>
        None = 0,

        /// <summary>
        /// Makes the mob stand up
        /// </summary>
        Stand,

        /// <summary>
        /// Makes the mob lie down
        /// </summary>
        Down
    }

    public abstract class SharedStrapComponent : Component
    {
        public sealed override string Name => "Strap";

        public sealed override uint? NetID => ContentNetIDs.STRAP;
    }

    [Serializable, NetSerializable]
    public enum StrapVisuals
    {
        RotationAngle
    }
}
