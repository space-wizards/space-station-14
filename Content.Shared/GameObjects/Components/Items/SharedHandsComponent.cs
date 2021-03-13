#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Items
{
    public abstract class SharedHandsComponent : Component, ISharedHandsComponent
    {
        public sealed override string Name => "Hands";
        public sealed override uint? NetID => ContentNetIDs.HANDS;

        [DataField("pickupRange")]
        public float PickupRange { get; private set; } = 2;

        /// <returns>true if the item is in one of the hands</returns>
        public abstract bool IsHolding(IEntity item);
    }

    public abstract class SharedHand
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public HandLocation Location { get; set; }

        public SharedHand(string name, bool enabled, HandLocation location)
        {
            Name = name;
            Enabled = enabled;
            Location = location;
        }
    }

    [Serializable, NetSerializable]
    public sealed class HandState
    {
        public int Index { get; }
        public string Name { get; }
        public EntityUid? EntityUid { get; }
        public HandLocation Location { get; }
        public bool Enabled { get; }

        public HandState(int index, string name, EntityUid? entityUid, HandLocation location, bool enabled)
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
        public HandState[] Hands { get; }
        public string? ActiveHand { get; }

        public HandsComponentState(HandState[] hands, string? activeHand = null) : base(ContentNetIDs.HANDS)
        {
            Hands = hands;
            ActiveHand = activeHand;
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

    /// <summary>
    ///     Whether a hand is a left or right hand, or some other type of hand.
    /// </summary>
    public enum HandLocation : byte
    {
        Left,
        Middle,
        Right
    }

    /// <summary>
    /// Component message for displaying an animation of an entity flying towards the owner of a HandsComponent
    /// </summary>
    [Serializable, NetSerializable]
    public class AnimatePickupEntityMessage : ComponentMessage
    {
        public readonly EntityUid EntityId;
        public readonly EntityCoordinates EntityPosition;
        public AnimatePickupEntityMessage(EntityUid entity, EntityCoordinates entityPosition)
        {
            Directed = true;
            EntityId = entity;
            EntityPosition = entityPosition;
        }
    }
}
