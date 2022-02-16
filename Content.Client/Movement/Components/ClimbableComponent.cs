using Content.Shared.Climbing;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.GameObjects;

namespace Content.Client.Movement.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IClimbable))]
    public sealed class ClimbableComponent : SharedClimbableComponent
    {
        public override bool CanDragDropOn(DragDropEvent eventArgs)
        {
            if (!base.CanDragDropOn(eventArgs))
                return false;

            var user = eventArgs.User;
            var target = eventArgs.Target;
            var dragged = eventArgs.Dragged;
            bool Ignored(EntityUid entity) => entity == target || entity == user || entity == dragged;

            var sys = EntitySystem.Get<SharedInteractionSystem>();

            return sys.InRangeUnobstructed(user, target, Range, predicate: Ignored)
                && sys.InRangeUnobstructed(user, dragged, Range, predicate: Ignored);
        }

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return false;
        }
    }
}
