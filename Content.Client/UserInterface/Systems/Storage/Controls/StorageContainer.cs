using System.Linq;
using System.Numerics;
using Content.Client.Items.Systems;
using Content.Client.Storage.Systems;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed class StorageContainer : BoxContainer
{
    [Dependency] private readonly IEntityManager _entity = default!;
    private readonly StorageUIController _storageController;
    private ItemSystem? _itemSystem;
    private StorageSystem? _storageSystem;

    public EntityUid? StorageEntity;

    private readonly GridContainer _pieceGrid;
    private readonly GridContainer _backgroundGrid;
    private readonly GridContainer _sidebar;
    private readonly Label _nameLabel;

    public event Action<GUIBoundKeyEventArgs, ItemGridPiece>? OnPiecePressed;
    public event Action<GUIBoundKeyEventArgs, ItemGridPiece>? OnPieceUnpressed;

    private readonly string _emptyTexturePath = "Storage/tile_empty";
    private Texture? _emptyTexture;
    private readonly string _blockedTexturePath = "Storage/tile_blocked";
    private Texture? _blockedTexture;
    private readonly string _exitTexturePath = "Storage/exit";
    private Texture? _exitTexture;
    private readonly string _sidebarTopTexturePath = "Storage/sidebar_top";
    private Texture? _sidebarTopTexture;
    private readonly string _sidebarMiddleTexturePath = "Storage/sidebar_mid";
    private Texture? _sidebarMiddleTexture;
    private readonly string _sidebarBottomTexturePath = "Storage/sidebar_bottom";
    private Texture? _sidebarBottomTexture;

    public StorageContainer()
    {
        IoCManager.InjectDependencies(this);

        _storageController = UserInterfaceManager.GetUIController<StorageUIController>();

        OnThemeUpdated();

        _nameLabel = new Label
        {
            ReservesSpace = true,
            Visible = false,
            HorizontalAlignment = HAlignment.Left
        };

        _sidebar = new GridContainer
        {
            HSeparationOverride = 0,
            VSeparationOverride = 0,
            Columns = 1
        };

        _pieceGrid = new GridContainer
        {
            HSeparationOverride = 0,
            VSeparationOverride = 0
        };

        _backgroundGrid = new GridContainer
        {
            HSeparationOverride = 0,
            VSeparationOverride = 0
        };

        var container = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        _sidebar,
                        new Control
                        {
                            Children =
                            {
                                _backgroundGrid,
                                _pieceGrid
                            }
                        }
                    }
                },
                _nameLabel
            }
        };

        AddChild(container);
    }

    protected override void OnThemeUpdated()
    {
        base.OnThemeUpdated();

        _emptyTexture = Theme.ResolveTextureOrNull(_emptyTexturePath)?.Texture;
        _blockedTexture = Theme.ResolveTextureOrNull(_blockedTexturePath)?.Texture;
        _exitTexture = Theme.ResolveTextureOrNull(_exitTexturePath)?.Texture;
        _sidebarTopTexture = Theme.ResolveTextureOrNull(_sidebarTopTexturePath)?.Texture;
        _sidebarMiddleTexture = Theme.ResolveTextureOrNull(_sidebarMiddleTexturePath)?.Texture;
        _sidebarBottomTexture = Theme.ResolveTextureOrNull(_sidebarBottomTexturePath)?.Texture;
    }

    public void UpdateContainer(Entity<StorageComponent>? entity)
    {
        Visible = entity != null;
        StorageEntity = entity;
        if (entity == null)
            return;

        _nameLabel.Text = _entity.GetComponent<MetaDataComponent>(entity.Value).EntityName;

        BuildGridRepresentation(entity.Value);
    }

    private void BuildGridRepresentation(Entity<StorageComponent> entity)
    {
        var comp = entity.Comp;
        if (!comp.StorageGrid.Any())
            return;

        var boundingGrid = SharedStorageSystem.GetBoundingBox(comp.StorageGrid);
        var totalWidth = boundingGrid.Width + 1;

        _backgroundGrid.Children.Clear();
        _backgroundGrid.Rows = boundingGrid.Height;
        _backgroundGrid.Columns = totalWidth;
        for (var y = boundingGrid.Bottom; y <= boundingGrid.Top; y++)
        {
            for (var x = boundingGrid.Left; x <= boundingGrid.Right; x++)
            {
                var texture = comp.StorageGrid.Any(g => g.Contains(x, y))
                    ? _emptyTexture
                    : _blockedTexture;

                _backgroundGrid.AddChild(new StorageBackgroundCell(new Vector2i(x, y))
                {
                    Texture = texture,
                    TextureScale = new Vector2(2, 2)
                });
            }
        }

        #region Sidebar
        _sidebar.Children.Clear();
        _sidebar.Rows = boundingGrid.Height + 1;
        //todo this should change when there is a parent container to return to.
        var exitButton = new TextureButton
        {
            TextureNormal = _exitTexture,
            Scale = new Vector2(2, 2),
        };
        exitButton.OnPressed += _ =>
        {
            if (_entity.TryGetComponent<UserInterfaceComponent>(entity, out var ui))
                ui.OpenInterfaces.GetValueOrDefault(StorageComponent.StorageUiKey.Key)?.Close();
        };
        var exitContainer = new BoxContainer
        {
            Children =
            {
                new TextureRect
                {
                    Texture = _sidebarTopTexture,
                    TextureScale = new Vector2(2, 2),
                    Children =
                    {
                        exitButton
                    }
                }
            }
        };
        _sidebar.AddChild(exitContainer);
        for (var i = 0; i < boundingGrid.Height - 1; i++)
        {
            _sidebar.AddChild(new TextureRect
            {
                Texture = _sidebarMiddleTexture,
                TextureScale = new Vector2(2, 2),
            });
        }
        _sidebar.AddChild(new TextureRect
        {
            Texture = _sidebarBottomTexture,
            TextureScale = new Vector2(2, 2),
        });
        #endregion

        BuildItemPieces(entity);
    }

    public void BuildItemPieces(Entity<StorageComponent> entity)
    {
        if (!entity.Comp.StorageGrid.Any())
            return;

        var boundingGrid = SharedStorageSystem.GetBoundingBox(entity.Comp.StorageGrid);
        var size = _emptyTexture!.Size * 2;

        _pieceGrid.Children.Clear();
        _pieceGrid.Rows = boundingGrid.Height;
        _pieceGrid.Columns = boundingGrid.Width + 1;
        for (var y = boundingGrid.Bottom; y <= boundingGrid.Top; y++)
        {
            for (var x = boundingGrid.Left; x <= boundingGrid.Right; x++)
            {
                var currentPosition = new Vector2i(x, y);
                var item = entity.Comp.StoredItems
                    .Where(pair => pair.Value.Position == currentPosition)
                    .FirstOrNull();

                var control = new Control
                {
                    MinSize = size
                };

                if (item != null)
                {
                    var itemEnt = _entity.GetEntity(item.Value.Key);

                    if (_entity.TryGetComponent<ItemComponent>(itemEnt, out var itemEntComponent))
                    {
                        var gridPiece = new ItemGridPiece((itemEnt, itemEntComponent), item.Value.Value, _entity)
                        {
                            MinSize = size,
                        };
                        gridPiece.OnPiecePressed += OnPiecePressed;
                        gridPiece.OnPieceUnpressed += OnPieceUnpressed;

                        control.AddChild(gridPiece);
                    }
                }

                _pieceGrid.AddChild(control);
            }
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        Vector2i? draggingOrigin = null;

        foreach (var control in _backgroundGrid.Children)
        {
            if (control is not StorageBackgroundCell cell)
                continue;

            cell.ModulateSelfOverride = Color.Black;

            if (_storageController.CurrentlyDragging is not { } dragging)
                continue;

            if (cell.SizeBox.Contains(UserInterfaceManager.MousePositionScaled.Position
                    - cell.GlobalPosition - dragging.GetCenterOffset() * 2f + _emptyTexture!.Size * UIScale / 2))
            {
                draggingOrigin = cell.Location;
            }
        }

        if (draggingOrigin is not {} origin)
            return;

        _itemSystem ??= _entity.System<ItemSystem>();
        _storageSystem ??= _entity.System<StorageSystem>();

        var itemShape = _itemSystem.GetAdjustedItemShape(
            (_storageController.CurrentlyDragging!.Entity, null),
            _storageController.CurrentlyDragging.Location.Rotation,
            origin);
        var itemBounding = SharedStorageSystem.GetBoundingBox(itemShape);

        if (!_entity.TryGetComponent<StorageComponent>(StorageEntity, out var storageComponent))
            return;

        var storageBounding = (Box2) SharedStorageSystem.GetBoundingBox(storageComponent.StorageGrid);
        if (!storageBounding.Contains(itemBounding))
            return;

        var validLocation = _storageSystem.ItemFitsInGridLocation(
            (_storageController.CurrentlyDragging!.Entity, null),
            (StorageEntity.Value, storageComponent),
            origin);

        for (var y = itemBounding.Bottom; y <= itemBounding.Top; y++)
        {
            for (var x = itemBounding.Left; x <= itemBounding.Right; x++)
            {
                if (GetBackgroundCell(x, y) is { } cell && itemShape.Any(b => b.Contains(x, y)))
                {
                    cell.ModulateSelfOverride = validLocation ? Color.Green : Color.Red;
                }
            }
        }
    }

    public StorageBackgroundCell? GetBackgroundCell(int x, int y)
    {
        return (StorageBackgroundCell) _backgroundGrid.GetChild(y * _backgroundGrid.Columns + x);
    }

    public sealed class StorageBackgroundCell : TextureRect
    {
        public readonly Vector2i Location;

        public StorageBackgroundCell(Vector2i location)
        {
            Location = location;
            MouseFilter = MouseFilterMode.Pass;
            ReservesSpace = true;
        }
    }
}
