using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Hands
{
    /// <summary>
    /// Raised directed on an entity when attempting to drop its hand items.
    /// </summary>
    public sealed class DropAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid Uid;
    }

    /// <summary>
    ///     Raised directed at an item that needs to update its in-hand sprites/layers.
    /// </summary>
    public sealed class GetInhandVisualsEvent : EntityEventArgs
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
    public sealed class HeldVisualsUpdatedEvent : EntityEventArgs
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
    public sealed class HandDeselectedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that owns the deselected hand.
        /// </summary>
        public EntityUid User { get; }

        public HandDeselectedEvent(EntityUid user)
        {
            User = user;
        }
    }

    /// <summary>
    ///     Raised when an item entity held by a hand is selected.
    /// </summary>
    [PublicAPI]
    public sealed class HandSelectedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that owns the selected hand.
        /// </summary>
        public EntityUid User { get; }

        public HandSelectedEvent(EntityUid user)
        {
            User = user;
        }
    }

    [Serializable, NetSerializable]
    public sealed class RequestSetHandEvent : EntityEventArgs
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
    public sealed class PickupAnimationEvent : EntityEventArgs
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
    public sealed class VirtualItemDeletedEvent : EntityEventArgs
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
    public abstract class EquippedHandEvent : HandledEntityEventArgs
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
    public abstract class UnequippedHandEvent : HandledEntityEventArgs
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

    public sealed class GotEquippedHandEvent : EquippedHandEvent
    {
        public GotEquippedHandEvent(EntityUid user, EntityUid unequipped, Hand hand) : base(user, unequipped, hand) { }
    }

    public sealed class GotUnequippedHandEvent : UnequippedHandEvent
    {
        public GotUnequippedHandEvent(EntityUid user, EntityUid unequipped, Hand hand) : base(user, unequipped, hand) { }
    }

    public sealed class DidEquipHandEvent : EquippedHandEvent
    {
        public DidEquipHandEvent(EntityUid user, EntityUid unequipped, Hand hand) : base(user, unequipped, hand) { }
    }

    public sealed class DidUnequipHandEvent : UnequippedHandEvent
    {
        public DidUnequipHandEvent(EntityUid user, EntityUid unequipped, Hand hand) : base(user, unequipped, hand) { }
    }

    /// <summary>
    ///     Event raised by a client when they want to use the item currently held in their hands.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestUseInHandEvent : EntityEventArgs
    {
    }

    /// <summary>
    ///     Event raised by a client when they want to activate the item currently in their hands.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestActivateInHandEvent : EntityEventArgs
    {
        public string HandName { get; }

        public RequestActivateInHandEvent(string handName)
        {
            HandName = handName;
        }
    }

    /// <summary>
    ///     Event raised by a client when they want to use the currently held item on some other held item
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestHandInteractUsingEvent : EntityEventArgs
    {
        public string HandName { get; }

        public RequestHandInteractUsingEvent(string handName)
        {
            HandName = handName;
        }
    }

    /// <summary>
    ///     Event raised by a client when they want to move an item held in another hand to their currently active hand
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestMoveHandItemEvent : EntityEventArgs
    {
        public string HandName { get; }

        public RequestMoveHandItemEvent(string handName)
        {
            HandName = handName;
        }
    }

    /// <summary>
    ///     Event raised by a client when they want to alt interact with the item currently in their hands.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestHandAltInteractEvent : EntityEventArgs
    {
        public string HandName { get; }

        public RequestHandAltInteractEvent(string handName)
        {
            HandName = handName;
        }
    }

    public sealed class HandCountChangedEvent : EntityEventArgs
    {
        public HandCountChangedEvent(EntityUid sender)
        {
            Sender = sender;
        }

        public EntityUid Sender { get; }
    }
}
