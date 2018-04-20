using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Storage;
using SS14.Client.GameObjects;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;

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

        public override void OnAdd()
        {
            base.OnAdd();

            Window = new StorageWindow()
            { StorageEntity = this};
        }

        public override void OnRemove()
        {
            Window.Dispose();

            base.OnRemove();
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

        /// <summary>
        /// Function for clicking one of the stored entity buttons in the UI, tells server to remove that entity
        /// </summary>
        /// <param name="entityuid"></param>
        private void Interact(EntityUid entityuid)
        {
            SendNetworkMessage(new RemoveEntityMessage(entityuid));
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
            
            protected override void Initialize()
            {

                base.Initialize();

                HideOnClose = true;

                // Get all the controls.
                VSplitContainer = Contents.GetChild("VSplitContainer");
                EntityList = VSplitContainer.GetChild("ListScrollContainer").GetChild<VBoxContainer>("EntityList");
                Information = VSplitContainer.GetChild<Label>("Information");
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
                    if (entity.TryGetComponent(out SpriteComponent sprite))
                    {
                        var tex = sprite.CurrentSprite;
                        var rect = container.GetChild("TextureWrap").GetChild<TextureRect>("TextureRect");

                        if (tex != null)
                        {
                            rect.Texture = tex;
                            // Copypasted but replaced with 32 dunno if good
                            var scale = (float)32 / tex.Height;
                            rect.Scale = new Vector2(scale, scale);
                        }
                        else
                        {
                            rect.Dispose();
                        }
                    }

                    EntityList.AddChild(button);
                }

                //Sets information about entire storage container current capacity
                if(StorageEntity.StorageCapacityMax != 0)
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
        private class EntityButton : Control
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
