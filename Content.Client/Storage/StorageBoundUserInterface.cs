using Content.Client.Examine;
using Content.Client.Storage.UI;
using Content.Client.UserInterface.Controls;
using Content.Client.Verbs.UI;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
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

        public StorageBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Open()
        {
            base.Open();

            if (_window == null)
            {
                // TODO: This is a bit of a mess but storagecomponent got moved to shared and cleaned up a bit.
                var controller = IoCManager.Resolve<IUserInterfaceManager>().GetUIController<StorageUIController>();
                _window = controller.EnsureStorageWindow(Owner);
                _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

                _window.EntityList.GenerateItem += _window.GenerateButton;
                _window.EntityList.ItemPressed += InteractWithItem;
                _window.StorageContainerButton.OnPressed += TouchedContainerButton;

                _window.OnClose += Close;

                if (EntMan.TryGetComponent<StorageComponent>(Owner, out var storageComp))
                {
                    BuildEntityList(Owner, storageComp);
                }

            }
            else
            {
                _window.Open();
            }
        }

        public void BuildEntityList(EntityUid uid, StorageComponent component)
        {
            _window?.BuildEntityList(uid, component);
        }

        public void InteractWithItem(BaseButton.ButtonEventArgs args, ListData cData)
        {
            if (cData is not EntityListData { Uid: var entity })
                return;

            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
                SendPredictedMessage(new StorageInteractWithItemEvent(_entManager.GetNetEntity(entity)));
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

            if (_window != null)
            {
                _window.Orphan();
                _window.EntityList.GenerateItem -= _window.GenerateButton;
                _window.EntityList.ItemPressed -= InteractWithItem;
                _window.StorageContainerButton.OnPressed -= TouchedContainerButton;
                _window.OnClose -= Close;
                _window = null;
            }
        }
    }
}
