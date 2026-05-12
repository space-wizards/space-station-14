using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;

namespace Content.Shared.DragDrop;

public abstract class SharedDragDropSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<DragDropRequestEvent>(OnDragDropRequestEvent);
    }

    private void OnDragDropRequestEvent(DragDropRequestEvent msg, EntitySessionEventArgs args)
    {
        var dragged = GetEntity(msg.Dragged);
        var target = GetEntity(msg.Target);

        if (Deleted(dragged) || Deleted(target))
            return;

        var user = args.SenderSession.AttachedEntity;

        if (user == null || !_actionBlockerSystem.CanInteract(user.Value, target))
            return;

        // must be in range of both the target and the object they are drag / dropping
        // Client also does this check but ya know we gotta validate it.
        if (!_interaction.InRangeUnobstructed(user.Value, dragged, popup: true)
            || !_interaction.InRangeUnobstructed(user.Value, target, popup: true))
        {
            return;
        }

        var dragArgs = new DragDropDraggedEvent(user.Value, target);

        // trigger dragdrops on the dropped entity
        RaiseLocalEvent(dragged, ref dragArgs);

        if (dragArgs.Handled)
            return;

        var dropArgs = new DragDropTargetEvent(user.Value, dragged);

        // trigger dragdrops on the target entity (what you are dropping onto)
        RaiseLocalEvent(GetEntity(msg.Target), ref dropArgs);
    }
}
