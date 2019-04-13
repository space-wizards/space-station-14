using Content.Shared.GameObjects.Components.Storage;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using SS14.Client.Interfaces.Graphics;

namespace Content.Client.GameObjects.Components.Storage
{
    /// <summary>
    /// Client version of item storage containers, contains a UI which displays stored entities and their size
    /// </summary>
    public class ClientStorageComponent : SharedStorageComponent
    {
        private Dictionary<EntityUid, int> StoredEntities { get; set; } = new Dictionary<EntityUid, int>();
        private int StorageSizeUsed;
        private int StorageCapacityMax;
        private StorageWindow Window;

        public bool Open
        {
            get => _open;
            set
            {
                _open = value;
                SetDoorSprite(_open);
            }
        }

        public override void OnAdd()
        {
            base.OnAdd();

            Window = new StorageWindow(IoCManager.Resolve<IDisplayManager>())
            { StorageEntity = this };
        }

        public override void OnRemove()
        {
            Window.Dispose();
            base.OnRemove();
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is StorageComponentState storageState))
                return;

            Open = storageState.Open;
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                //Updates what we are storing for the UI
                case StorageHeldItemsMessage msg:
                    HandleStorageMessage(msg);
                    break;
                //Opens the UI
                case OpenStorageUIMessage msg:
                    OpenUI();
                    break;
                case CloseStorageUIMessage msg:
                    CloseUI();
                    break;
            }
        }

        /// <summary>
        /// Copies received values from server about contents of storage container
        /// </summary>
        /// <param name="storagestate"></param>
        private void HandleStorageMessage(StorageHeldItemsMessage storagestate)
        {
            StoredEntities = new Dictionary<EntityUid, int>(storagestate.StoredEntities);
            StorageSizeUsed = storagestate.StorageSizeUsed;
            StorageCapacityMax = storagestate.StorageSizeMax;
            Window.BuildEntityList();
        }

        /// <summary>
        /// Opens the storage UI
        /// </summary>
        private void OpenUI()
        {
            Window.AddToScreen();
            Window.Open();
        }

        private void CloseUI()
        {
            Window.Close();
        }

        /// <summary>
        /// Function for clicking one of the stored entity buttons in the UI, tells server to remove that entity
        /// </summary>
        /// <param name="entityuid"></param>
        private void Interact(EntityUid entityuid)
        {
            SendNetworkMessage(new RemoveEntityMessage(entityuid));
        }

        private void SetDoorSprite(bool open)
        {
            if(!Owner.TryGetComponent<ISpriteComponent>(out var spriteComp))
                return;

            if(!spriteComp.Running)
                return;

            if (spriteComp.BaseRSI == null)
            {
                return;
            }

            var baseName = spriteComp.LayerGetState(0).Name;

            var stateId = open ? $"{baseName}_open" : $"{baseName}_door";

            if (spriteComp.BaseRSI.TryGetState(stateId, out _))
                spriteComp.LayerSetState(1, stateId);
        }

        /// <summary>
        /// GUI class for client storage component
        /// </summary>
        private class StorageWindow : SS14Window
        {
            private Control VSplitContainer;
            private VBoxContainer EntityList;
            private Label Information;
            public ClientStorageComponent StorageEntity;

            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Storage/Storage.tscn");

            public StorageWindow(IDisplayManager displayMan) : base(displayMan) { }

            protected override void Initialize()
            {
                base.Initialize();

                HideOnClose = true;

                // Get all the controls.
                VSplitContainer = Contents.GetChild("VSplitContainer");
                EntityList = VSplitContainer.GetChild("ListScrollContainer").GetChild<VBoxContainer>("EntityList");
                Information = VSplitContainer.GetChild<Label>("Information");
            }

            public override void Close()
            {
                StorageEntity.SendNetworkMessage(new CloseStorageUIMessage());
                base.Close();
            }

            /// <summary>
            /// Loops through stored entities creating buttons for each, updates information labels
            /// </summary>
            public void BuildEntityList()
            {
                EntityList.DisposeAllChildren();

                var storagelist = StorageEntity.StoredEntities;

                foreach (var entityuid in storagelist)
                {
                    var entity = IoCManager.Resolve<IEntityManager>().GetEntity(entityuid.Key);

                    var button = new EntityButton()
                    {
                        EntityuID = entityuid.Key
                    };
                    var container = button.GetChild("HBoxContainer");
                    button.ActualButton.OnToggled += OnItemButtonToggled;
                    //Name and Size labels set
                    container.GetChild<Label>("Name").Text = entity.Name;
                    container.GetChild<Control>("Control").GetChild<Label>("Size").Text = string.Format("{0}", entityuid.Value);

                    //Gets entity sprite and assigns it to button texture
                    if (entity.TryGetComponent(out ISpriteComponent sprite))
                    {
                        var view = container.GetChild<SpriteView>("SpriteView");
                        view.Sprite = sprite;
                    }

                    EntityList.AddChild(button);
                }

                //Sets information about entire storage container current capacity
                if (StorageEntity.StorageCapacityMax != 0)
                {
                    Information.Text = String.Format("Items: {0}, Stored: {1}/{2}", storagelist.Count, StorageEntity.StorageSizeUsed, StorageEntity.StorageCapacityMax);
                }
                else
                {
                    Information.Text = String.Format("Items: {0}", storagelist.Count);
                }
            }

            /// <summary>
            /// Function assigned to button toggle which removes the entity from storage
            /// </summary>
            /// <param name="args"></param>
            private void OnItemButtonToggled(BaseButton.ButtonToggledEventArgs args)
            {
                var control = (EntityButton)args.Button.Parent;
                args.Button.Pressed = false;
                StorageEntity.Interact(control.EntityuID);
            }
        }

        /// <summary>
        /// Button created for each entity that represents that item in the storage UI, with a texture, and name and size label
        /// </summary>
        private class EntityButton : PanelContainer
        {
            public EntityUid EntityuID { get; set; }
            public Button ActualButton { get; private set; }

            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Storage/StorageEntity.tscn");

            protected override void Initialize()
            {
                base.Initialize();
                ActualButton = GetChild<Button>("Button");
            }
        }
    }
}
