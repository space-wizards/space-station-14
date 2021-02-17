using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IClimbable))]
    public class ClimbableComponent : SharedClimbableComponent
    {
        public override bool CanDragDropOn(DragDropEventArgs eventArgs)
        {
            if (!base.CanDragDropOn(eventArgs))
                return false;

            var user = eventArgs.User;
            var target = eventArgs.Target;
            var dragged = eventArgs.Dragged;
            bool Ignored(IEntity entity) => entity == target || entity == user || entity == dragged;

            return user.InRangeUnobstructed(target, Range, predicate: Ignored) && user.InRangeUnobstructed(dragged, Range, predicate: Ignored);
        }

        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            return false;
        }
    }
}
