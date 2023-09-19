using Content.Client.Gameplay;
using Content.Client.Storage.Systems;
using Content.Shared.Storage;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Storage.UI;

public sealed class StorageUIController : UIController, IOnSystemChanged<StorageSystem>, IOnStateExited<GameplayState>
{
    [UISystemDependency] private readonly StorageSystem _storage = default!;

    private StorageWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        EntityManager.EventBus.SubscribeLocalEvent<StorageComponent, ComponentShutdown>(OnStorageShutdown);
    }

    public StorageWindow EnsureStorageWindow(EntityUid uid)
    {
        if (_window == null)
        {
            _window = new StorageWindow(EntityManager);
            _window.OpenCenteredLeft();
        }
        // If it's not already open for another entity.
        else if (!_window.VisibleInTree)
        {
            UIManager.WindowRoot.AddChild(_window);
        }

        return _window;
    }

    private void OnStorageShutdown(EntityUid uid, StorageComponent component, ComponentShutdown args)
    {
        if (uid != _storage.OpenEntity || _window == null)
            return;

        _window.Visible = false;
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
        _window?.Dispose();
        _window = null;
    }
}
