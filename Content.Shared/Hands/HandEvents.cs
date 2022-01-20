using System;
using System.Collections.Generic;
using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Content.Shared.Hands
{
    /// <summary>
    ///     Raised directed at an item that needs to update its in-hand sprites/layers.
    /// </summary>
    public class GetInhandVisualsEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that owns the hand holding the item.
        /// </summary>
        public readonly EntityUid User;

        public readonly HandLocation Location;

        /// <summary>
        ///     The layers that will be added to the entity that is holding this item.
        /// </summary>
        /// <remarks>
        ///     Note that the actual ordering of the layers depends on the order in which they are added to this list;
        /// </remarks>
        public List<(string, PrototypeLayerData)> Layers = new();

        public GetInhandVisualsEvent(EntityUid user, HandLocation location)
        {
            User = user;
            Location = location;
        }
    }

    /// <summary>
    ///     Raised directed at an item after its visuals have been updated.
    /// </summary>
    /// <remarks>
    ///     Useful for systems/components that modify the visual layers that an item adds to a player. (e.g. RGB memes)
    /// </remarks>
    public class HeldVisualsUpdatedEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that is holding the item.
        /// </summary>
        public readonly EntityUid User;

        /// <summary>
        ///     The layers that this item is now revealing.
        /// </summary>
        public HashSet<string> RevealedLayers;

        public HeldVisualsUpdatedEvent(EntityUid user, HashSet<string> revealedLayers)
        {
            User = user;
            RevealedLayers = revealedLayers;
        }
    }

    /// <summary>
    ///     Raised when an entity item in a hand is deselected.
    /// </summary>
    [PublicAPI]
    public class HandDeselectedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that owns the deselected hand.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Item in the hand that was deselected.
        /// </summary>
        public EntityUid Item { get; }

        public HandDeselectedEvent(EntityUid user, EntityUid item)
        {
            User = user;
            Item = item;
        }
    }

    /// <summary>
    ///     Raised when an item entity held by a hand is selected.
    /// </summary>
    [PublicAPI]
    public class HandSelectedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that owns the selected hand.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Item in the hand that was selected.
        /// </summary>
        public EntityUid Item { get; }

        public HandSelectedEvent(EntityUid user, EntityUid item)
        {
            User = user;
            Item = item;
        }
    }

    [Serializable, NetSerializable]
    public class RequestSetHandEvent : EntityEventArgs
    {
        /// <summary>
        ///     The hand to be swapped to.
        /// </summary>
        public string HandName { get; }

        public RequestSetHandEvent(string handName)
        {
            HandName = handName;
        }
    }

    [Serializable, NetSerializable]
    public class PickupAnimationEvent : EntityEventArgs
    {
        public EntityUid ItemUid { get; }
        public EntityCoordinates InitialPosition { get; }
        public Vector2 FinalPosition { get; }

        public PickupAnimationEvent(EntityUid itemUid, EntityCoordinates initialPosition,
            Vector2 finalPosition)
        {
            ItemUid = itemUid;
            FinalPosition = finalPosition;
            InitialPosition = initialPosition;
        }
    }

    /// <summary>
    ///     Raised directed on both the blocking entity and user when
    ///     a virtual hand item is deleted.
    /// </summary>
    public class VirtualItemDeletedEvent : EntityEventArgs
    {
        public EntityUid BlockingEntity;
        public EntityUid User;

        public VirtualItemDeletedEvent(EntityUid blockingEntity, EntityUid user)
        {
            BlockingEntity = blockingEntity;
            User = user;
        }
    }

    /// <summary>
    ///     Raised when putting an entity into a hand slot
    /// </summary>
    [PublicAPI]
    public class EquippedHandEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that equipped the item.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Item that was equipped.
        /// </summary>
        public EntityUid Equipped { get; }

        /// <summary>
        ///     Hand that the item was placed into.
        /// </summary>
        public Hand Hand { get; }

        public EquippedHandEvent(EntityUid user, EntityUid equipped, Hand hand)
        {
            User = user;
            Equipped = equipped;
            Hand = hand;
        }
    }

    /// <summary>
    ///     Raised when removing an entity from an inventory slot.
    /// </summary>
    [PublicAPI]
    public class UnequippedHandEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that equipped the item.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Item that was unequipped.
        /// </summary>
        public EntityUid Unequipped { get; }

        /// <summary>
        ///     Hand that the item is removed from.
        /// </summary>
        public Hand Hand { get; }

        public UnequippedHandEvent(EntityUid user, EntityUid unequipped, Hand hand)
        {
            User = user;
            Unequipped = unequipped;
            Hand = hand;
        }
    }
}
