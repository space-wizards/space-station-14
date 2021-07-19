using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Animations;
using Content.Client.Hands;
using Content.Shared.DragDrop;
using Content.Shared.Storage;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Players;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Storage
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
        private StorageWindow? _window;
        private SharedBagState _bagState;

        public override IReadOnlyList<IEntity> StoredEntities => _storedEntities;

        protected override void Initialize()
        {
            base.Initialize();

            // Hide stackVisualizer on start
            ChangeStorageVisualization(SharedBagState.Close);
        }

        protected override void OnAdd()
        {
            base.OnAdd();

            _window = new StorageWindow(this) {Title = Owner.Name};
        }

        protected override void OnRemove()
        {
            _window?.Dispose();
            base.OnRemove();
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
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

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                //Updates what we are storing for the UI
                case StorageHeldItemsMessage msg:
                    HandleStorageMessage(msg);
                    ChangeStorageVisualization(_bagState);
                    break;
                //Opens the UI
                case OpenStorageUIMessage _:
                    ChangeStorageVisualization(SharedBagState.Open);
                    ToggleUI();
                    break;
                case CloseStorageUIMessage _:
                    ChangeStorageVisualization(SharedBagState.Close);
                    CloseUI();
                    break;
                case AnimateInsertingEntitiesMessage msg:
                    HandleAnimatingInsertingEntities(msg);
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
            _window?.BuildEntityList();
        }

        /// <summary>
        /// Animate the newly stored entities in <paramref name="msg"/> flying towards this storage's position
        /// </summary>
        /// <param name="msg"></param>
        private void HandleAnimatingInsertingEntities(AnimateInsertingEntitiesMessage msg)
        {
            for (var i = 0; msg.StoredEntities.Count > i; i++)
            {
                var entityId = msg.StoredEntities[i];
                var initialPosition = msg.EntityPositions[i];

                if (Owner.EntityManager.TryGetEntity(entityId, out var entity))
                {
                    ReusableAnimations.AnimateEntityPickup(entity, initialPosition, Owner.Transform.WorldPosition);
                }
            }
        }

        /// <summary>
        /// Opens the storage UI if closed. Closes it if opened.
        /// </summary>
        private void ToggleUI()
        {
            if (_window == null) return;

            if (_window.IsOpen)
                _window.Close();
            else
                _window.OpenCentered();
        }

        private void CloseUI()
        {
            _window?.Close();
        }

        private void ChangeStorageVisualization(SharedBagState state)
        {
            _bagState = state;
            if (Owner.TryGetComponent<AppearanceComponent>(out var appearanceComponent))
            {
                appearanceComponent.SetData(SharedBagOpenVisuals.BagState, state);
            }
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
            private readonly BoxContainer _entityList;
            private readonly Label _information;
            public ClientStorageComponent StorageEntity;

            private readonly StyleBoxFlat _hoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.35f) };
            private readonly StyleBoxFlat _unHoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.0f) };

            public StorageWindow(ClientStorageComponent storageEntity)
            {
                StorageEntity = storageEntity;
                SetSize = (200, 320);
                Title = "Storage Item";
                RectClipContent = true;

                var containerButton = new ContainerButton
                {
                    MouseFilter = MouseFilterMode.Pass,
                };

                var innerContainerButton = new PanelContainer
                {
                    PanelOverride = _unHoveredBox,
                };


                containerButton.AddChild(innerContainerButton);
                containerButton.OnPressed += args =>
                {
                    var controlledEntity = IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity;

                    if (controlledEntity?.TryGetComponent(out HandsComponent? hands) ?? false)
                    {
                        StorageEntity.SendNetworkMessage(new InsertEntityMessage());
                    }
                };

                VSplitContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    MouseFilter = MouseFilterMode.Ignore,
                };
                containerButton.AddChild(VSplitContainer);
                _information = new Label
                {
                    Text = "Items: 0 Volume: 0/0 Stuff",
                    VerticalAlignment = VAlignment.Center
                };
                VSplitContainer.AddChild(_information);

                var listScrollContainer = new ScrollContainer
                {
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    HScrollEnabled = false,
                    VScrollEnabled = true,
                };
                _entityList = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    HorizontalExpand = true
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
                    if (entity.TryGetComponent(out ISpriteComponent? sprite))
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
                if (args.Button.Parent is not EntityButton button)
                {
                    return;
                }

                args.Button.Pressed = false;
                StorageEntity.Interact(button.EntityUid);
            }
        }

        /// <summary>
        /// Button created for each entity that represents that item in the storage UI, with a texture, and name and size label
        /// </summary>
        private class EntityButton : Control
        {
            public EntityUid EntityUid { get; set; }
            public Button ActualButton { get; }
            public SpriteView EntitySpriteView { get; }
            public Label EntityName { get; }
            public Label EntitySize { get; }

            public EntityButton()
            {
                ActualButton = new Button
                {
                    HorizontalExpand = true,
                    VerticalExpand = true,
                    ToggleMode = true,
                    MouseFilter = MouseFilterMode.Stop
                };
                AddChild(ActualButton);

                var hBoxContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal
                };
                EntitySpriteView = new SpriteView
                {
                    MinSize = new Vector2(32.0f, 32.0f),
                    OverrideDirection = Direction.South
                };
                EntityName = new Label
                {
                    VerticalAlignment = VAlignment.Center,
                    HorizontalExpand = true,
                    Margin = new Thickness(0, 0, 6, 0),
                    Text = "Backpack",
                    ClipText = true
                };

                hBoxContainer.AddChild(EntitySpriteView);
                hBoxContainer.AddChild(EntityName);

                EntitySize = new Label
                {
                    VerticalAlignment = VAlignment.Bottom,
                    Text = "Size 6",
                    Align = Label.AlignMode.Right,
                };

                hBoxContainer.AddChild(EntitySize);
                AddChild(hBoxContainer);
            }
        }
    }
}
