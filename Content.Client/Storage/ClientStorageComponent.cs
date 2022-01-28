using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Animations;
using Content.Client.Hands;
using Content.Client.Items.Components;
using Content.Client.Items.Managers;
using Content.Client.UserInterface.Controls;
using Content.Shared.DragDrop;
using Content.Shared.Storage;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Players;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Storage
{
    /// <summary>
    /// Client version of item storage containers, contains a UI which displays stored entities and their size
    /// </summary>
    [RegisterComponent]
    public class ClientStorageComponent : SharedStorageComponent, IDraggable
    {
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private List<EntityUid> _storedEntities = new();
        private int StorageSizeUsed;
        private int StorageCapacityMax;
        private StorageWindow? _window;
        public bool UIOpen => _window?.IsOpen ?? false;

        public override IReadOnlyList<EntityUid> StoredEntities => _storedEntities;

        private StorageWindow GetOrCreateWindow()
        {
            if (_window == null)
            {
                _window = new StorageWindow(this, _playerManager, _entityManager)
                {
                    Title = _entityManager.GetComponent<MetaDataComponent>(Owner).EntityName
                };

                _window.EntityList.GenerateItem += GenerateButton;
                _window.EntityList.ItemPressed += Interact;
            }

            return _window;
        }

        protected override void OnRemove()
        {
            if (_window is { Disposed: false })
            {
                _window.EntityList.GenerateItem -= GenerateButton;
                _window.EntityList.ItemPressed -= Interact;
                _window.Dispose();
            }

            _window = null;
            base.OnRemove();
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not StorageComponentState state)
            {
                return;
            }

            _storedEntities = state.StoredEntities.ToList();
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
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
            _storedEntities = storageState.StoredEntities.ToList();
            StorageSizeUsed = storageState.StorageSizeUsed;
            StorageCapacityMax = storageState.StorageSizeMax;
            GetOrCreateWindow().BuildEntityList(storageState.StoredEntities.ToList());
        }

        /// <summary>
        /// Animate the newly stored entities in <paramref name="msg"/> flying towards this storage's position
        /// </summary>
        /// <param name="msg"></param>
        private void HandleAnimatingInsertingEntities(AnimateInsertingEntitiesMessage msg)
        {
            for (var i = 0; msg.StoredEntities.Count > i; i++)
            {
                var entity = msg.StoredEntities[i];
                var initialPosition = msg.EntityPositions[i];

                if (_entityManager.EntityExists(entity))
                {
                    ReusableAnimations.AnimateEntityPickup(entity, initialPosition, _entityManager.GetComponent<TransformComponent>(Owner).LocalPosition, _entityManager);
                }
            }
        }

        /// <summary>
        /// Opens the storage UI if closed. Closes it if opened.
        /// </summary>
        private void ToggleUI()
        {
            var window = GetOrCreateWindow();

            if (window.IsOpen)
                window.Close();
            else
                window.OpenCentered();
        }

        private void CloseUI()
        {
            _window?.Close();
        }

        /// <summary>
        /// Function for clicking one of the stored entity buttons in the UI, tells server to remove that entity
        /// </summary>
        /// <param name="entity"></param>
        private void Interact(ButtonEventArgs args, EntityUid entity)
        {
            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
#pragma warning disable 618
                SendNetworkMessage(new RemoveEntityMessage(entity));
#pragma warning restore 618
                args.Event.Handle();
            }
            else if (_entityManager.EntityExists(entity))
            {
                _itemSlotManager.OnButtonPressed(args.Event, entity);
            }
        }

        public override bool Remove(EntityUid entity)
        {
            if (_storedEntities.Remove(entity))
            {
                Dirty();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Button created for each entity that represents that item in the storage UI, with a texture, and name and size label
        /// </summary>
        private void GenerateButton(EntityUid entity, EntityContainerButton button)
        {
            if (!_entityManager.EntityExists(entity))
                return;

            _entityManager.TryGetComponent(entity, out ISpriteComponent? sprite);
            _entityManager.TryGetComponent(entity, out ItemComponent? item);

            button.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 2,
                Children =
                {
                    new SpriteView
                    {
                        HorizontalAlignment = HAlignment.Left,
                        VerticalAlignment = VAlignment.Center,
                        MinSize = new Vector2(32.0f, 32.0f),
                        OverrideDirection = Direction.South,
                        Sprite = sprite
                    },
                    new Label
                    {
                        HorizontalExpand = true,
                        ClipText = true,
                        Text = _entityManager.GetComponent<MetaDataComponent>(entity).EntityName
                    },
                    new Label
                    {
                        Align = Label.AlignMode.Right,
                        Text = item?.Size.ToString() ?? Loc.GetString("no-item-size")
                    }
                }
            });

            button.EnableAllKeybinds = true;
        }

        /// <summary>
        /// GUI class for client storage component
        /// </summary>
        private class StorageWindow : DefaultWindow
        {
            private Control _vBox;
            private readonly Label _information;
            public readonly EntityListDisplay EntityList;
            public readonly ClientStorageComponent StorageEntity;

            private readonly StyleBoxFlat _hoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.35f) };
            private readonly StyleBoxFlat _unHoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.0f) };

            public StorageWindow(ClientStorageComponent storageEntity, IPlayerManager players, IEntityManager entities)
            {
                StorageEntity = storageEntity;
                SetSize = (200, 320);
                Title = Loc.GetString("comp-storage-window-title");
                RectClipContent = true;

                var containerButton = new ContainerButton
                {
                    Name = "StorageContainerButton",
                    MouseFilter = MouseFilterMode.Pass,
                };
                Contents.AddChild(containerButton);

                var innerContainerButton = new PanelContainer
                {
                    PanelOverride = _unHoveredBox,
                };

                containerButton.AddChild(innerContainerButton);
                containerButton.OnPressed += args =>
                {
                    var controlledEntity = players.LocalPlayer?.ControlledEntity;

                    if (entities.HasComponent<HandsComponent>(controlledEntity))
                    {
#pragma warning disable 618
                        StorageEntity.SendNetworkMessage(new InsertEntityMessage());
#pragma warning restore 618
                    }
                };

                _vBox = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Vertical,
                    MouseFilter = MouseFilterMode.Ignore,
                };
                containerButton.AddChild(_vBox);
                _information = new Label
                {
                    Text = Loc.GetString("comp-storage-window-volume", ("itemCount", 0), ("usedVolume", 0), ("maxVolume", 0)),
                    VerticalAlignment = VAlignment.Center
                };
                _vBox.AddChild(_information);

                EntityList = new EntityListDisplay
                {
                    Name = "EntityListContainer",
                };
                _vBox.AddChild(EntityList);
                EntityList.OnMouseEntered += _ =>
                {
                    innerContainerButton.PanelOverride = _hoveredBox;
                };

                EntityList.OnMouseExited += _ =>
                {
                    innerContainerButton.PanelOverride = _unHoveredBox;
                };
            }

            public override void Close()
            {
#pragma warning disable 618
                StorageEntity.SendNetworkMessage(new CloseStorageUIMessage());
#pragma warning restore 618
                base.Close();
            }

            /// <summary>
            /// Loops through stored entities creating buttons for each, updates information labels
            /// </summary>
            public void BuildEntityList(List<EntityUid> entityUids)
            {
                EntityList.PopulateList(entityUids);

                //Sets information about entire storage container current capacity
                if (StorageEntity.StorageCapacityMax != 0)
                {
                    _information.Text = Loc.GetString("comp-storage-window-volume", ("itemCount", entityUids.Count),
                        ("usedVolume", StorageEntity.StorageSizeUsed), ("maxVolume", StorageEntity.StorageCapacityMax));
                }
                else
                {
                    _information.Text = Loc.GetString("comp-storage-window-volume-unlimited", ("itemCount", entityUids.Count));
                }
            }
        }
    }
}
