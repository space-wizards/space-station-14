using JetBrains.Annotations;

namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Raised when throwing the entity in your hands.
    /// </summary>
    [PublicAPI]
    public sealed class ThrownEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity that threw the item.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Item that was thrown.
        /// </summary>
        public EntityUid Thrown { get; }

        public ThrownEvent(EntityUid user, EntityUid thrown)
        {
            User = user;
            Thrown = thrown;
        }
    }
}
