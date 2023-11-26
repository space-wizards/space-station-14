using Content.Client.Examine;
using Content.Client.Storage.Systems;
using Content.Client.Storage.UI;
using Content.Client.UserInterface.Controls;
using Content.Client.Verbs.UI;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using static Content.Shared.Storage.StorageComponent;

namespace Content.Client.Storage
{
    [UsedImplicitly]
    public sealed class StorageBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private StorageWindow? _window;

        [Dependency] private readonly IEntityManager _entManager = default!;
        private readonly StorageSystem _storage;
        private readonly StorageComponent _component;

        public StorageBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
            _storage = _entManager.System<StorageSystem>();
            _component = _entManager.GetComponent<StorageComponent>(owner);
        }

        protected override void Open()
        {
            base.Open();

            _storage.OpenStorageUI(Owner, _component);
        }

        public void BuildEntityList(EntityUid uid, StorageComponent component)
        {
            _window?.BuildEntityList(uid, component);
        }

        public void InteractWithItem(BaseButton.ButtonEventArgs? args, ListData? cData)
        {
            if (args == null || cData is not EntityListData { Uid: var entity })
                return;

            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
                //SendPredictedMessage(new StorageInteractWithItemEvent(_entManager.GetNetEntity(entity)));
            }
            else if (EntMan.EntityExists(entity))
            {
                OnButtonPressed(args.Event, entity);
            }
        }

        private void OnButtonPressed(GUIBoundKeyEventArgs args, EntityUid entity)
        {
            if (args.Function == ContentKeyFunctions.ExamineEntity)
            {
                EntMan.System<ExamineSystem>()
                    .DoExamine(entity);
            }
            else if (args.Function == EngineKeyFunctions.UseSecondary)
            {
                IoCManager.Resolve<IUserInterfaceManager>().GetUIController<VerbMenuUIController>().OpenVerbMenu(entity);
            }
            else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
            {
                EntMan.EntityNetManager?.SendSystemNetworkMessage(
                    new InteractInventorySlotEvent(EntMan.GetNetEntity(entity), altInteract: false));
            }
            else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
            {
                EntMan.RaisePredictiveEvent(new InteractInventorySlotEvent(EntMan.GetNetEntity(entity), altInteract: true));
            }
            else
            {
                return;
            }

            args.Handle();
        }

        public void TouchedContainerButton(BaseButton.ButtonEventArgs args)
        {
            SendPredictedMessage(new StorageInsertItemMessage());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _storage.CloseStorageUI(Owner, _component);
        }
    }
}
