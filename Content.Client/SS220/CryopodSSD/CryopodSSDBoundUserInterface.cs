// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Examine;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Content.Shared.SS220.CryopodSSD;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Content.Shared.Storage;
using Content.Client.Storage.Systems;

namespace Content.Client.SS220.CryopodSSD;

public sealed class CryopodSSDBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private CryopodSSDWindow? _window = default!;

    private readonly StorageSystem _storage;

    public CryopodSSDBoundUserInterface(EntityUid owner, Enum uikey) : base(owner, uikey)
    {
        IoCManager.InjectDependencies(this);

        _storage = _entManager.System<StorageSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = new CryopodSSDWindow();
        _window.OnClose += Close;

        _window.OpenCentered();

        if (_entManager.TryGetComponent<StorageComponent>(Owner, out var comp))
        {
            _storage.OpenStorageUI(Owner, comp);
        }
    }

    public void InteractWithItem(BaseButton.ButtonEventArgs? args, ListData? cData)
    {
        if (args is null || cData is null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();

        if (cData is not EntityListData { Uid: var entity })
            return;

        if (args.Event.Function == EngineKeyFunctions.UIClick)
        {
            SendMessage(new CryopodSSDStorageInteractWithItemEvent(entMan.GetNetEntity(entity)));
        }
        else if (entMan.EntityExists(entity))
        {
            OnButtonPressed(args.Event, entity);
        }
    }

    private static void OnButtonPressed(GUIBoundKeyEventArgs args, EntityUid entity)
    {
        var entitySys = IoCManager.Resolve<IEntitySystemManager>();

        if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            entitySys.GetEntitySystem<ExamineSystem>()
                .DoExamine(entity);
        }
        else
        {
            return;
        }

        args.Handle();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SSDStorageConsoleState castedState)
        {
            return;
        }

        if (!castedState.HasAccess)
        {
            _storage.CloseStorageUI(Owner);
        }

        //if (_storageWindow is not null)
        //{
        //    _storageWindow.Visible = castedState.HasAccess;
        //}

        //var entityMan = IoCManager.Resolve<IEntityManager>();
        //if (entityMan.TryGetComponent<StorageComponent>(Owner, out var storageComp))
        //    _storageWindow?.BuildEntityList(Owner, storageComp);
        _window?.UpdateState(castedState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            return;
        }

        _storage.CloseStorageUI(Owner);

        _window?.Close();
    }
}
