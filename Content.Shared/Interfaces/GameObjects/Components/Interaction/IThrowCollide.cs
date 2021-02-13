using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    [RequiresExplicitImplementation]
    public interface IThrowCollide
    {
        void HitBy(ThrowCollideEventArgs eventArgs) {}
        void DoHit(ThrowCollideEventArgs eventArgs) {}
    }

    public class ThrowCollideEventArgs : EventArgs
    {
        /// <summary>
        ///     The entity that threw <see cref="Thrown"/> and hit <see cref="Target"/>.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The entity thrown by <see cref="User"/> that hit <see cref="Target"/>
        /// </summary>
        public IEntity Thrown { get; }

        /// <summary>
        ///     The entity hit with <see cref="Thrown"/> by <see cref="User"/>
        /// </summary>
        public IEntity Target { get; }
        public EntityCoordinates Location { get; }

        public ThrowCollideEventArgs(IEntity user, IEntity thrown, IEntity target, EntityCoordinates location)
        {
            User = user;
            Thrown = thrown;
            Target = target;
            Location = location;
        }
    }

    public class ThrowCollideMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     The entity that threw <see cref="Thrown"/>.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The entity thrown by <see cref="User"/> that hit <see cref="Target"/>
        /// </summary>
        public IEntity Thrown { get; }

        /// <summary>
        ///     The entity hit with <see cref="Thrown"/> by <see cref="User"/>
        /// </summary>
        public IEntity Target { get; }
        public EntityCoordinates Location { get; }

        public ThrowCollideMessage(IEntity user, IEntity thrown, IEntity target, EntityCoordinates location)
        {
            User = user;
            Thrown = thrown;
            Target = target;
            Location = location;
        }
    }
}
