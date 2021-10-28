using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Melee
{
    /// <summary>
    ///     Raised directed on the used entity when a target entity is click attacked by a user.
    /// </summary>
    public class ClickAttackEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity used to attack, for broadcast purposes.
        /// </summary>
        public IEntity Used { get; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        /// <summary>
        ///     UID of the entity that was attacked.
        /// </summary>
        public EntityUid Target { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity? TargetEntity { get; }

        public ClickAttackEvent(IEntity used, IEntity user, EntityCoordinates clickLocation, EntityUid target = default)
        {
            Used = used;
            User = user;
            ClickLocation = clickLocation;
            Target = target;

            IoCManager.Resolve<IEntityManager>().TryGetEntity(Target, out var targetEntity);
            TargetEntity = targetEntity;
        }
    }

    /// <summary>
    ///     Raised directed on the used entity when a target entity is wide attacked by a user.
    /// </summary>
    public class WideAttackEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity used to attack, for broadcast purposes.
        /// </summary>
        public IEntity Used { get; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        public WideAttackEvent(IEntity used, IEntity user, EntityCoordinates clickLocation)
        {
            Used = used;
            User = user;
            ClickLocation = clickLocation;
        }
    }

    /// <summary>
    ///     Event raised on entities that have been attacked.
    /// </summary>
    public class AttackedEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity used to attack, for broadcast purposes.
        /// </summary>
        public IEntity Used { get; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        public AttackedEvent(IEntity used, IEntity user, EntityCoordinates clickLocation)
        {
            Used = used;
            User = user;
            ClickLocation = clickLocation;
        }
    }
}
