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

        public abstract StrapPosition Position { get; protected set; }
    }

    [Serializable, NetSerializable]
    public sealed class StrapComponentState : ComponentState
    {
        public readonly StrapPosition Position;

        public StrapComponentState(StrapPosition position) : base(ContentNetIDs.BUCKLE)
        {
            Position = position;
        }

        public bool Buckled { get; }
    }

    [Serializable, NetSerializable]
    public enum StrapVisuals
    {
        RotationAngle
    }
}
