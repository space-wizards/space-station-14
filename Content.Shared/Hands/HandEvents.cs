using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Hands
{
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
}
