using Content.Shared.Climbing.Systems;
using Content.Shared.DragDrop;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Containers;

public sealed class DragInsertContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragInsertContainerComponent, DragDropTargetEvent>(OnDragDropOn, before: new []{ typeof(ClimbSystem)});
        SubscribeLocalEvent<DragInsertContainerComponent, CanDropTargetEvent>(OnCanDragDropOn);
    }

    private void OnDragDropOn(Entity<DragInsertContainerComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        TryInsert(ent, args.User, args.Dragged);
        args.Handled = true;
    }

    private void OnCanDragDropOn(Entity<DragInsertContainerComponent> ent, ref CanDropTargetEvent args)
    {
        var (_, comp) = ent;
        if (!_container.TryGetContainer(ent, comp.ContainerId, out var container))
            return;

        args.Handled = true;
        args.CanDrop |= _container.CanInsert(args.Dragged, container);
    }

    [PublicAPI]
    private bool TryInsert(Entity<DragInsertContainerComponent> ent, EntityUid user, EntityUid dragged)
    {
        var (_, comp) = ent;
        if (!_container.TryGetContainer(ent, comp.ContainerId, out var container))
            return false;

        return _container.Insert(dragged, container);
    }
}
