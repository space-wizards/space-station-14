using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Containers;

public sealed class InsertOnDragSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InsertOnDragComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<InsertOnDragComponent, InsertOnDragDoAfterEvent>(OnDragDoAfter);
    }

    private void OnDragDropTarget(EntityUid uid, InsertOnDragComponent component, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        var doAfterArgs = new DoAfterArgs(uid, component.Delay, new InsertOnDragDoAfterEvent(), uid, target: args.Dragged)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDragDoAfter(EntityUid uid, InsertOnDragComponent component, InsertOnDragDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        if (!_containerSystem.TryGetContainer(uid, component.Container, out var container))
            return;

        var target = args.Args.Target.Value;
        container.Insert(target, EntityManager);
        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed class InsertOnDragDoAfterEvent : SimpleDoAfterEvent
{
}
