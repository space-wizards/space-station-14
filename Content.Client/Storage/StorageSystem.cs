using Content.Shared.Storage;

namespace Content.Client.Storage;

// TODO kill this is all horrid.
public sealed class StorageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AnimateInsertingEntitiesEvent>(OnAnimateInsertingEntities);
    }

    private void OnAnimateInsertingEntities(AnimateInsertingEntitiesEvent ev)
    {
        if (TryComp<ClientStorageComponent>(ev.Storage, out var storage))
        {
            storage.HandleAnimatingInsertingEntities(ev);
        }
    }
}
