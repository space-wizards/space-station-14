using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    /// <summary>
    ///     This interface gives components behavior when landing after being thrown.
    /// </summary>
    public interface ILand
    {
        void Land(LandEventArgs eventArgs);
    }

    public class LandEventArgs : EventArgs
    {
        public LandEventArgs(IEntity user, GridCoordinates landingLocation)
        {
            User = user;
            LandingLocation = landingLocation;
        }

        public IEntity User { get; }
        public GridCoordinates LandingLocation { get; }
    }

    /// <summary>
    ///     Raised when an entity that was thrown lands.
    /// </summary>
    [PublicAPI]
    public class LandMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that threw the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was thrown.
        /// </summary>
        public IEntity Thrown { get; }

        /// <summary>
        ///     Location where the item landed.
        /// </summary>
        public GridCoordinates LandLocation { get; }

        public LandMessage(IEntity user, IEntity thrown, GridCoordinates landLocation)
        {
            User = user;
            Thrown = thrown;
            LandLocation = landLocation;
        }
    }
}
