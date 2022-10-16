using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;

namespace Content.Shared.Strip.Components
{
    /// <summary>
    ///     Give to an entity to say they can strip another entity.
    /// </summary>
    [RegisterComponent]
    public sealed class SharedStrippingComponent : Component, IDragDropOn
    {
        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            var ent = IoCManager.Resolve<IEntityManager>();
            return eventArgs.Target != eventArgs.Dragged &&
                eventArgs.Target == eventArgs.User &&
                ent.HasComponent<SharedStrippableComponent>(eventArgs.Dragged) &&
                ent.HasComponent<SharedHandsComponent>(eventArgs.User) &&
                ent.EntitySysManager.GetEntitySystem<ActionBlockerSystem>().CanInteract(eventArgs.User, eventArgs.Dragged);
        }

        bool IDragDropOn.DragDropOn(DragDropEvent eventArgs)
        {
            // Handled by StrippableComponent
            return true;
        }
    }
}
