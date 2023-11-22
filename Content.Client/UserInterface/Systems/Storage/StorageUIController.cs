using Content.Client.Gameplay;
using Content.Client.Storage.Systems;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Storage.Controls;
using Content.Shared.Storage;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.UserInterface.Systems.Storage;

public sealed class StorageUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<StorageSystem>
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private StorageContainer? _container;

    private HotbarGui? Hotbar => UIManager.GetActiveUIWidgetOrNull<HotbarGui>();

    public override void Initialize()
    {
        base.Initialize();

        //EntityManager.EventBus.SubscribeLocalEvent<StorageComponent, ComponentShutdown>(OnStorageShutdown);
    }

    private void OnClick(ICommonSession? session)
    {

    }

    private void OnStorageShutdown(EntityUid uid, StorageComponent component, ComponentShutdown args)
    {
        //todo: close the storage window nerd
    }

    public void OnSystemLoaded(StorageSystem system)
    {
        system.StorageOpened += OnStorageOpened;
        system.StorageClosed += OnStorageClosed;
    }

    public void OnSystemUnloaded(StorageSystem system)
    {
        system.StorageOpened -= OnStorageOpened;
        system.StorageClosed -= OnStorageClosed;
    }

    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.UIClick, InputCmdHandler.FromDelegate(OnClick))
            .Register<StorageUIController>();
    }

    private void OnStorageOpened(EntityUid uid, StorageComponent component)
    {
        if (_container == null)
            return;

        _container.UpdateContainer((uid, component));
        _container.Visible = true;
    }

    private void OnStorageClosed(EntityUid uid, StorageComponent component)
    {
        if (_container == null)
            return;

        _container.Visible = false;
    }

    public void RegisterStorageContainer(StorageContainer container)
    {
        _container = container;
    }
}
