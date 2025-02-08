using System.Numerics;
using Content.Client.Examine;
using Content.Client.Hands.Systems;
using Content.Client.Interaction;
using Content.Client.Storage;
using Content.Client.Storage.Systems;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Storage.Controls;
using Content.Client.Verbs.UI;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Storage;

public sealed class StorageUIController : UIController, IOnSystemChanged<StorageSystem>
{
    /*
     * Things are a bit over the shop but essentially
     * - Clicking into storagewindow is handled via storagewindow
     * - Clicking out of it is via ItemGridPiece
     * - Dragging around is handled here
     * - Drawing is handled via ItemGridPiece
     * - StorageSystem handles any sim stuff around open windows.
     */

    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [UISystemDependency] private readonly StorageSystem _storage = default!;

    private readonly DragDropHelper<ItemGridPiece> _menuDragHelper;

    public ItemGridPiece? DraggingGhost => _menuDragHelper.Dragged;
    public Angle DraggingRotation = Angle.Zero;
    public bool StaticStorageUIEnabled;
    public bool OpaqueStorageWindow;

    public bool IsDragging => _menuDragHelper.IsDragging;
    public ItemGridPiece? CurrentlyDragging => _menuDragHelper.Dragged;

    public bool WindowTitle { get; private set; } = false;

    public StorageUIController()
    {
        _menuDragHelper = new DragDropHelper<ItemGridPiece>(OnMenuBeginDrag, OnMenuContinueDrag, OnMenuEndDrag);
    }

    public override void Initialize()
    {
        base.Initialize();

        UIManager.OnScreenChanged += OnScreenChange;

        _configuration.OnValueChanged(CCVars.StaticStorageUI, OnStaticStorageChanged, true);
        _configuration.OnValueChanged(CCVars.OpaqueStorageWindow, OnOpaqueWindowChanged, true);
        _configuration.OnValueChanged(CCVars.StorageWindowTitle, OnStorageWindowTitle, true);
    }

    private void OnScreenChange((UIScreen? Old, UIScreen? New) obj)
    {
        // Handle reconnects with hotbargui.

        // Essentially HotbarGui / the screen gets loaded AFTER gamestates at the moment (because clientgameticker manually changes it via event)
        // and changing this may be a massive change.
        // So instead we'll just manually reload it for now.
        if (!StaticStorageUIEnabled ||
            obj.New == null ||
            !EntityManager.TryGetComponent(_player.LocalEntity, out UserInterfaceUserComponent? userComp))
        {
            return;
        }

        // UISystemDependency not injected at this point so do it the old fashion way, I love ordering issues.
        var uiSystem = EntityManager.System<SharedUserInterfaceSystem>();

        foreach (var bui in uiSystem.GetActorUis((_player.LocalEntity.Value, userComp)))
        {
            if (!uiSystem.TryGetOpenUi<StorageBoundUserInterface>(bui.Entity, StorageComponent.StorageUiKey.Key, out var storageBui))
                continue;

            storageBui.ReOpen();
        }
    }

    private void OnStorageWindowTitle(bool obj)
    {
        WindowTitle = obj;
    }

    private void OnOpaqueWindowChanged(bool obj)
    {
        OpaqueStorageWindow = obj;
    }

    private void OnStaticStorageChanged(bool obj)
    {
        StaticStorageUIEnabled = obj;
    }

    public StorageWindow CreateStorageWindow(EntityUid uid)
    {
        var window = new StorageWindow();
        window.MouseFilter = Control.MouseFilterMode.Pass;

        window.OnPiecePressed += (args, piece) =>
        {
            OnPiecePressed(args, window, piece);
        };
        window.OnPieceUnpressed += (args, piece) =>
        {
            OnPieceUnpressed(args, window, piece);
        };

        if (StaticStorageUIEnabled)
        {
            UIManager.GetActiveUIWidgetOrNull<HotbarGui>()?.StorageContainer.AddChild(window);
        }
        else
        {
            window.OpenCenteredLeft();
        }

        return window;
    }

    public void OnSystemLoaded(StorageSystem system)
    {
        _input.FirstChanceOnKeyEvent += OnMiddleMouse;
    }

    public void OnSystemUnloaded(StorageSystem system)
    {
        _input.FirstChanceOnKeyEvent -= OnMiddleMouse;
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

        if (!IsDragging && EntityManager.System<HandsSystem>().GetActiveHandEntity() == null)
            return;

        //clamp it to a cardinal.
        DraggingRotation = (DraggingRotation + Math.PI / 2f).GetCardinalDir().ToAngle();
        if (DraggingGhost != null)
            DraggingGhost.Location.Rotation = DraggingRotation;

        if (IsDragging || UIManager.CurrentlyHovered is StorageWindow)
            keyEvent.Handle();
    }

