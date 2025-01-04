using System.Linq;
using Content.Server.Popups;
using Content.Shared.Construction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Item.PseudoItem;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;

namespace Content.Server.Storage;

/// <summary>
/// This is used for restricting anchor operations on storage (one bag max per tile)
/// and ejecting living contents on anchor.
/// </summary>
public sealed class AnchorableStorageSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AnchorableStorageComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<AnchorableStorageComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<AnchorableStorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

    private void OnAnchorStateChanged(Entity<AnchorableStorageComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (CheckOverlap((ent, ent.Comp, Transform(ent))))
        {
            _popup.PopupEntity(Loc.GetString("anchored-storage-already-present"), ent);
            _xform.Unanchor(ent, Transform(ent));
            return;
        }

        // Eject any sapient creatures inside the storage.
        // Does not recurse down into bags in bags - player characters are the largest concern, and they'll only fit in duffelbags.
        if (!TryComp(ent.Owner, out StorageComponent? storage))
            return;

        var entsToRemove = storage.StoredItems.Keys.Where(storedItem =>
                HasComp<MindContainerComponent>(storedItem)
                || HasComp<PseudoItemComponent>(storedItem)
            ).ToList();

        foreach (var removeUid in entsToRemove)
            _container.RemoveEntity(ent.Owner, removeUid);
    }

    private void OnAnchorAttempt(Entity<AnchorableStorageComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Nothing around? We can anchor without issue.
        if (!CheckOverlap((ent, ent.Comp, Transform(ent))))
            return;

        _popup.PopupEntity(Loc.GetString("anchored-storage-already-present"), ent, args.User);
        args.Cancel();
    }

    private void OnInsertAttempt(Entity<AnchorableStorageComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Check for living things, they should not insert when anchored.
        if (!HasComp<MindContainerComponent>(args.EntityUid) && !HasComp<PseudoItemComponent>(args.EntityUid))
            return;

        if (Transform(ent.Owner).Anchored)
            args.Cancel();
    }

    [PublicAPI]
    public bool CheckOverlap(EntityUid uid)
    {
        if (!TryComp(uid, out AnchorableStorageComponent? comp))
            return false;

        return CheckOverlap((uid, comp, Transform(uid)));
    }

    public bool CheckOverlap(Entity<AnchorableStorageComponent, TransformComponent> ent)
    {
        if (ent.Comp2.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var indices = _map.TileIndicesFor(grid, gridComp, ent.Comp2.Coordinates);
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid, gridComp, indices);

        while (enumerator.MoveNext(out var otherEnt))
        {
            // Don't match yourself.
            if (otherEnt == ent.Owner)
                continue;

            // Is another storage entity is already anchored here?
            if (HasComp<AnchorableStorageComponent>(otherEnt))
                return true;
        }

        return false;
    }
}
