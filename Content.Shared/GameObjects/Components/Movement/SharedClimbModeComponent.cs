using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Movement
{
    public class SharedClimbModeComponent : Component
    {
        public sealed override string Name => "ClimbMode";
        public sealed override uint? NetID => ContentNetIDs.CLIMB_MODE;

        [Serializable, NetSerializable]
        protected sealed class ClimbModeComponentState : ComponentState
        {
            public ClimbModeComponentState(bool climbing) : base(ContentNetIDs.CLIMB_MODE)
            {
                Climbing = climbing;
            }

            public bool Climbing { get; }
        }
    }
}
