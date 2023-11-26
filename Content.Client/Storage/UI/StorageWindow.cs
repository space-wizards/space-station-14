using System.Numerics;
using Content.Client.Items.Systems;
using Content.Client.Message;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.UserInterface;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Storage.UI
{
    /// <summary>
    /// GUI class for client storage component
    /// </summary>
    public sealed class StorageWindow : FancyWindow
    {
        private readonly IEntityManager _entityManager;

        private readonly SharedStorageSystem _storage;
        private readonly ItemSystem _item;

        private readonly RichTextLabel _information;
        public readonly ContainerButton StorageContainerButton;
        public readonly ListContainer EntityList;
        private readonly StyleBoxFlat _hoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.35f) };
        private readonly StyleBoxFlat _unHoveredBox = new() { BackgroundColor = Color.Black.WithAlpha(0.0f) };

        public StorageWindow(IEntityManager entityManager)
        {
            _entityManager = entityManager;
            _storage = _entityManager.System<SharedStorageSystem>();
            _item = _entityManager.System<ItemSystem>();
            SetSize = new Vector2(240, 320);
            Title = Loc.GetString("comp-storage-window-title");
            RectClipContent = true;

            StorageContainerButton = new ContainerButton
            {
                Name = "StorageContainerButton",
                MouseFilter = MouseFilterMode.Pass,
            };

            ContentsContainer.AddChild(StorageContainerButton);

            var innerContainerButton = new PanelContainer
            {
                PanelOverride = _unHoveredBox,
            };

            StorageContainerButton.AddChild(innerContainerButton);

            Control vBox = new BoxContainer()
            {
                Orientation = LayoutOrientation.Vertical,
                MouseFilter = MouseFilterMode.Ignore,
                Margin = new Thickness(5),
            };

            StorageContainerButton.AddChild(vBox);

            _information = new RichTextLabel
            {
                VerticalAlignment = VAlignment.Center
            };
            _information.SetMessage(Loc.GetString("comp-storage-window-weight",
                ("weight", 0),
                ("maxWeight", 0),
                ("size", _item.GetItemSizeLocale(SharedStorageSystem.DefaultStorageMaxItemSize))));

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
        public void BuildEntityList(EntityUid entity, StorageComponent component)
        {
            var storedCount = component.Container.ContainedEntities.Count;
            var list = new List<EntityListData>(storedCount);

            foreach (var uid in component.Container.ContainedEntities)
            {
                list.Add(new EntityListData(uid));
            }

            EntityList.PopulateList(list);

            SetStorageInformation((entity, component));
        }

        private void SetStorageInformation(Entity<StorageComponent> uid)
        {
            //todo: text is the straight agenda. What about anything else?
            if (uid.Comp.MaxSlots == null)
            {
                _information.SetMarkup(Loc.GetString("comp-storage-window-weight",
                    ("weight", _storage.GetCumulativeItemSizes(uid, uid.Comp)),
                    ("maxWeight", uid.Comp.MaxTotalWeight),
                    ("size", _item.GetItemSizeLocale(_storage.GetMaxItemSize((uid, uid.Comp))))));
            }
            else
            {
                _information.SetMarkup(Loc.GetString("comp-storage-window-slots",
                    ("itemCount", uid.Comp.Container.ContainedEntities.Count),
                    ("maxCount", uid.Comp.MaxSlots),
                    ("size", _item.GetItemSizeLocale(_storage.GetMaxItemSize((uid, uid.Comp))))));
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

            _entityManager.TryGetComponent(entity, out StackComponent? stack);
            _entityManager.TryGetComponent(entity, out ItemComponent? item);
            var count = stack?.Count ?? 1;

            var spriteView = new SpriteView
            {
                HorizontalAlignment = HAlignment.Left,
                VerticalAlignment = VAlignment.Center,
                SetSize = new Vector2(32.0f, 32.0f),
                OverrideDirection = Direction.South,
            };
            spriteView.SetEntity(entity);
            button.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 2,
                Children =
                    {
                        spriteView,
                        new Label
                        {
                            HorizontalExpand = true,
                            ClipText = true,
                            Text = _entityManager.GetComponent<MetaDataComponent>(Identity.Entity(entity, _entityManager)).EntityName +
                                   (count > 1 ? $" x {count}" : string.Empty)
                        },
                        new Label
                        {
                            Align = Label.AlignMode.Right,
                            Text = item?.Size != null
                                ? $"{_item.GetItemSizeWeight(item.Size)}"
                                : Loc.GetString("comp-storage-no-item-size")
                        }
                    }
            });
            button.StyleClasses.Add(StyleNano.StyleClassStorageButton);
            button.EnableAllKeybinds = true;
        }
    }
}
