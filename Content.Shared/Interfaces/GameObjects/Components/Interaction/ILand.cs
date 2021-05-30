#nullable enable
using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when landing after being thrown.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface ILand
    {
        void Land(LandEventArgs eventArgs);
    }

    public class LandEventArgs : EventArgs
    {
        public LandEventArgs(IEntity? user, EntityCoordinates landingLocation)
        {
            User = user;
            LandingLocation = landingLocation;
        }

        public IEntity? User { get; }
        public EntityCoordinates LandingLocation { get; }
    }

    /// <summary>
    ///     Raised when an entity that was thrown lands.
    /// </summary>
    [PublicAPI]
    public class LandEvent : HandledEntityEventArgs
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
