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
        bool IDragDropOn.CanDragDropOn(DragDropEvent args)
        {
            var ent = IoCManager.Resolve<IEntityManager>();
            return args.Target != args.Dragged &&
                args.Target == args.User &&
                ent.HasComponent<SharedStrippableComponent>(args.Dragged) &&
                ent.HasComponent<SharedHandsComponent>(args.User) &&
                ent.EntitySysManager.GetEntitySystem<ActionBlockerSystem>().CanInteract(args.User, args.Dragged);
        }

        bool IDragDropOn.DragDropOn(DragDropEvent eventArgs)
        {
            // Handled by StrippableComponent
            return true;
        }
    }
}
