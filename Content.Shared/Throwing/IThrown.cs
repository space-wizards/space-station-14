using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Raised when throwing the entity in your hands.
    /// </summary>
    [PublicAPI]
    public class ThrownEvent : HandledEntityEventArgs
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
