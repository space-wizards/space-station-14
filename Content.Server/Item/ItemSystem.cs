using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Stacks;
using Content.Shared.Storage;

namespace Content.Server.Item;

public sealed class ItemSystem : SharedItemSystem
{
    [Dependency] private readonly StorageSystem _storage = default!;

    protected override void OnStackCountChanged(EntityUid uid, ItemComponent component, StackCountChangedEvent args)
    {
        base.OnStackCountChanged(uid, component, args);

        if (!Container.TryGetContainingContainer(uid, out var container) ||
            !TryComp<StorageComponent>(container.Owner, out var storage))
            return;

        _storage.RecalculateStorageUsed(container.Owner, storage);
        _storage.UpdateUI(container.Owner, storage);
    }
}
