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
    /// A message that calls the use interaction on an item in hand, presumed for now the interaction will occur only on the active hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class UseInHandMsg : ComponentMessage
    {
        public UseInHandMsg()
        {
            Directed = true;
        }
    }

    /// <summary>
    /// A message that calls the activate interaction on the item in Index.
    /// </summary>
    [Serializable, NetSerializable]
    public class ActivateInHandMsg : ComponentMessage
    {
        public string Index { get; }

        public ActivateInHandMsg(string index)
        {
            Directed = true;
            Index = index;
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
