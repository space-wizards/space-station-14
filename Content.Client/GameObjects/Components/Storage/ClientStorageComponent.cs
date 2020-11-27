using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Storage
{
    /// <summary>
    /// Client version of item storage containers, contains a UI which displays stored entities and their size
    /// </summary>
    [RegisterComponent]
    public class ClientStorageComponent : SharedStorageComponent, IDraggable
    {
        private List<IEntity> _storedEntities = new();
        private int StorageSizeUsed;
        private int StorageCapacityMax;
        private StorageWindow Window;

        public override IReadOnlyList<IEntity> StoredEntities => _storedEntities;

        public override void OnAdd()
        {
            base.OnAdd();

            Window = new StorageWindow()
            { StorageEntity = this, Title = Owner.Name };
        }

        public override void OnRemove()
        {
            Window.Dispose();
            base.OnRemove();
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not StorageComponentState state)
            {
                return;
            }

            _storedEntities = state.StoredEntities
                .Select(id => Owner.EntityManager.GetEntity(id))
                .ToList();
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                //Updates what we are storing for the UI
                case StorageHeldItemsMessage msg:
                    HandleStorageMessage(msg);
                    break;
                //Opens the UI
                case OpenStorageUIMessage _:
                    ToggleUI();
                    break;
                case CloseStorageUIMessage _:
                    CloseUI();
                    break;
            }
        }

        /// <summary>
        /// Copies received values from server about contents of storage container
        /// </summary>
        /// <param name="storageState"></param>
        private void HandleStorageMessage(StorageHeldItemsMessage storageState)
        {
            _storedEntities = storageState.StoredEntities.Select(id => Owner.EntityManager.GetEntity(id)).ToList();
            StorageSizeUsed = storageState.StorageSizeUsed;
            StorageCapacityMax = storageState.StorageSizeMax;
            Window.BuildEntityList();
        }

        /// <summary>
        /// Opens the storage UI if closed. Closes it if opened.
        /// </summary>
        private void ToggleUI()
        {
            if (Window.IsOpen)
                Window.Close();
            else
                Window.Open();
        }

        private void CloseUI()
        {
            Window.Close();
        }

        /// <summary>
        /// Function for clicking one of the stored entity buttons in the UI, tells server to remove that entity
        /// </summary>
        /// <param name="entityUid"></param>
        private void Interact(EntityUid entityUid)
        {
            SendNetworkMessage(new RemoveEntityMessage(entityUid));
        }

        public override bool Remove(IEntity entity)
        {
            if (_storedEntities.Remove(entity))
            {
                Dirty();
                return true;
            }

            return false;
        }

        /// <summary>
        /// GUI class for client storage component
        /// </summary>
        private class StorageWindow : SS14Window
        {
            private Control VSplitContainer;
            private readonly VBoxContainer _entityList;
            private readonly Label _information;
            public ClientStorageComponent StorageEntity;

            private readonly StyleBoxFlat _hoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.35f) };
            private readonly StyleBoxFlat _unHoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.0f) };

            protected override Vector2? CustomSize => (180, 320);

            public StorageWindow()
            {
                Title = "Storage Item";
                RectClipContent = true;

                var containerButton = new ContainerButton
                {
                    SizeFlagsHorizontal = SizeFlags.Fill,
                    SizeFlagsVertical = SizeFlags.Fill,
                    MouseFilter = MouseFilterMode.Pass,
                };

                var innerContainerButton = new PanelContainer
                {
                    PanelOverride = _unHoveredBox,
                    SizeFlagsHorizontal = SizeFlags.Fill,
                    SizeFlagsVertical = SizeFlags.Fill,
                };


                containerButton.AddChild(innerContainerButton);
                containerButton.OnPressed += args =>
                {
                    var controlledEntity = IoCManager.Resolve<IPlayerManager>().LocalPlayer.ControlledEntity;

                    if (controlledEntity.TryGetComponent(out HandsComponent hands))
                    {
                        StorageEntity.SendNetworkMessage(new InsertEntityMessage());
                    }
                };

                VSplitContainer = new VBoxContainer()
                {
                    MouseFilter = MouseFilterMode.Ignore,
                };
                containerButton.AddChild(VSplitContainer);
                _information = new Label
                {
                    Text = "Items: 0 Volume: 0/0 Stuff",
                    SizeFlagsVertical = SizeFlags.ShrinkCenter
                };
                VSplitContainer.AddChild(_information);

                var listScrollContainer = new ScrollContainer
                {
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    HScrollEnabled = true,
                    VScrollEnabled = true,
                };
                _entityList = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand
                };
                listScrollContainer.AddChild(_entityList);
                VSplitContainer.AddChild(listScrollContainer);

                Contents.AddChild(containerButton);

                listScrollContainer.OnMouseEntered += args =>
                {
                    innerContainerButton.PanelOverride = _hoveredBox;
                };

                listScrollContainer.OnMouseExited += args =>
                {
                    innerContainerButton.PanelOverride = _unHoveredBox;
                };
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
                _entityList.DisposeAllChildren();

                var storageList = StorageEntity.StoredEntities;

                var storedGrouped = storageList.GroupBy(e => e).Select(e => new
                {
                    Entity = e.Key,
                    Amount = e.Count()
                });

                foreach (var group in storedGrouped)
                {
                    var entity = group.Entity;
                    var button = new EntityButton()
                    {
                        EntityUid = entity.Uid,
                        MouseFilter = MouseFilterMode.Stop,
                    };
                    button.ActualButton.OnToggled += OnItemButtonToggled;
                    //Name and Size labels set
                    button.EntityName.Text = entity.Name;

                    button.EntitySize.Text = group.Amount.ToString();

                    //Gets entity sprite and assigns it to button texture
                    if (entity.TryGetComponent(out ISpriteComponent sprite))
                    {
                        button.EntitySpriteView.Sprite = sprite;
                    }

                    _entityList.AddChild(button);
                }

                //Sets information about entire storage container current capacity
                if (StorageEntity.StorageCapacityMax != 0)
                {
                    _information.Text = String.Format("Items: {0}, Stored: {1}/{2}", storageList.Count,
                        StorageEntity.StorageSizeUsed, StorageEntity.StorageCapacityMax);
                }
                else
                {
                    _information.Text = String.Format("Items: {0}", storageList.Count);
                }
            }

            /// <summary>
            /// Function assigned to button toggle which removes the entity from storage
            /// </summary>
            /// <param name="args"></param>
            private void OnItemButtonToggled(BaseButton.ButtonToggledEventArgs args)
            {
                var control = (EntityButton) args.Button.Parent;
                args.Button.Pressed = false;
                StorageEntity.Interact(control.EntityUid);
            }

            /// <summary>
            /// Function assigned to button that adds items to the storage entity.
            /// </summary>
            private void OnAddItemButtonPressed(BaseButton.ButtonEventArgs args)
            {
                var controlledEntity = IoCManager.Resolve<IPlayerManager>().LocalPlayer.ControlledEntity;

                if (controlledEntity.TryGetComponent(out HandsComponent hands))
                {
                    StorageEntity.SendNetworkMessage(new InsertEntityMessage());
                }
            }
        }

        /// <summary>
        /// Button created for each entity that represents that item in the storage UI, with a texture, and name and size label
        /// </summary>
        private class EntityButton : PanelContainer
        {
            public EntityUid EntityUid { get; set; }
            public Button ActualButton { get; }
            public SpriteView EntitySpriteView { get; }
            public Control EntityControl { get; }
            public Label EntityName { get; }
            public Label EntitySize { get; }

            public EntityButton()
            {
                ActualButton = new Button
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    ToggleMode = true,
                    MouseFilter = MouseFilterMode.Stop
                };
                AddChild(ActualButton);

                var hBoxContainer = new HBoxContainer();
                EntitySpriteView = new SpriteView
                {
                    CustomMinimumSize = new Vector2(32.0f, 32.0f)
                };
                EntityName = new Label
                {
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    Text = "Backpack",
                };
                hBoxContainer.AddChild(EntitySpriteView);
                hBoxContainer.AddChild(EntityName);

                EntityControl = new Control
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand
                };
                EntitySize = new Label
                {
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    Text = "Size 6",
                    Align = Label.AlignMode.Right,
                    /*AnchorLeft = 1.0f,
                    AnchorRight = 1.0f,
                    AnchorBottom = 0.5f,
                    AnchorTop = 0.5f,
                    MarginLeft = -38.0f,
                    MarginTop = -7.0f,
                    MarginRight = -5.0f,
                    MarginBottom = 7.0f*/
                };

                EntityControl.AddChild(EntitySize);
                hBoxContainer.AddChild(EntityControl);
                AddChild(hBoxContainer);
            }
        }
    }
}
