using Content.Client.Storage.Systems;
using Content.Shared.Storage;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Storage.UI;

public sealed class StorageUIController : UIController, IOnSystemChanged<StorageSystem>
{
    private void OnStorageUpdate(EntityUid uid, StorageComponent component)
    {
        if (EntityManager.TryGetComponent<UserInterfaceComponent>(uid, out var uiComp) &&
            uiComp.OpenInterfaces.TryGetValue(StorageComponent.StorageUiKey.Key, out var bui))
        {
            var storageBui = (StorageBoundUserInterface) bui;

            storageBui.BuildEntityList(uid, component);
        }
    }

    public void OnSystemLoaded(StorageSystem system)
    {
        system.StorageUpdated += OnStorageUpdate;
    }

    public void OnSystemUnloaded(StorageSystem system)
    {
        system.StorageUpdated -= OnStorageUpdate;
    }
}
