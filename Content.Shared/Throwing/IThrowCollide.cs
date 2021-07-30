using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Throwing
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
        public IEntity? User { get; }

        /// <summary>
        ///     The entity thrown by <see cref="User"/> that hit <see cref="Target"/>
        /// </summary>
        public IEntity Thrown { get; }

        /// <summary>
        ///     The entity hit with <see cref="Thrown"/> by <see cref="User"/>
        /// </summary>
        public IEntity Target { get; }

        public ThrowCollideEventArgs(IEntity? user, IEntity thrown, IEntity target)
        {
            User = user;
            Thrown = thrown;
            Target = target;
        }
    }

    public class ThrowCollideEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     The entity that threw <see cref="Thrown"/>.
        /// </summary>
        public IEntity? User { get; }

        /// <summary>
        ///     The entity thrown by <see cref="User"/> that hit <see cref="Target"/>
        /// </summary>
        public IEntity Thrown { get; }

        /// <summary>
        ///     The entity hit with <see cref="Thrown"/> by <see cref="User"/>
        /// </summary>
        public IEntity Target { get; }

        public ThrowCollideEvent(IEntity? user, IEntity thrown, IEntity target)
        {
            User = user;
            Thrown = thrown;
            Target = target;
        }
    }
}
