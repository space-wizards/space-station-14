using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Movement
{
    public class SharedClimbingComponent : Component
    {
        public sealed override string Name => "Climbing";
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
