using Robust.Shared.Physics;

namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Base class for all throw events.
    /// </summary>
    public abstract class ThrowEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     The entity that threw <see cref="Thrown"/>.
        /// </summary>
        public EntityUid? User { get; }

        /// <summary>
        ///     The entity thrown by <see cref="User"/> that hit <see cref="Target"/>
        /// </summary>
        public EntityUid Thrown { get; }

        /// <summary>
        ///     The entity hit with <see cref="Thrown"/> by <see cref="User"/>
        /// </summary>
        public EntityUid Target { get; }

        public ThrowEvent(EntityUid? user, EntityUid thrown, EntityUid target)
        {
            User = user;
            Thrown = thrown;
            Target = target;
        }
    }

    /// <summary>
    ///     Raised directed on the target entity being hit by the thrown entity.
    /// </summary>
    [ByRefEvent]
    public record struct ThrowHitByEvent
    {
        public EntityUid? User;
        public EntityUid Thrown;
        public EntityUid Target;

        public ThrowHitByEvent(EntityUid? user, EntityUid thrown, EntityUid target)
        {
            User = user;
            Thrown = thrown;
            Target = target;
        }

        public ThrowHitByEvent(EntityUid? user, IPhysBody thrown, IPhysBody target)
        {
            User = user;
            Thrown = thrown.Owner;
            Target = target.Owner;
        }
    }

    /// <summary>
    ///     Raised directed on the thrown entity that hits another. 'User' is whoever threw it, if anyone.
    /// </summary>
    [ByRefEvent]
    public record struct ThrowDoHitEvent
    {
        public EntityUid? User;
        public EntityUid Thrown;
        public EntityUid Target;
        public bool StopCollisions = false;
        public bool StopMoving = false;

        public ThrowDoHitEvent(EntityUid? user, EntityUid thrown, EntityUid target)
        {
            User = user;
            Thrown = thrown;
            Target = target;
        }

        public ThrowDoHitEvent(EntityUid? user, IPhysBody thrown, IPhysBody target)
        {
            User = user;
            Thrown = thrown.Owner;
            Target = target.Owner;
        }
    }
}
