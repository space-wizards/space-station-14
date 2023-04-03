using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Content.Client.Items.Components;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Item;
using Robust.Client.UserInterface;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Content.Shared.Storage.SharedStorageComponent;

namespace Content.Client.Storage.UI
{
    /// <summary>
    /// GUI class for client storage component
    /// </summary>
    public sealed class StorageWindow : DefaultWindow
    {
        private IEntityManager _entityManager;

        private readonly Label _information;
        public readonly ContainerButton StorageContainerButton;
        public readonly ListContainer EntityList;
        private readonly StyleBoxFlat _hoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.35f) };
        private readonly StyleBoxFlat _unHoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.0f) };

        public StorageWindow(IEntityManager entityManager)
        {
            _entityManager = entityManager;
            SetSize = (200, 320);
            Title = Loc.GetString("comp-storage-window-title");
            RectClipContent = true;

            StorageContainerButton = new ContainerButton
            {
                Name = "StorageContainerButton",
                MouseFilter = MouseFilterMode.Pass,
            };

            Contents.AddChild(StorageContainerButton);

            var innerContainerButton = new PanelContainer
            {
                PanelOverride = _unHoveredBox,
            };

            StorageContainerButton.AddChild(innerContainerButton);

            Control vBox = new BoxContainer()
            {
                Orientation = LayoutOrientation.Vertical,
                MouseFilter = MouseFilterMode.Ignore,
            };

            StorageContainerButton.AddChild(vBox);

            _information = new Label
            {
                Text = Loc.GetString("comp-storage-window-volume", ("itemCount", 0), ("usedVolume", 0), ("maxVolume", 0)),
                VerticalAlignment = VAlignment.Center
            };

            vBox.AddChild(_information);

            EntityList = new ListContainer
            {
                Name = "EntityListContainer",
            };

            vBox.AddChild(EntityList);

            EntityList.OnMouseEntered += _ =>
            {
                innerContainerButton.PanelOverride = _hoveredBox;
            };

            EntityList.OnMouseExited += _ =>
            {
                innerContainerButton.PanelOverride = _unHoveredBox;
            };
        }

        /// <summary>
        /// Loops through stored entities creating buttons for each, updates information labels
        /// </summary>
        public void BuildEntityList(StorageBoundUserInterfaceState state)
        {
            var list = state.StoredEntities.ConvertAll(uid => new EntityListData(uid));
            EntityList.PopulateList(list);

            //Sets information about entire storage container current capacity
            if (state.StorageCapacityMax != 0)
            {
                _information.Text = Loc.GetString("comp-storage-window-volume", ("itemCount", state.StoredEntities.Count),
                    ("usedVolume", state.StorageSizeUsed), ("maxVolume", state.StorageCapacityMax));
            }
            else
            {
                _information.Text = Loc.GetString("comp-storage-window-volume-unlimited", ("itemCount", state.StoredEntities.Count));
            }
        }

        /// <summary>
        /// Button created for each entity that represents that item in the storage UI, with a texture, and name and size label
        /// </summary>
        public void GenerateButton(ListData data, ListContainerButton button)
        {
            if (data is not EntityListData {Uid: var entity}
                || !_entityManager.EntityExists(entity))
                return;

            _entityManager.TryGetComponent(entity, out SpriteComponent? sprite);
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
                            Text = item?.Size.ToString() ?? Loc.GetString("comp-storage-no-item-size"),
                        }
                    }
            });
            button.StyleClasses.Add(StyleNano.StyleClassStorageButton);
            button.EnableAllKeybinds = true;
        }
    }
}
