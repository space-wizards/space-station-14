using System;
using Content.Shared.DragDrop;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components
{
    public enum StrapPosition
    {
        /// <summary>
        /// (Default) Makes no change to the buckled mob
        /// </summary>
        None = 0,

        /// <summary>
        /// Makes the mob stand up
        /// </summary>
        Stand,

        /// <summary>
        /// Makes the mob lie down
        /// </summary>
        Down
    }

    [NetworkedComponent()]
    public abstract class SharedStrapComponent : Component, IDragDropOn
    {
        public sealed override string Name => "Strap";

        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(eventArgs.Dragged, out SharedBuckleComponent? buckleComponent)) return false;
            bool Ignored(EntityUid entity) => entity == eventArgs.User || entity == eventArgs.Dragged || entity == eventArgs.Target;

            return eventArgs.Target.InRangeUnobstructed(eventArgs.Dragged, buckleComponent.Range, predicate: Ignored);
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);
    }

    [Serializable, NetSerializable]
    public sealed class StrapComponentState : ComponentState
    {
        public StrapComponentState(StrapPosition position)
        {
            Position = position;
        }

        /// <summary>
        /// The change in position that this strap makes to the strapped mob
        /// </summary>
        public StrapPosition Position { get; }
    }

    [Serializable, NetSerializable]
    public enum StrapVisuals : byte
    {
        RotationAngle,
        BuckledState
    }

    // TODO : Convert this to an Entity Message. Careful, it will Break ShuttleControllerComponent (only place where it's used)
    [Serializable, NetSerializable]
#pragma warning disable 618
    public abstract class StrapChangeMessage : ComponentMessage
#pragma warning restore 618
    {
        /// <summary>
        ///     Constructs a new instance of <see cref="StrapChangeMessage"/>
        /// </summary>
        /// <param name="entity">The entity that had its buckling status changed</param>
        /// <param name="strap">The strap that the entity was buckled to or unbuckled from</param>
        /// <param name="buckled">True if the entity was buckled, false otherwise</param>
        protected StrapChangeMessage(EntityUid entity, EntityUid strap, bool buckled)
        {
            Entity = entity;
            Strap = strap;
            Buckled = buckled;
        }

        /// <summary>
        ///     The entity that had its buckling status changed
        /// </summary>
        public EntityUid Entity { get; }

        /// <summary>
        ///     The strap that the entity was buckled to or unbuckled from
        /// </summary>
        public EntityUid Strap { get; }

        /// <summary>
        ///     True if the entity was buckled, false otherwise.
        /// </summary>
        public bool Buckled { get; }
    }

    [Serializable, NetSerializable]
    public class StrapMessage : StrapChangeMessage
    {
        /// <summary>
        ///     Constructs a new instance of <see cref="StrapMessage"/>
        /// </summary>
        /// <param name="entity">The entity that had its buckling status changed</param>
        /// <param name="strap">The strap that the entity was buckled to or unbuckled from</param>
        public StrapMessage(EntityUid entity, EntityUid strap) : base(entity, strap, true)
        {
        }
    }

    [Serializable, NetSerializable]
    public class UnStrapMessage : StrapChangeMessage
    {
        /// <summary>
        ///     Constructs a new instance of <see cref="UnStrapMessage"/>
        /// </summary>
        /// <param name="entity">The entity that had its buckling status changed</param>
        /// <param name="strap">The strap that the entity was buckled to or unbuckled from</param>
        public UnStrapMessage(EntityUid entity, EntityUid strap) : base(entity, strap, false)
        {
        }
    }
}
