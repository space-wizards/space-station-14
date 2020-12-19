#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Items
{
    public abstract class SharedHandsComponent : Component, ISharedHandsComponent
    {
        public sealed override string Name => "Hands";
        public sealed override uint? NetID => ContentNetIDs.HANDS;

        /// <returns>true if the item is in one of the hands</returns>
        public abstract bool IsHolding(IEntity item);
    }

    [Serializable, NetSerializable]
    public sealed class SharedHand
    {
        public readonly int Index;
        public readonly string Name;
        public readonly EntityUid? EntityUid;
        public readonly HandLocation Location;
        public readonly bool Enabled;

        public SharedHand(int index, string name, EntityUid? entityUid, HandLocation location, bool enabled)
        {
            Index = index;
            Name = name;
            EntityUid = entityUid;
            Location = location;
            Enabled = enabled;
        }
    }

    // The IDs of the items get synced over the network.
    [Serializable, NetSerializable]
    public class HandsComponentState : ComponentState
    {
        public readonly SharedHand[] Hands;
        public readonly string? ActiveIndex;

        public HandsComponentState(SharedHand[] hands, string? activeIndex) : base(ContentNetIDs.HANDS)
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

    [Serializable, NetSerializable]
    public class HandEnabledMsg : ComponentMessage
    {
        public string Name { get; }

        public HandEnabledMsg(string name)
        {
            Name = name;
        }
    }

    [Serializable, NetSerializable]
    public class HandDisabledMsg : ComponentMessage
    {
        public string Name { get; }

        public HandDisabledMsg(string name)
        {
            Name = name;
        }
    }

    public enum HandLocation : byte
    {
        Left,
        Middle,
        Right
    }
}
