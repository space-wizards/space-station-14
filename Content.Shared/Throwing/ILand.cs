using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Raised when an entity that was thrown lands.
    /// </summary>
    [PublicAPI]
    public sealed class LandEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that threw the item.
        /// </summary>
        public IEntity? User { get; }

        /// <summary>
        ///     Item that was thrown.
        /// </summary>
        public IEntity Thrown { get; }

        /// <summary>
        ///     Location where the item landed.
        /// </summary>
        public EntityCoordinates LandLocation { get; }

        public LandEvent(IEntity? user, IEntity thrown, EntityCoordinates landLocation)
        {
            User = user;
            Thrown = thrown;
            LandLocation = landLocation;
        }
    }
}
