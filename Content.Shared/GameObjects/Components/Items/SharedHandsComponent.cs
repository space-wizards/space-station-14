#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Containers;
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

        /// <returns>true if the item is in one of the hands</returns>
        public abstract bool IsHolding(IEntity item);
    }

    public interface IReadOnlyHand
    {
        public string Name { get; }

        public bool Enabled { get; }

        public HandLocation Location { get; }

        public abstract IEntity? HeldEntity { get; }
    }

    public class SharedHand : IReadOnlyHand
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public HandLocation Location { get; set; }

        public IContainer Container { get; }

        public IEntity? HeldEntity => Container.ContainedEntities.FirstOrDefault();

        public SharedHand(string name, bool enabled, HandLocation location, IContainer container)
        {
            Name = name;
            Enabled = enabled;
            Location = location;
            Container = container;
        }

        public HandState ToHandState()
        {
            return new(Name, Location, Enabled);
        }
    }

    [Serializable, NetSerializable]
    public sealed class HandState
    {
        public string Name { get; }
        public HandLocation Location { get; }
        public bool Enabled { get; }

        public HandState(string name, HandLocation location, bool enabled)
        {
            Name = name;
            Location = location;
            Enabled = enabled;
        }
    }

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
        public string HandName { get; }

        public ActivateInHandMsg(string handName)
        {
            Directed = true;
            HandName = handName;
        }
    }

    [Serializable, NetSerializable]
    public class ClientAttackByInHandMsg : ComponentMessage
    {
        public string HandName { get; }

        public ClientAttackByInHandMsg(string handName)
        {
            Directed = true;
            HandName = handName;
        }
    }

    [Serializable, NetSerializable]
    public class MoveItemFromHandMsg : ComponentMessage
    {
        public string HandName { get; }

        public MoveItemFromHandMsg(string handName)
        {
            Directed = true;
            HandName = handName;
        }
    }

    [Serializable, NetSerializable]
    public class ClientChangedHandMsg : ComponentMessage
    {
        public string HandName { get; }

        public ClientChangedHandMsg(string handName)
        {
            Directed = true;
            HandName = handName;
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
