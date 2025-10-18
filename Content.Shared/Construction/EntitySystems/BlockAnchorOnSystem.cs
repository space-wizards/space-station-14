using Content.Shared.Construction.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;

namespace Content.Shared.Construction.EntitySystems;

/// <summary>
/// Prevents anchoring an item in the same tile as an item matching the <see cref="EntityWhitelist"/>.
/// <seealso cref="BlockAnchorOnComponent"/>
/// </summary>
public sealed class BlockAnchorOnSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockAnchorOnComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<BlockAnchorOnComponent, AnchorAttemptEvent>(OnAnchorAttempt);
    }

    /// <summary>
    /// Handles the <see cref="AnchorStateChangedEvent"/>.
    /// </summary>
    private void OnAnchorStateChanged(Entity<BlockAnchorOnComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (!HasOverlap((ent, ent.Comp, Transform(ent))))
            return;

        _popup.PopupPredicted(Loc.GetString("anchored-already-present"), ent, null);
        _xform.Unanchor(ent, Transform(ent));
    }

    /// <summary>
    /// Handles the <see cref="AnchorAttemptEvent"/>.
    /// </summary>
    private void OnAnchorAttempt(Entity<BlockAnchorOnComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasOverlap((ent, ent.Comp, Transform(ent))))
            return;

        _popup.PopupPredicted(Loc.GetString("anchored-already-present"), ent, args.User);
        args.Cancel();
    }

    /// <summary>
    /// Check if there is any anchored overlap with non whitelisted or blacklisted entities.
    /// </summary>
    /// <returns>True if there is, false if there isn't</returns>
    private bool HasOverlap(Entity<BlockAnchorOnComponent, TransformComponent> ent)
    {
        if (ent.Comp2.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var indices = _map.TileIndicesFor(grid, gridComp, ent.Comp2.Coordinates);
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid, gridComp, indices);

        while (enumerator.MoveNext(out var otherEnt))
        {
            // Don't match yourself.
            if (otherEnt == ent)
                continue;

            if (!_whitelist.CheckBoth(otherEnt, ent.Comp1.Blacklist, ent.Comp1.Whitelist))
                return true;
        }

        return false;
    }
}