    private void OnPiecePressed(GUIBoundKeyEventArgs args, StorageWindow window, ItemGridPiece control)
    {
        if (IsDragging || !window.IsOpen)
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
            if (window.StorageEntity is not {} storage)
                return;

            EntityManager.RaisePredictiveEvent(new StorageSaveItemLocationEvent(
                EntityManager.GetNetEntity(control.Entity),
                EntityManager.GetNetEntity(storage)));
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            EntityManager.System<ExamineSystem>().DoExamine(control.Entity);
            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            UIManager.GetUIController<VerbMenuUIController>().OpenVerbMenu(control.Entity);
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            EntityManager.RaisePredictiveEvent(
                new InteractInventorySlotEvent(EntityManager.GetNetEntity(control.Entity), altInteract: false));
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            EntityManager.RaisePredictiveEvent(new InteractInventorySlotEvent(EntityManager.GetNetEntity(control.Entity), altInteract: true));
            args.Handle();
        }

        window.FlagDirty();
    }

    private void OnPieceUnpressed(GUIBoundKeyEventArgs args, StorageWindow window, ItemGridPiece control)
    {
        if (args.Function != ContentKeyFunctions.MoveStoredItem)
            return;

        // Want to get the control under the dragged control.
        // This means we can drag the original control around (and not hide the original).
        control.MouseFilter = Control.MouseFilterMode.Ignore;
        var targetControl = UIManager.MouseGetControl(args.PointerLocation);
        var targetStorage = targetControl as StorageWindow;
        control.MouseFilter = Control.MouseFilterMode.Pass;

        var localPlayer = _player.LocalEntity;
        window.RemoveGrid(control);
        window.FlagDirty();

        // If we tried to drag it on top of another grid piece then cancel out.
        if (targetControl is ItemGridPiece || window.StorageEntity is not { } sourceStorage || localPlayer == null)
        {
            window.Reclaim(control.Location, control);
            args.Handle();
            _menuDragHelper.EndDrag();
            return;
        }

        if (_menuDragHelper.IsDragging && DraggingGhost is { } draggingGhost)
        {
            var dragEnt = draggingGhost.Entity;
            var dragLoc = draggingGhost.Location;

            // Dragging in the same storage
            // The existing ItemGridPiece just stops rendering but still exists so check if it's hovered.
            if (targetStorage == window)
            {
                var position = targetStorage.GetMouseGridPieceLocation(dragEnt, dragLoc);
                var newLocation = new ItemStorageLocation(DraggingRotation, position);

                EntityManager.RaisePredictiveEvent(new StorageSetItemLocationEvent(
                    EntityManager.GetNetEntity(draggingGhost.Entity),
                    EntityManager.GetNetEntity(sourceStorage),
                    newLocation));

                window.Reclaim(newLocation, control);
            }
            // Dragging to new storage
            else if (targetStorage?.StorageEntity != null && targetStorage != window)
            {
                var position = targetStorage.GetMouseGridPieceLocation(dragEnt, dragLoc);
                var newLocation = new ItemStorageLocation(DraggingRotation, position);

                // Check it fits and we can move to hand (no free transfers).
                if (_storage.ItemFitsInGridLocation(
                        (dragEnt, null),
                        (targetStorage.StorageEntity.Value, null),
                        newLocation))
                {
                    // Can drop and move.
                    EntityManager.RaisePredictiveEvent(new StorageTransferItemEvent(
                        EntityManager.GetNetEntity(dragEnt),
                        EntityManager.GetNetEntity(targetStorage.StorageEntity.Value),
                        newLocation));

                    targetStorage.Reclaim(newLocation, control);
                    DraggingRotation = Angle.Zero;
                }
                else
                {
                    // Cancel it (rather than dropping).
                    window.Reclaim(dragLoc, control);
                }
            }

            targetStorage?.FlagDirty();
        }
        // If we just clicked, then take it out of the bag.
        else
        {
            EntityManager.RaisePredictiveEvent(new StorageInteractWithItemEvent(
                EntityManager.GetNetEntity(control.Entity),
                EntityManager.GetNetEntity(sourceStorage)));
        }

        _menuDragHelper.EndDrag();
        args.Handle();
    }

    private bool OnMenuBeginDrag()
    {
        if (_menuDragHelper.Dragged is not { } dragged)
            return false;

        DraggingGhost!.Orphan();
        DraggingRotation = dragged.Location.Rotation;

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
            EntityManager);

        // I don't know why it divides the position by 2. Hope this helps! -emo
        LayoutContainer.SetPosition(DraggingGhost, UIManager.MousePositionScaled.Position / 2 - offset );
    }

    private void OnMenuEndDrag()
    {
        if (DraggingGhost == null)
            return;

        DraggingRotation = Angle.Zero;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _menuDragHelper.Update(args.DeltaSeconds);
    }
}
