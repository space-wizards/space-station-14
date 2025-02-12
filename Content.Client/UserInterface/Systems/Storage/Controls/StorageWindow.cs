using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Client.Items.Systems;
using Content.Client.Storage;
using Content.Client.Storage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed class StorageWindow : BaseWindow
{
    [Dependency] private readonly IEntityManager _entity = default!;
    private readonly StorageUIController _storageController;

    public EntityUid? StorageEntity;

    private readonly GridContainer _pieceGrid;
    private readonly GridContainer _backgroundGrid;
    private readonly GridContainer _sidebar;

    private Control _titleContainer;
    private Label _titleLabel;

    // Needs to be nullable in case a piece is in default spot.
    private readonly Dictionary<EntityUid, (ItemStorageLocation? Loc, ItemGridPiece Control)> _pieces = new();
    private readonly List<Control> _controlGrid = new();

    private ValueList<EntityUid> _contained = new();
    private ValueList<EntityUid> _toRemove = new();

    private TextureButton? _backButton;

    private bool _isDirty;

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

    public StorageWindow()
    {
        IoCManager.InjectDependencies(this);
        Resizable = false;

        _storageController = UserInterfaceManager.GetUIController<StorageUIController>();

        OnThemeUpdated();

        MouseFilter = MouseFilterMode.Stop;

        _sidebar = new GridContainer
        {
            Name = "SideBar",
            HSeparationOverride = 0,
            VSeparationOverride = 0,
            Columns = 1
        };

        _pieceGrid = new GridContainer
        {
            Name = "PieceGrid",
            HSeparationOverride = 0,
            VSeparationOverride = 0
        };

        _backgroundGrid = new GridContainer
        {
            Name = "BackgroundGrid",
            HSeparationOverride = 0,
            VSeparationOverride = 0
        };

        _titleLabel = new Label()
        {
            HorizontalExpand = true,
            Name = "StorageLabel",
            ClipText = true,
            Text = "Dummy",
            StyleClasses =
            {
                "FancyWindowTitle",
            }
        };

        _titleContainer = new PanelContainer()
        {
            StyleClasses =
            {
                "WindowHeadingBackground"
            },
            Children =
            {
                _titleLabel
            }
        };

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                _titleContainer,
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

        if (UserInterfaceManager.GetUIController<StorageUIController>().WindowTitle)
        {
            _titleLabel.Text = Identity.Name(entity.Value, _entity);
            _titleContainer.Visible = true;
        }
        else
        {
            _titleContainer.Visible = false;
        }

        BuildGridRepresentation();
    }

    private void BuildGridRepresentation()
    {
        if (!_entity.TryGetComponent<StorageComponent>(StorageEntity, out var comp) || comp.Grid.Count == 0)
            return;

        var boundingGrid = comp.Grid.GetBoundingBox();

        BuildBackground();

        #region Sidebar
        _sidebar.Children.Clear();
        var rows = boundingGrid.Height + 1;
        _sidebar.Rows = rows;

        var exitButton = new TextureButton
        {
            Name = "ExitButton",
            TextureNormal = _exitTexture,
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
            Name = "ExitContainer",
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
        var offset = 2;

        if (_entity.System<StorageSystem>().NestedStorage && rows > 0)
        {
            _backButton = new TextureButton
            {
                TextureNormal = _backTexture,
                Scale = new Vector2(2, 2),
            };
            _backButton.OnPressed += _ =>
            {
                var containerSystem = _entity.System<SharedContainerSystem>();

                if (containerSystem.TryGetContainingContainer(StorageEntity.Value, out var container) &&
                    _entity.TryGetComponent(container.Owner, out StorageComponent? storage))
                {
                    Close();

                    if (_entity.System<SharedUserInterfaceSystem>()
                        .TryGetOpenUi<StorageBoundUserInterface>(container.Owner,
                            StorageComponent.StorageUiKey.Key,
                            out var parentBui))
                    {
                        parentBui.Show();
                    }
                }
            };

            var backContainer = new BoxContainer
            {
                Name = "ExitContainer",
                Children =
                {
                    new TextureRect
                    {
                        Texture = rows > 2 ? _sidebarMiddleTexture : _sidebarBottomTexture,
                        TextureScale = new Vector2(2, 2),
                        Children =
                        {
                            _backButton,
                        }
                    }
                }
            };

            _sidebar.AddChild(backContainer);
        }

        var fillerRows = rows - offset;

        for (var i = 0; i < fillerRows; i++)
        {
            _sidebar.AddChild(new TextureRect
            {
                Texture = i != (fillerRows - 1) ? _sidebarMiddleTexture : _sidebarBottomTexture,
                TextureScale = new Vector2(2, 2),
            });
        }

        #endregion

        FlagDirty();
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

    public void Reclaim(ItemStorageLocation location, ItemGridPiece draggingGhost)
    {
        draggingGhost.OnPiecePressed += OnPiecePressed;
        draggingGhost.OnPieceUnpressed += OnPieceUnpressed;
        _pieces[draggingGhost.Entity] = (location, draggingGhost);
        draggingGhost.Location = location;
        var controlIndex = GetGridIndex(draggingGhost);
        _controlGrid[controlIndex].AddChild(draggingGhost);
    }

    private int GetGridIndex(ItemGridPiece piece)
    {
        return piece.Location.Position.X + piece.Location.Position.Y * _pieceGrid.Columns;
    }

    public void FlagDirty()
    {
        _isDirty = true;
    }

    public void RemoveGrid(ItemGridPiece control)
    {
        control.Orphan();
        _pieces.Remove(control.Entity);
        control.OnPiecePressed -= OnPiecePressed;
        control.OnPieceUnpressed -= OnPieceUnpressed;
    }

    public void BuildItemPieces()
    {
        if (!_entity.TryGetComponent<StorageComponent>(StorageEntity, out var storageComp))
            return;

        if (storageComp.Grid.Count == 0)
            return;

        var boundingGrid = storageComp.Grid.GetBoundingBox();
        var size = _emptyTexture!.Size * 2;
        _contained.Clear();
        _contained.AddRange(storageComp.Container.ContainedEntities.Reverse());

        // Build the grid representation
        if (_pieceGrid.Rows - 1 != boundingGrid.Height || _pieceGrid.Columns - 1 != boundingGrid.Width)
        {
            _pieceGrid.Rows = boundingGrid.Height + 1;
            _pieceGrid.Columns = boundingGrid.Width + 1;
            _controlGrid.Clear();

            for (var y = boundingGrid.Bottom; y <= boundingGrid.Top; y++)
            {
                for (var x = boundingGrid.Left; x <= boundingGrid.Right; x++)
                {
                    var control = new Control
                    {
                        MinSize = size
                    };

                    _controlGrid.Add(control);
                    _pieceGrid.AddChild(control);
                }
            }
        }

        _toRemove.Clear();

        // Remove entities no longer relevant / Update existing ones
        foreach (var (ent, data) in _pieces)
        {
            if (storageComp.StoredItems.TryGetValue(ent, out var updated))
            {
                if (data.Loc.Equals(updated))
                {
                    DebugTools.Assert(data.Control.Location == updated);
                    continue;
                }

                // Update
                data.Control.Location = updated;
                var index = GetGridIndex(data.Control);
                data.Control.Orphan();
                _controlGrid[index].AddChild(data.Control);
                _pieces[ent] = (updated, data.Control);
                continue;
            }

            _toRemove.Add(ent);
        }

        foreach (var ent in _toRemove)
        {
            _pieces.Remove(ent, out var data);
            data.Control.Orphan();
        }

        // Add new ones
        foreach (var (ent, loc) in storageComp.StoredItems)
        {
            if (_pieces.TryGetValue(ent, out var existing))
            {
                DebugTools.Assert(existing.Loc == loc);
                continue;
            }

            if (_entity.TryGetComponent<ItemComponent>(ent, out var itemEntComponent))
            {
                var gridPiece = new ItemGridPiece((ent, itemEntComponent), loc, _entity)
                {
                    MinSize = size,
                    Marked = _contained.IndexOf(ent) switch
                    {
                        0 => ItemGridPieceMarks.First,
                        1 => ItemGridPieceMarks.Second,
                        _ => null,
                    }
                };
                gridPiece.OnPiecePressed += OnPiecePressed;
                gridPiece.OnPieceUnpressed += OnPieceUnpressed;
                var controlIndex = loc.Position.X + loc.Position.Y * (boundingGrid.Width + 1);

                _controlGrid[controlIndex].AddChild(gridPiece);
                _pieces[ent] = (loc, gridPiece);
            }
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!IsOpen)
            return;

        if (_isDirty)
        {
            _isDirty = false;
            BuildItemPieces();
        }

        var containerSystem = _entity.System<SharedContainerSystem>();

        if (_backButton != null)
        {
            if (StorageEntity != null && _entity.System<StorageSystem>().NestedStorage)
            {
                if (containerSystem.TryGetContainingContainer(StorageEntity.Value, out var container) &&
                    _entity.HasComponent<StorageComponent>(container.Owner))
                {
                    _backButton.Visible = true;
                }
                else
                {
                    _backButton.Visible = false;
                }
            }
            // Hide the button.
            else
            {
                _backButton.Visible = false;
            }
        }

        var itemSystem = _entity.System<ItemSystem>();
        var storageSystem = _entity.System<StorageSystem>();
        var handsSystem = _entity.System<HandsSystem>();

        foreach (var child in _backgroundGrid.Children)
        {
            child.ModulateSelfOverride = Color.FromHex("#222222");
        }

        if (UserInterfaceManager.CurrentlyHovered is StorageWindow con && con != this)
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
            var marked = new ValueList<Control>();

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
}
