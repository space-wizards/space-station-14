using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// Ejects items that do not match a <see cref="EntityWhitelist"/> from a storage when it is anchored.
/// <seealso cref="AnchoredStorageFilterComponent"/>
/// </summary>
public sealed class AnchoredStorageFilterSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnchoredStorageFilterComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<AnchoredStorageFilterComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

    /// <summary>
    /// Handles the <see cref="AnchorStateChangedEvent"/>.
    /// </summary>
    private void OnAnchorStateChanged(Entity<AnchoredStorageFilterComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (!TryComp<StorageComponent>(ent, out var storage))
            return;

        foreach (var item in storage.StoredItems.Keys)
        {
            if (!_whitelist.CheckBoth(item, ent.Comp.Blacklist, ent.Comp.Whitelist))
                _container.RemoveEntity(ent, item);
        }
    }

    /// <summary>
    /// Handles the <see cref="ContainerIsInsertingAttemptEvent"/>.
    /// </summary>
    private void OnInsertAttempt(Entity<AnchoredStorageFilterComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (Transform(ent).Anchored && !_whitelist.CheckBoth(args.EntityUid, ent.Comp.Blacklist, ent.Comp.Whitelist))
            args.Cancel();
    }
}
