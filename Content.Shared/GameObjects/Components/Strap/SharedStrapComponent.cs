#nullable enable
using System;
using Content.Shared.GameObjects.Components.Buckle;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Strap
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

    public abstract class SharedStrapComponent : Component, IDragDropOn
    {
        public sealed override string Name => "Strap";

        public sealed override uint? NetID => ContentNetIDs.STRAP;

        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
            if (!eventArgs.Dragged.TryGetComponent(out SharedBuckleComponent? buckleComponent)) return false;
            bool Ignored(IEntity entity) => entity == eventArgs.User || entity == eventArgs.Dragged || entity == eventArgs.Target;

            return eventArgs.Target.InRangeUnobstructed(eventArgs.Dragged, buckleComponent.Range, predicate: Ignored);
        }

        public abstract bool DragDropOn(DragDropEventArgs eventArgs);
    }

    [Serializable, NetSerializable]
    public sealed class StrapComponentState : ComponentState
    {
        public StrapComponentState(StrapPosition position) : base(ContentNetIDs.BUCKLE)
        {
            Position = position;
        }

        /// <summary>
        /// The change in position that this strap makes to the strapped mob
        /// </summary>
        public StrapPosition Position { get; }
    }

    [Serializable, NetSerializable]
    public enum StrapVisuals
    {
        RotationAngle,
        BuckledState
    }

    // TODO : Convert this to an Entity Message. Careful, it will Break ShuttleControllerComponent (only place where it's used)
    [Serializable, NetSerializable]
    public abstract class StrapChangeMessage : ComponentMessage
    {
        /// <summary>
        ///     Constructs a new instance of <see cref="StrapChangeMessage"/>
        /// </summary>
        /// <param name="entity">The entity that had its buckling status changed</param>
        /// <param name="strap">The strap that the entity was buckled to or unbuckled from</param>
        /// <param name="buckled">True if the entity was buckled, false otherwise</param>
        protected StrapChangeMessage(IEntity entity, IEntity strap, bool buckled)
        {
            Entity = entity;
            Strap = strap;
            Buckled = buckled;
        }

        /// <summary>
        ///     The entity that had its buckling status changed
        /// </summary>
        public IEntity Entity { get; }

        /// <summary>
        ///     The strap that the entity was buckled to or unbuckled from
        /// </summary>
        public IEntity Strap { get; }

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
        public StrapMessage(IEntity entity, IEntity strap) : base(entity, strap, true)
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
        public UnStrapMessage(IEntity entity, IEntity strap) : base(entity, strap, false)
        {
        }
    }
}
