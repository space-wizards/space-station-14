using Content.Client.Storage.UI;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
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
                _window = new StorageWindow(entMan) {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};

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
