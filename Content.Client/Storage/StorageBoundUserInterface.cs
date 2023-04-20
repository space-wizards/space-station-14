using Content.Client.Examine;
using Content.Client.Storage.UI;
using Content.Client.UserInterface.Controls;
using Content.Client.Verbs;
using Content.Client.Verbs.UI;
using Content.Shared.Input;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using static Content.Shared.Storage.SharedStorageComponent;

namespace Content.Client.Storage
{
    [UsedImplicitly]
    public sealed class StorageBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private StorageWindow? _window;

        public StorageBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            if (_window == null)
            {
                var entMan = IoCManager.Resolve<IEntityManager>();
                _window = new StorageWindow(entMan)
                    {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};

                _window.EntityList.GenerateItem += _window.GenerateButton;
                _window.EntityList.ItemPressed += InteractWithItem;
                _window.StorageContainerButton.OnPressed += TouchedContainerButton;

                _window.OnClose += Close;
                _window.OpenCenteredLeft();
            }
            else
            {
                _window.Open();
            }
        }

        public void InteractWithItem(BaseButton.ButtonEventArgs args, ListData cData)
        {
            if (cData is not EntityListData {Uid: var entity})
                return;

            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
                SendMessage(new StorageInteractWithItemEvent(entity));
            }
            else if (IoCManager.Resolve<IEntityManager>().EntityExists(entity))
            {
                OnButtonPressed(args.Event, entity);
            }
        }

        private void OnButtonPressed(GUIBoundKeyEventArgs args, EntityUid entity)
        {
            var entitySys = IoCManager.Resolve<IEntitySystemManager>();
            var entities = IoCManager.Resolve<IEntityManager>();

            if (args.Function == ContentKeyFunctions.ExamineEntity)
            {
                entitySys.GetEntitySystem<ExamineSystem>()
                    .DoExamine(entity);
            }
            else if (args.Function == EngineKeyFunctions.UseSecondary)
            {
                IoCManager.Resolve<IUserInterfaceManager>().GetUIController<VerbMenuUIController>().OpenVerbMenu(entity);
            }
            else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
            {
                entities.EntityNetManager?.SendSystemNetworkMessage(
                    new InteractInventorySlotEvent(entity, altInteract: false));
            }
            else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
            {
                entities.RaisePredictiveEvent(new InteractInventorySlotEvent(entity, altInteract: true));
            }
            else
            {
                return;
            }

            args.Handle();
        }

        public void TouchedContainerButton(BaseButton.ButtonEventArgs args)
        {
            SendMessage(new StorageInsertItemMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null || state is not StorageBoundUserInterfaceState cast)
                return;

            _window?.BuildEntityList(cast);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_window != null)
            {
                _window.EntityList.GenerateItem -= _window.GenerateButton;
                _window.EntityList.ItemPressed -= InteractWithItem;
                _window.StorageContainerButton.OnPressed -= TouchedContainerButton;
                _window.OnClose -= Close;
            }

            _window?.Dispose();
            _window = null;
        }
    }
}
