using Content.Client.UserInterface.Systems.Storage;
using Content.Client.UserInterface.Systems.Storage.Controls;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Storage;

[UsedImplicitly]
public sealed class StorageBoundUserInterface : BoundUserInterface
{
    private StorageWindow? _window;

    public StorageBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = IoCManager.Resolve<IUserInterfaceManager>()
            .GetUIController<StorageUIController>()
            .CreateStorageWindow(Owner);

        if (EntMan.TryGetComponent(Owner, out StorageComponent? storage))
        {
            _window.UpdateContainer((Owner, storage));
        }

        _window.OnClose += Close;
        _window.FlagDirty();
    }

    public void Refresh()
    {
        _window?.FlagDirty();
    }

    public void Reclaim()
    {
        if (_window == null)
            return;

        _window.OnClose -= Close;
        _window.Orphan();
        _window = null;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Reclaim();
    }

    public void Hide()
    {
        if (_window == null)
            return;

        _window.Visible = false;
    }

    public void Show()
    {
        if (_window == null)
            return;

        _window.Visible = true;
    }

    public void ReOpen()
    {
        _window?.Orphan();
        _window = null;
        Open();
    }
}

