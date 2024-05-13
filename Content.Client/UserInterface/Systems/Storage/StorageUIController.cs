using System.Numerics;
using Content.Client.Examine;
using Content.Client.Hands.Systems;
using Content.Client.Interaction;
using Content.Client.Storage.Systems;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Storage.Controls;
using Content.Client.Verbs.UI;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Storage;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Storage;

public sealed class StorageUIController : UIController, IOnSystemChanged<StorageSystem>
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly DragDropHelper<ItemGridPiece> _menuDragHelper;
    private StorageContainer? _container;

    private Vector2? _lastContainerPosition;

    private HotbarGui? Hotbar => UIManager.GetActiveUIWidgetOrNull<HotbarGui>();

    public ItemGridPiece? DraggingGhost;
    public Angle DraggingRotation = Angle.Zero;
    public bool StaticStorageUIEnabled;
    public bool OpaqueStorageWindow;

    public bool IsDragging => _menuDragHelper.IsDragging;
    public ItemGridPiece? CurrentlyDragging => _menuDragHelper.Dragged;

    public StorageUIController()
    {
        _menuDragHelper = new DragDropHelper<ItemGridPiece>(OnMenuBeginDrag, OnMenuContinueDrag, OnMenuEndDrag);
    }

    public override void Initialize()
    {
        base.Initialize();

        _configuration.OnValueChanged(CCVars.StaticStorageUI, OnStaticStorageChanged, true);
        _configuration.OnValueChanged(CCVars.OpaqueStorageWindow, OnOpaqueWindowChanged, true);
    }

    public void OnSystemLoaded(StorageSystem system)
    {
        _input.FirstChanceOnKeyEvent += OnMiddleMouse;
        system.StorageUpdated += OnStorageUpdated;
        system.StorageOrderChanged += OnStorageOrderChanged;
    }

    public void OnSystemUnloaded(StorageSystem system)
    {
        _input.FirstChanceOnKeyEvent -= OnMiddleMouse;
        system.StorageUpdated -= OnStorageUpdated;
        system.StorageOrderChanged -= OnStorageOrderChanged;
    }

    private void OnStorageOrderChanged(Entity<StorageComponent>? nullEnt)
    {
        if (_container == null)
            return;

        if (IsDragging)
            _menuDragHelper.EndDrag();

        _container.UpdateContainer(nullEnt);

        if (nullEnt is not null)
        {
            // center it if we knock it off screen somehow.
            if (!StaticStorageUIEnabled &&
                (_lastContainerPosition == null ||
                _lastContainerPosition.Value.X < 0 ||
                _lastContainerPosition.Value.Y < 0 ||
                _lastContainerPosition.Value.X > _ui.WindowRoot.Width ||
                _lastContainerPosition.Value.Y > _ui.WindowRoot.Height))
            {
                _container.OpenCenteredAt(new Vector2(0.5f, 0.75f));
            }
            else
            {
                _container.Open();

                var pos = !StaticStorageUIEnabled && _lastContainerPosition != null
                    ? _lastContainerPosition.Value
                    : Vector2.Zero;

                LayoutContainer.SetPosition(_container, pos);
            }

            if (StaticStorageUIEnabled)
            {
                // we have to orphan it here because Open() sets the parent.
                _container.Orphan();
                Hotbar?.StorageContainer.AddChild(_container);
            }
            _lastContainerPosition = _container.GlobalPosition;
        }
        else
        {
            _lastContainerPosition = _container.GlobalPosition;
            _container.Close();
        }
    }

    private void OnStaticStorageChanged(bool obj)
    {
        if (StaticStorageUIEnabled == obj)
            return;

        StaticStorageUIEnabled = obj;
        _lastContainerPosition = null;

        if (_container == null)
            return;

        if (!_container.IsOpen)
            return;

        _container.Orphan();
        if (StaticStorageUIEnabled)
        {
            Hotbar?.StorageContainer.AddChild(_container);
        }
        else
        {
            _ui.WindowRoot.AddChild(_container);
        }

        if (_entity.TryGetComponent<StorageComponent>(_container.StorageEntity, out var comp))
            OnStorageOrderChanged((_container.StorageEntity.Value, comp));
    }

    private void OnOpaqueWindowChanged(bool obj)
    {
        if (OpaqueStorageWindow == obj)
            return;
        OpaqueStorageWindow = obj;
        _container?.BuildBackground();
    }

    /// One might ask, Hey Emo, why are you parsing raw keyboard input just to rotate a rectangle?
    /// The answer is, that input bindings regarding mouse inputs are always intercepted by the UI,
    /// thus, if i want to be able to rotate my damn piece anywhere on the screen,
    /// I have to side-step all of the input handling. Cheers.
    private void OnMiddleMouse(KeyEventArgs keyEvent, KeyEventType type)
    {
        if (keyEvent.Handled)
            return;

        if (type != KeyEventType.Down)
            return;

        //todo there's gotta be a method for this in InputManager just expose it to content I BEG.
        if (!_input.TryGetKeyBinding(ContentKeyFunctions.RotateStoredItem, out var binding))
            return;
        if (binding.BaseKey != keyEvent.Key)
            return;

        if (keyEvent.Shift &&
            !(binding.Mod1 == Keyboard.Key.Shift ||
              binding.Mod2 == Keyboard.Key.Shift ||
              binding.Mod3 == Keyboard.Key.Shift))
            return;

        if (keyEvent.Alt &&
            !(binding.Mod1 == Keyboard.Key.Alt ||
              binding.Mod2 == Keyboard.Key.Alt ||
              binding.Mod3 == Keyboard.Key.Alt))
            return;

        if (keyEvent.Control &&
            !(binding.Mod1 == Keyboard.Key.Control ||
              binding.Mod2 == Keyboard.Key.Control ||
              binding.Mod3 == Keyboard.Key.Control))
            return;

        if (!IsDragging && _entity.System<HandsSystem>().GetActiveHandEntity() == null)
            return;

        //clamp it to a cardinal.
        DraggingRotation = (DraggingRotation + Math.PI / 2f).GetCardinalDir().ToAngle();
        if (DraggingGhost != null)
            DraggingGhost.Location.Rotation = DraggingRotation;

        if (IsDragging || (_container != null && UIManager.CurrentlyHovered == _container))
            keyEvent.Handle();
    }

    private void OnStorageUpdated(Entity<StorageComponent> uid)
    {
        if (_container?.StorageEntity != uid)
            return;

        _container.BuildItemPieces();
    }

    public void RegisterStorageContainer(StorageContainer container)
    {
        if (_container != null)
        {
            container.OnPiecePressed -= OnPiecePressed;
            container.OnPieceUnpressed -= OnPieceUnpressed;
        }

        _container = container;
        container.OnPiecePressed += OnPiecePressed;
        container.OnPieceUnpressed += OnPieceUnpressed;

        if (!StaticStorageUIEnabled)
            _container.Orphan();
    }

    private void OnPiecePressed(GUIBoundKeyEventArgs args, ItemGridPiece control)
    {
        if (IsDragging || !_container?.IsOpen == true)
            return;

        if (args.Function == ContentKeyFunctions.MoveStoredItem)
        {
            DraggingRotation = control.Location.Rotation;

            _menuDragHelper.MouseDown(control);
            _menuDragHelper.Update(0f);

            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.SaveItemLocation)
        {
            if (_container?.StorageEntity is not {} storage)
                return;

            _entity.RaisePredictiveEvent(new StorageSaveItemLocationEvent(
                _entity.GetNetEntity(control.Entity),
                _entity.GetNetEntity(storage)));
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            _entity.System<ExamineSystem>().DoExamine(control.Entity);
            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            UIManager.GetUIController<VerbMenuUIController>().OpenVerbMenu(control.Entity);
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            _entity.RaisePredictiveEvent(
                new InteractInventorySlotEvent(_entity.GetNetEntity(control.Entity), altInteract: false));
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            _entity.RaisePredictiveEvent(new InteractInventorySlotEvent(_entity.GetNetEntity(control.Entity), altInteract: true));
            args.Handle();
        }
    }

    private void OnPieceUnpressed(GUIBoundKeyEventArgs args, ItemGridPiece control)
    {
        if (args.Function != ContentKeyFunctions.MoveStoredItem)
            return;

        if (_container?.StorageEntity is not { } storageEnt|| !_entity.TryGetComponent<StorageComponent>(storageEnt, out var storageComp))
            return;

        if (DraggingGhost is { } draggingGhost)
        {
            var dragEnt = draggingGhost.Entity;
            var dragLoc = draggingGhost.Location;
            var itemSys = _entity.System<SharedItemSystem>();

            var position = _container.GetMouseGridPieceLocation(dragEnt, dragLoc);
            var itemBounding = itemSys.GetAdjustedItemShape(dragEnt, dragLoc).GetBoundingBox();
            var gridBounding = storageComp.Grid.GetBoundingBox();

            // The extended bounding box for if this is out of the window is the grid bounding box dimensions combined
            // with the item shape bounding box dimensions. Plus 1 on the left for the sidebar. This makes it so that.
            // dropping an item on the floor requires dragging it all the way out of the window.
            var left = gridBounding.Left - itemBounding.Width - 1;
            var bottom = gridBounding.Bottom - itemBounding.Height;
            var top = gridBounding.Top;
            var right = gridBounding.Right;
            var lenientBounding = new Box2i(left, bottom, right, top);

            if (lenientBounding.Contains(position))
            {
                _entity.RaisePredictiveEvent(new StorageSetItemLocationEvent(
                    _entity.GetNetEntity(draggingGhost.Entity),
                    _entity.GetNetEntity(storageEnt),
                    new ItemStorageLocation(DraggingRotation, position)));
            }
            else
            {
                _entity.RaisePredictiveEvent(new StorageRemoveItemEvent(
                    _entity.GetNetEntity(draggingGhost.Entity),
                    _entity.GetNetEntity(storageEnt)));
            }

            _container?.BuildItemPieces();
        }
        else //if we just clicked, then take it out of the bag.
        {
            _entity.RaisePredictiveEvent(new StorageInteractWithItemEvent(
                _entity.GetNetEntity(control.Entity),
                _entity.GetNetEntity(storageEnt)));
        }
        _menuDragHelper.EndDrag();
        args.Handle();
    }

    private bool OnMenuBeginDrag()
    {
        if (_menuDragHelper.Dragged is not { } dragged)
            return false;

        DraggingRotation = dragged.Location.Rotation;
        DraggingGhost = new ItemGridPiece(
            (dragged.Entity, _entity.GetComponent<ItemComponent>(dragged.Entity)),
            dragged.Location,
            _entity);
        DraggingGhost.MouseFilter = Control.MouseFilterMode.Ignore;
        DraggingGhost.Visible = true;
        DraggingGhost.Orphan();

        UIManager.PopupRoot.AddChild(DraggingGhost);
        SetDraggingRotation();
        return true;
    }

    private bool OnMenuContinueDrag(float frameTime)
    {
        if (DraggingGhost == null)
            return false;
        SetDraggingRotation();
        return true;
    }

    private void SetDraggingRotation()
    {
        if (DraggingGhost == null)
            return;

        var offset = ItemGridPiece.GetCenterOffset(
            (DraggingGhost.Entity, null),
            new ItemStorageLocation(DraggingRotation, Vector2i.Zero),
            _entity);

        // I don't know why it divides the position by 2. Hope this helps! -emo
        LayoutContainer.SetPosition(DraggingGhost, UIManager.MousePositionScaled.Position / 2 - offset );
    }

    private void OnMenuEndDrag()
    {
        if (DraggingGhost == null)
            return;
        DraggingGhost.Visible = false;
        DraggingGhost = null;
        DraggingRotation = Angle.Zero;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _menuDragHelper.Update(args.DeltaSeconds);

        if (!StaticStorageUIEnabled && _container?.Parent != null && _lastContainerPosition != null)
            _lastContainerPosition = _container.GlobalPosition;
    }
}
