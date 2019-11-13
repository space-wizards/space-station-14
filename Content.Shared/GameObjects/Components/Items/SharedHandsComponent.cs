using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects
{
    public abstract class SharedHandsComponent : Component
    {
        public sealed override string Name => "Hands";
        public sealed override uint? NetID => ContentNetIDs.HANDS;
        public sealed override Type StateType => typeof(HandsComponentState);
    }

    // The IDs of the items get synced over the network.
    [Serializable, NetSerializable]
    public class HandsComponentState : ComponentState
    {
        public readonly Dictionary<string, EntityUid> Hands;
        public readonly string ActiveIndex;

        public HandsComponentState(Dictionary<string, EntityUid> hands, string activeIndex) : base(ContentNetIDs.HANDS)
        {
            Hands = hands;
            ActiveIndex = activeIndex;
        }
    }

    /// <summary>
    /// A message that activates the inhand, presumed for now the activation will occur only on the active hand
    /// </summary>
    [Serializable, NetSerializable]
    public class ActivateInhandMsg : ComponentMessage
    {
        public ActivateInhandMsg()
        {
            Directed = true;
        }
    }

    [Serializable, NetSerializable]
    public class ClientAttackByInHandMsg : ComponentMessage
    {
        public string Index { get; }

        public ClientAttackByInHandMsg(string index)
        {
            Directed = true;
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public class ClientChangedHandMsg : ComponentMessage
    {
        public string Index { get; }

        public ClientChangedHandMsg(string index)
        {
            Directed = true;
            Index = index;
        }
    }
}
