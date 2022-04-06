using Content.Shared.Storage;

namespace Content.Client.Storage;

// TODO kill this is all horrid.
public sealed class StorageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<StorageHeldItemsEvent>(OnStorageHeldItems);
        SubscribeNetworkEvent<OpenStorageUIEvent>(OnOpenStorageUI);
        SubscribeNetworkEvent<CloseStorageUIEvent>(OnCloseStorageUI);
        SubscribeNetworkEvent<AnimateInsertingEntitiesEvent>(OnAnimateInsertingEntities);
    }

    private void OnStorageHeldItems(StorageHeldItemsEvent ev)
    {
        if (TryComp<ClientStorageComponent>(ev.Storage, out var storage))
        {
            storage.HandleStorageMessage(ev);
        }
    }

    private void OnOpenStorageUI(OpenStorageUIEvent ev)
    {
        if (TryComp<ClientStorageComponent>(ev.Storage, out var storage))
        {
            storage.ToggleUI();
        }
    }

    private void OnCloseStorageUI(CloseStorageUIEvent ev)
    {
        if (TryComp<ClientStorageComponent>(ev.Storage, out var storage))
        {
            storage.CloseUI();
        }
    }

    private void OnAnimateInsertingEntities(AnimateInsertingEntitiesEvent ev)
    {
        if (TryComp<ClientStorageComponent>(ev.Storage, out var storage))
        {
            storage.HandleAnimatingInsertingEntities(ev);
        }
    }
}
