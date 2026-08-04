// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Interaction;
using Content.Shared.DragDrop;
using Content.Shared.Materials;
using Content.Shared.Tag;
using Content.Shared.Lathe;
using Robust.Shared.Containers;
using Content.Shared.DeadSpace.Storage.Components;
using Content.Shared.DeadSpace.Prison;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Storage.Systems;

public sealed class OreBoxSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    [ValidatePrototypeId<TagPrototype>]
    private static readonly ProtoId<TagPrototype> OreTag = "Ore";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OreBoxComponent, CanDropTargetEvent>(OnCanDropTarget);
        SubscribeLocalEvent<OreBoxComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<OreBoxComponent, CanDragEvent>(OnCanDrag);

        SubscribeLocalEvent<OreBoxComponent, CanDropDraggedEvent>(OnCanDropDragged);
    }

    private void OnCanDrag(EntityUid uid, OreBoxComponent comp, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnCanDropDragged(EntityUid uid, OreBoxComponent comp, ref CanDropDraggedEvent args)
    {
        if (args.Handled) return;

        if (!HasComp<LatheComponent>(args.Target) && !HasComp<PrisonOreProcessorComponent>(args.Target)) return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnCanDropTarget(EntityUid uid, OreBoxComponent comp, ref CanDropTargetEvent args)
    {
        if (args.Handled) return;
        if (!_tag.HasTag(args.Dragged, OreTag) && !HasComp<MaterialComponent>(args.Dragged)) return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnDragDropTarget(EntityUid uid, OreBoxComponent comp, ref DragDropTargetEvent args)
    {
        if (args.Handled) return;
        if (!_tag.HasTag(args.Dragged, OreTag) && !HasComp<MaterialComponent>(args.Dragged)) return;

        var container = _container.EnsureContainer<Container>(uid, "storagebase");
        if (_container.Insert(args.Dragged, container))
        {
            args.Handled = true;
        }
    }
}
