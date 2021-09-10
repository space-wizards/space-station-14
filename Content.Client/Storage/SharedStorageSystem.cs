using Content.Shared.Inventory.Events;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage.Events;
using JetBrains.Annotations;

namespace Content.Client.Storage
{
    [UsedImplicitly]
    public sealed class StorageSystem : SharedStorageSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<OpenCloseBagEvent>(OnOpenCloseBag);
        }

        private void OnOpenCloseBag(OpenCloseBagEvent ev)
        {
            if (!ComponentManager.HasComponent<ClientStorageComponent>(ev.Owner)) return;

            OnOpenCloseEvent(ev);
        }
    }
}
