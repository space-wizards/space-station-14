using Content.Client.Gameplay;
using Content.Client.Storage.Systems;
using Content.Shared.Storage;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Storage.UI;

public sealed class StorageUIController : UIController, IOnSystemChanged<StorageSystem>, IOnStateExited<GameplayState>
{
    // This is mainly to keep legacy functionality for now.
    private readonly Dictionary<EntityUid, StorageWindow> _storageWindows = new();

    public override void Initialize()
    {
        base.Initialize();
        EntityManager.EventBus.SubscribeLocalEvent<StorageComponent, ComponentShutdown>(OnStorageShutdown);
    }
    public StorageWindow EnsureStorageWindow(EntityUid uid)
    {
        if (_storageWindows.TryGetValue(uid, out var window))
        {
            UIManager.WindowRoot.AddChild(window);
            return window;
        }

        window = new StorageWindow(EntityManager);
        _storageWindows[uid] = window;
        window.OpenCenteredLeft();
        return window;
    }

    private void OnStorageShutdown(EntityUid uid, StorageComponent component, ComponentShutdown args)
    {
        if (!_storageWindows.TryGetValue(uid, out var window))
            return;

        _storageWindows.Remove(uid);
        window.Dispose();
    }

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

    public void OnStateExited(GameplayState state)
    {
        foreach (var window in _storageWindows.Values)
        {
            window.Dispose();
        }

        _storageWindows.Clear();
    }
}
