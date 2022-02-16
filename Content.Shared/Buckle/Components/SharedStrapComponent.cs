using System;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
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
        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(eventArgs.Dragged, out SharedBuckleComponent? buckleComponent)) return false;
            bool Ignored(EntityUid entity) => entity == eventArgs.User || entity == eventArgs.Dragged || entity == eventArgs.Target;

            return EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(eventArgs.Target, eventArgs.Dragged, buckleComponent.Range, predicate: Ignored);
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
}
