using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interaction
{
    /// <summary>
    ///     This interface gives components behavior when they're dropped by a mob.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IDropped
    {
        [Obsolete("Use DroppedMessage instead")]
        void Dropped(DroppedEventArgs eventArgs);
    }

    public class DroppedEventArgs : EventArgs
    {
        public DroppedEventArgs(EntityUid user)
        {
            User = user;
        }

        public EntityUid User { get; }
    }

    /// <summary>
    ///     Raised when an entity is dropped
    /// </summary>
    [PublicAPI]
    public class DroppedEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that dropped the item.
        /// </summary>
        public EntityUid UserUid { get; }

        /// <summary>
        ///     Item that was dropped.
        /// </summary>
        public EntityUid DroppedUid { get; }

        public DroppedEvent(EntityUid user, EntityUid dropped)
        {
            UserUid = user;
            DroppedUid = dropped;
        }
    }
}
