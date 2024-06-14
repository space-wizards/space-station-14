using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Client.Items.Systems;
using Content.Client.Storage.Systems;
using Content.Shared.Input;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed class StorageContainer : BaseWindow
{
    [Dependency] private readonly IEntityManager _entity = default!;
    private readonly StorageUIController _storageController;

    public EntityUid? StorageEntity;

    private readonly GridContainer _pieceGrid;
    private readonly GridContainer _backgroundGrid;
    private readonly GridContainer _sidebar;

    public event Action<GUIBoundKeyEventArgs, ItemGridPiece>? OnPiecePressed;
    public event Action<GUIBoundKeyEventArgs, ItemGridPiece>? OnPieceUnpressed;

    private readonly string _emptyTexturePath = "Storage/tile_empty";
    private Texture? _emptyTexture;
    private readonly string _blockedTexturePath = "Storage/tile_blocked";
    private Texture? _blockedTexture;
    private readonly string _emptyOpaqueTexturePath = "Storage/tile_empty_opaque";
    private Texture? _emptyOpaqueTexture;
    private readonly string _blockedOpaqueTexturePath = "Storage/tile_blocked_opaque";
    private Texture? _blockedOpaqueTexture;
    private readonly string _exitTexturePath = "Storage/exit";
    private Texture? _exitTexture;
    private readonly string _backTexturePath = "Storage/back";
    private Texture? _backTexture;
    private readonly string _sidebarTopTexturePath = "Storage/sidebar_top";
    private Texture? _sidebarTopTexture;
    private readonly string _sidebarMiddleTexturePath = "Storage/sidebar_mid";
    private Texture? _sidebarMiddleTexture;
    private readonly string _sidebarBottomTexturePath = "Storage/sidebar_bottom";
    private Texture? _sidebarBottomTexture;
    private readonly string _sidebarFatTexturePath = "Storage/sidebar_fat";
    private Texture? _sidebarFatTexture;

    public StorageContainer()
    {
        IoCManager.InjectDependencies(this);

        _storageController = UserInterfaceManager.GetUIController<StorageUIController>();

        OnThemeUpdated();

        MouseFilter = MouseFilterMode.Stop;

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
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
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
                }
            }
        };

        AddChild(container);
    }

    protected override void OnThemeUpdated()
    {
        base.OnThemeUpdated();

        _emptyTexture = Theme.ResolveTextureOrNull(_emptyTexturePath)?.Texture;
        _blockedTexture = Theme.ResolveTextureOrNull(_blockedTexturePath)?.Texture;
        _emptyOpaqueTexture = Theme.ResolveTextureOrNull(_emptyOpaqueTexturePath)?.Texture;
        _blockedOpaqueTexture = Theme.ResolveTextureOrNull(_blockedOpaqueTexturePath)?.Texture;
        _exitTexture = Theme.ResolveTextureOrNull(_exitTexturePath)?.Texture;
        _backTexture = Theme.ResolveTextureOrNull(_backTexturePath)?.Texture;
        _sidebarTopTexture = Theme.ResolveTextureOrNull(_sidebarTopTexturePath)?.Texture;
        _sidebarMiddleTexture = Theme.ResolveTextureOrNull(_sidebarMiddleTexturePath)?.Texture;
        _sidebarBottomTexture = Theme.ResolveTextureOrNull(_sidebarBottomTexturePath)?.Texture;
        _sidebarFatTexture = Theme.ResolveTextureOrNull(_sidebarFatTexturePath)?.Texture;
    }

    public void UpdateContainer(Entity<StorageComponent>? entity)
    {
        Visible = entity != null;
        StorageEntity = entity;
        if (entity == null)
            return;

        BuildGridRepresentation();
    }

    private void BuildGridRepresentation()
    {
        if (!_entity.TryGetComponent<StorageComponent>(StorageEntity, out var comp) || !comp.Grid.Any())
            return;

        var boundingGrid = comp.Grid.GetBoundingBox();

        BuildBackground();

        #region Sidebar
        _sidebar.Children.Clear();
        _sidebar.Rows = boundingGrid.Height + 1;
        var exitButton = new TextureButton
        {
            TextureNormal = _entity.System<StorageSystem>().OpenStorageAmount == 1
                ?_exitTexture
                : _backTexture,
            Scale = new Vector2(2, 2),
        };
        exitButton.OnPressed += _ =>
        {
            Close();
        };
        exitButton.OnKeyBindDown += args =>
        {
            // it just makes sense...
            if (!args.Handled && args.Function == ContentKeyFunctions.ActivateItemInWorld)
            {
                Close();
                args.Handle();
            }
        };
        var exitContainer = new BoxContainer
        {
            Children =
            {
                new TextureRect
                {
                    Texture = boundingGrid.Height != 0
                        ? _sidebarTopTexture
                        : _sidebarFatTexture,
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

        if (boundingGrid.Height > 0)
        {
            _sidebar.AddChild(new TextureRect
            {
                Texture = _sidebarBottomTexture,
                TextureScale = new Vector2(2, 2),
            });
        }

        #endregion

        BuildItemPieces();
    }

    public void BuildBackground()
    {
        if (!_entity.TryGetComponent<StorageComponent>(StorageEntity, out var comp) || !comp.Grid.Any())
            return;

        var boundingGrid = comp.Grid.GetBoundingBox();

        var emptyTexture = _storageController.OpaqueStorageWindow
            ? _emptyOpaqueTexture
            : _emptyTexture;
        var blockedTexture = _storageController.OpaqueStorageWindow
            ? _blockedOpaqueTexture
            : _blockedTexture;

        _backgroundGrid.Children.Clear();
        _backgroundGrid.Rows = boundingGrid.Height + 1;
        _backgroundGrid.Columns = boundingGrid.Width + 1;
        for (var y = boundingGrid.Bottom; y <= boundingGrid.Top; y++)
        {
            for (var x = boundingGrid.Left; x <= boundingGrid.Right; x++)
            {
                var texture = comp.Grid.Contains(x, y)
                    ? emptyTexture
                    : blockedTexture;

                _backgroundGrid.AddChild(new TextureRect
                {
                    Texture = texture,
                    TextureScale = new Vector2(2, 2)
                });
            }
        }
    }

    public void BuildItemPieces()
    {
        if (!_entity.TryGetComponent<StorageComponent>(StorageEntity, out var storageComp))
            return;

        if (!storageComp.Grid.Any())
            return;

        var boundingGrid = storageComp.Grid.GetBoundingBox();
        var size = _emptyTexture!.Size * 2;
        var containedEntities = storageComp.Container.ContainedEntities.Reverse().ToArray();

        //todo. at some point, we may want to only rebuild the pieces that have actually received new data.

        _pieceGrid.RemoveAllChildren();
        _pieceGrid.Rows = boundingGrid.Height + 1;
        _pieceGrid.Columns = boundingGrid.Width + 1;
        for (var y = boundingGrid.Bottom; y <= boundingGrid.Top; y++)
        {
            for (var x = boundingGrid.Left; x <= boundingGrid.Right; x++)
            {
                var control = new Control
                {
                    MinSize = size
                };

                var currentPosition = new Vector2i(x, y);

                foreach (var (itemEnt, itemPos) in storageComp.StoredItems)
                {
                    if (itemPos.Position != currentPosition)
                        continue;

                    if (_entity.TryGetComponent<ItemComponent>(itemEnt, out var itemEntComponent))
                    {
                        ItemGridPiece gridPiece;

                        if (_storageController.CurrentlyDragging?.Entity is { } dragging
                            && dragging == itemEnt)
                        {
                            _storageController.CurrentlyDragging.Orphan();
                            gridPiece = _storageController.CurrentlyDragging;
                        }
                        else
                        {
                            gridPiece = new ItemGridPiece((itemEnt, itemEntComponent), itemPos, _entity)
                            {
                                MinSize = size,
                                Marked = Array.IndexOf(containedEntities, itemEnt) switch
                                {
                                    0 => ItemGridPieceMarks.First,
                                    1 => ItemGridPieceMarks.Second,
                                    _ => null,
                                }
                            };
                            gridPiece.OnPiecePressed += OnPiecePressed;
                            gridPiece.OnPieceUnpressed += OnPieceUnpressed;
                        }

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

        if (!IsOpen)
            return;

        var itemSystem = _entity.System<ItemSystem>();
        var storageSystem = _entity.System<StorageSystem>();
        var handsSystem = _entity.System<HandsSystem>();

        foreach (var child in _backgroundGrid.Children)
        {
            child.ModulateSelfOverride = Color.FromHex("#222222");
        }

        if (UserInterfaceManager.CurrentlyHovered is StorageContainer con && con != this)
            return;

        if (!_entity.TryGetComponent<StorageComponent>(StorageEntity, out var storageComponent))
            return;

        EntityUid currentEnt;
        ItemStorageLocation currentLocation;
        var usingInHand = false;
        if (_storageController.IsDragging && _storageController.DraggingGhost is { } dragging)
        {
            currentEnt = dragging.Entity;
            currentLocation = dragging.Location;
        }
        else if (handsSystem.GetActiveHandEntity() is { } handEntity &&
                 storageSystem.CanInsert(StorageEntity.Value, handEntity, out _, storageComp: storageComponent, ignoreLocation: true))
        {
            currentEnt = handEntity;
            currentLocation = new ItemStorageLocation(_storageController.DraggingRotation, Vector2i.Zero);
            usingInHand = true;
        }
        else
        {
            return;
        }

        if (!_entity.TryGetComponent<ItemComponent>(currentEnt, out var itemComp))
            return;

        var origin = GetMouseGridPieceLocation((currentEnt, itemComp), currentLocation);

        var itemShape = itemSystem.GetAdjustedItemShape(
            (currentEnt, itemComp),
            currentLocation.Rotation,
            origin);
        var itemBounding = itemShape.GetBoundingBox();

        var validLocation = storageSystem.ItemFitsInGridLocation(
            (currentEnt, itemComp),
            (StorageEntity.Value, storageComponent),
            origin,
            currentLocation.Rotation);

        foreach (var locations in storageComponent.SavedLocations)
        {
            if (!_entity.TryGetComponent<MetaDataComponent>(currentEnt, out var meta) || meta.EntityName != locations.Key)
                continue;

            float spot = 0;
            var marked = new List<Control>();

            foreach (var location in locations.Value)
            {
                var shape = itemSystem.GetAdjustedItemShape(currentEnt, location);
                var bound = shape.GetBoundingBox();

                var spotFree = storageSystem.ItemFitsInGridLocation(currentEnt, StorageEntity.Value, location);

                if (spotFree)
                    spot++;

                for (var y = bound.Bottom; y <= bound.Top; y++)
                {
                    for (var x = bound.Left; x <= bound.Right; x++)
                    {
                        if (TryGetBackgroundCell(x, y, out var cell) && shape.Contains(x, y) && !marked.Contains(cell))
                        {
                            marked.Add(cell);
                            cell.ModulateSelfOverride = spotFree
                                ? Color.FromHsv((0.18f, 1 / spot, 0.5f / spot + 0.5f, 1f))
                                : Color.FromHex("#2222CC");
                        }
                    }
                }
            }
        }

        var validColor = usingInHand ? Color.Goldenrod : Color.FromHex("#1E8000");

        for (var y = itemBounding.Bottom; y <= itemBounding.Top; y++)
        {
            for (var x = itemBounding.Left; x <= itemBounding.Right; x++)
            {
                if (TryGetBackgroundCell(x, y, out var cell) && itemShape.Contains(x, y))
                {
                    cell.ModulateSelfOverride = validLocation ? validColor : Color.FromHex("#B40046");
                }
            }
        }
    }

    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        if (_storageController.StaticStorageUIEnabled)
            return DragMode.None;

        if (_sidebar.SizeBox.Contains(relativeMousePos - _sidebar.Position))
        {
            return DragMode.Move;
        }

        return DragMode.None;
    }

    public Vector2i GetMouseGridPieceLocation(Entity<ItemComponent?> entity, ItemStorageLocation location)
    {
        var origin = Vector2i.Zero;

        if (StorageEntity != null)
            origin = _entity.GetComponent<StorageComponent>(StorageEntity.Value).Grid.GetBoundingBox().BottomLeft;

        var textureSize = (Vector2) _emptyTexture!.Size * 2;
        var position = ((UserInterfaceManager.MousePositionScaled.Position
                         - _backgroundGrid.GlobalPosition
                         - ItemGridPiece.GetCenterOffset(entity, location, _entity) * 2
                         + textureSize / 2f)
                        / textureSize).Floored() + origin;
        return position;
    }

    public bool TryGetBackgroundCell(int x, int y, [NotNullWhen(true)] out Control? cell)
    {
        cell = null;

        if (!_entity.TryGetComponent<StorageComponent>(StorageEntity, out var storageComponent))
            return false;
        var boundingBox = storageComponent.Grid.GetBoundingBox();
        x -= boundingBox.Left;
        y -= boundingBox.Bottom;

        if (x < 0 ||
            x >= _backgroundGrid.Columns ||
            y < 0 ||
            y >= _backgroundGrid.Rows)
        {
            return false;
        }

        cell = _backgroundGrid.GetChild(y * _backgroundGrid.Columns + x);
        return true;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (!IsOpen)
            return;

        var storageSystem = _entity.System<StorageSystem>();
        var handsSystem = _entity.System<HandsSystem>();

        if (args.Function == ContentKeyFunctions.MoveStoredItem && StorageEntity != null)
        {
            if (handsSystem.GetActiveHandEntity() is { } handEntity &&
                storageSystem.CanInsert(StorageEntity.Value, handEntity, out _))
            {
                var pos = GetMouseGridPieceLocation((handEntity, null),
                    new ItemStorageLocation(_storageController.DraggingRotation, Vector2i.Zero));

                var insertLocation = new ItemStorageLocation(_storageController.DraggingRotation, pos);
                if (storageSystem.ItemFitsInGridLocation(
                        (handEntity, null),
                        (StorageEntity.Value, null),
                        insertLocation))
                {
                    _entity.RaisePredictiveEvent(new StorageInsertItemIntoLocationEvent(
                        _entity.GetNetEntity(handEntity),
                        _entity.GetNetEntity(StorageEntity.Value),
                        insertLocation));
                    _storageController.DraggingRotation = Angle.Zero;
                    args.Handle();
                }
            }
        }
    }

    public override void Close()
    {
        base.Close();

        if (StorageEntity == null)
            return;

        _entity.System<StorageSystem>().CloseStorageWindow(StorageEntity.Value);
    }
}
