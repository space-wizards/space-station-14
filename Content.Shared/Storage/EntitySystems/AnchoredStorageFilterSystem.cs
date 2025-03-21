using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Storage.EntitySystems;

public sealed class AnchoredStorageFilterSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnchoredStorageFilterComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<AnchoredStorageFilterComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

    private void OnAnchorStateChanged(Entity<AnchoredStorageFilterComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (!TryComp<StorageComponent>(ent.Owner, out var storage))
            return;

        foreach (var item in storage.StoredItems.Keys)
        {
            if (!_whitelist.CheckBoth(item, ent.Comp.Blacklist, ent.Comp.Whitelist))
                _container.RemoveEntity(ent.Owner, item);
        }
    }

    private void OnInsertAttempt(Entity<AnchoredStorageFilterComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (Transform(ent).Anchored && !_whitelist.CheckBoth(args.EntityUid, ent.Comp.Blacklist, ent.Comp.Whitelist))
        {
            args.Cancel();
            return;
        }
    }
}
