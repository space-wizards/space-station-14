using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;

namespace Content.Shared.GameObjects
{
    public abstract class SharedHandsComponent : Component
    {
        public sealed override string Name => "Hands";
        public sealed override uint? NetID => ContentNetIDs.HANDS;
        public sealed override Type StateType => typeof(HandsComponentState);
    }

    // The IDs of the items get synced over the network.
    [Serializable]
    public class HandsComponentState : ComponentState
    {
        public readonly Dictionary<string, int> Hands;

        public HandsComponentState(Dictionary<string, int> hands)
        {
            Hands = hands;
        }
    }
}
