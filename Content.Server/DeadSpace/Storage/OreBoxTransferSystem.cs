// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Storage.Components;
using Content.Shared.DeadSpace.Prison;
using Content.Shared.DragDrop;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Server.DeadSpace.Storage;

public sealed class OreBoxTransferSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OreBoxComponent, DragDropDraggedEvent>(OnDragDropDragged);
    }

    private void OnDragDropDragged(EntityUid uid, OreBoxComponent comp, ref DragDropDraggedEvent args)
    {
        if (args.Handled) return;

        if (HasComp<PrisonOreProcessorComponent>(args.Target))
        {
            var deposit = new PrisonOreBoxDepositEvent(uid, args.User);
            RaiseLocalEvent(args.Target, ref deposit);
            args.Handled = true;
            return;
        }

        if (!HasComp<LatheComponent>(args.Target)) return;

        if (!_container.TryGetContainer(uid, "storagebase", out var container))
            return;

        var allOres = container.ContainedEntities.ToArray();

        foreach (var ore in allOres)
        {
            _materialStorage.TryInsertMaterialEntity(uid, ore, args.Target);
        }

        args.Handled = true;
    }
}
