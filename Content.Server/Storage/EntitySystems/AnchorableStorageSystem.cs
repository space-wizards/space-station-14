using System.Linq;
using Content.Server.Popups;
using Content.Shared.Construction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
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
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

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

        if (!TryComp<StorageComponent>(ent.Owner, out var storage) || ent.Comp.StorageBlacklist == null)
            return;

        foreach (var item in storage.StoredItems.Keys)
        {
            if (_whitelist.IsBlacklistPassOrNull(ent.Comp.StorageBlacklist, item))
                _container.RemoveEntity(ent.Owner, item);
        }
    }

    private void OnAnchorAttempt(Entity<AnchorableStorageComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // No other stashes or anchored items?
        if (!CheckOverlap((ent, ent.Comp, Transform(ent))))
            return;

        _popup.PopupEntity(Loc.GetString("anchored-storage-already-present"), ent, args.User);
        args.Cancel();
    }

    private void OnInsertAttempt(Entity<AnchorableStorageComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_whitelist.IsBlacklistPassOrNull(ent.Comp.StorageBlacklist, args.EntityUid))
        {
            args.Cancel();
        }

        if (Transform(ent.Owner).Anchored)
            args.Cancel();
    }

    /// <summary>
    ///     Check the overlap of this entity on its tile.
    /// </summary>
    /// <param name="ent">Entity to check.</param>
    /// <returns>True if there isn't any overlap, and false if there is.</returns>
    [PublicAPI]
    public bool CheckOverlap(Entity<AnchorableStorageComponent?, TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return false;

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
