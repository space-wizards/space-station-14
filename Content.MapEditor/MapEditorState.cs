using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.MapEditor.Commands;
using Content.MapEditor.Tools;
using Content.MapEditor.UI;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.MapEditor;

public sealed class MapEditorState : State
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IFileDialogManager _fileDialog = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;

    private ISawmill _sawmill = default!;
    private MapEditorScreen _screen = default!;
    private Eye _eye = default!;

    // Camera pan state
    private bool _isPanning;
    private ScreenCoordinates _lastMouseScreen;

    // Zoom limits
    private const float MinZoom = 0.05f;
    private const float MaxZoom = 10f;

    // Loaded map tracking
    private string? _loadedFileName;
    private MapId _loadedMapId;

    // Active grid — all tool operations target this grid.
    private EntityUid _activeGridUid;

    // Tool system
    private readonly CommandStack _commandStack = new();
    private ToolContext _toolContext = default!;
    private IEditorTool _activeTool = new PaintTool();
    private string _activeToolKey = "paint";
    private bool _isToolActive; // true while left mouse is held and tool is in a stroke
    private bool _wasLeftDown;
    private Vector2i _lastToolTilePos;

    // Keyboard shortcut edge detection (tracks previous frame state to detect press edges)
    private bool _wasBDown;
    private bool _wasEDown;
    private bool _wasIDown;
    private bool _wasZDown;
    private bool _wasYDown;

    public MapEditorState()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Startup()
    {
        _sawmill = _logManager.GetSawmill("map_editor");
        _sawmill.Info("MapEditorState started");

        _uiManager.LoadScreen<MapEditorScreen>();
        _screen = (MapEditorScreen) _uiManager.ActiveScreen!;

        // Create a dedicated eye for the editor and point the viewport at it.
        _eye = new Eye
        {
            Zoom = Vector2.One,
            DrawFov = false,
            DrawLight = false,
        };
        _eyeManager.CurrentEye = _eye;
        _eyeManager.MainViewport = _screen.MainViewport.Viewport;

        // Initialize tool context.
        _toolContext = new ToolContext
        {
            EntityManager = _entityManager,
            MapSystem = _entityManager.System<SharedMapSystem>(),
            CommandStack = _commandStack,
        };

        // Wire menu button events.
        _screen.FileOpenButton.OnPressed += OnFileOpenPressed;
        _screen.FileSaveButton.OnPressed += OnFileSavePressed;
        _screen.FileExitButton.OnPressed += OnFileExitPressed;
        _screen.EditUndoButton.OnPressed += OnUndoPressed;
        _screen.EditRedoButton.OnPressed += OnRedoPressed;
        _screen.ViewResetZoomButton.OnPressed += OnResetZoomPressed;

        // Wire scroll-wheel zoom from the viewport overlay.
        _screen.OnViewportScroll += OnViewportScroll;

        // Wire toolbar and palette events.
        _screen.OnToolSelected += OnToolSelected;
        _screen.OnTileSelected += OnTileSelected;

        // Wire grid tab events.
        _screen.OnGridTabSelected += OnGridTabSelected;
        _screen.OnAddGridPressed += OnAddGridPressed;

        // Populate the tile palette.
        _screen.PopulateTilePalette(_tileDefs);

        // Set initial toolbar state.
        _screen.SetActiveToolButton(_activeToolKey);
    }

    protected override void Shutdown()
    {
        _screen.FileOpenButton.OnPressed -= OnFileOpenPressed;
        _screen.FileSaveButton.OnPressed -= OnFileSavePressed;
        _screen.FileExitButton.OnPressed -= OnFileExitPressed;
        _screen.EditUndoButton.OnPressed -= OnUndoPressed;
        _screen.EditRedoButton.OnPressed -= OnRedoPressed;
        _screen.ViewResetZoomButton.OnPressed -= OnResetZoomPressed;
        _screen.OnViewportScroll -= OnViewportScroll;
        _screen.OnToolSelected -= OnToolSelected;
        _screen.OnTileSelected -= OnTileSelected;
        _screen.OnGridTabSelected -= OnGridTabSelected;
        _screen.OnAddGridPressed -= OnAddGridPressed;

        _uiManager.UnloadScreen();
        _sawmill.Info("MapEditorState shutdown");
    }

    #region Active Grid

    /// <summary>
    ///     Sets the active grid and updates the tool context.
    /// </summary>
    private void SetActiveGrid(EntityUid gridUid)
    {
        _activeGridUid = gridUid;
        _toolContext.ActiveGridUid = gridUid;
        _sawmill.Debug($"Active grid set to {gridUid}");
    }

    /// <summary>
    ///     Enumerates all grids on the given map and populates the grid tab bar.
    ///     Sets the active grid to the first one found.
    /// </summary>
    private void PopulateGridTabs(MapId mapId)
    {
        var grids = new List<(EntityUid Uid, string Label)>();
        var query = _entityManager.AllEntityQueryEnumerator<MapGridComponent, TransformComponent>();
        var index = 0;

        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            grids.Add((uid, $"Grid {index}"));
            index++;
        }

        _screen.PopulateGridTabs(grids);

        if (grids.Count > 0)
        {
            SetActiveGrid(grids[0].Uid);
            _screen.SetActiveGridTab(grids[0].Uid);
        }
        else
        {
            SetActiveGrid(EntityUid.Invalid);
        }
    }

    private void OnGridTabSelected(EntityUid gridUid)
    {
        SetActiveGrid(gridUid);
        _screen.SetActiveGridTab(gridUid);
    }

    private void OnAddGridPressed()
    {
        if (_loadedMapId == default)
            return;

        var newGrid = _mapManager.CreateGridEntity(_loadedMapId);
        var uid = newGrid.Owner;

        // Count existing tabs for label.
        var tabCount = _screen.GridTabCount;
        _screen.AddGridTab(uid, $"Grid {tabCount}");

        SetActiveGrid(uid);
        _screen.SetActiveGridTab(uid);

        _sawmill.Info($"Created new grid {uid} on map {_loadedMapId}");
        _screen.SetStatusInfo($"Created new grid");
    }

    #endregion

    #region Camera

    public override void FrameUpdate(FrameEventArgs e)
    {
        UpdatePan();
        UpdateKeyboardShortcuts();
        UpdateToolInput();
        UpdateStatusBar();
    }

    private void UpdatePan()
    {
        var mouseDown = _input.IsKeyDown(Keyboard.Key.MouseRight)
                     || _input.IsKeyDown(Keyboard.Key.MouseMiddle);
        var currentPos = _input.MouseScreenPosition;

        if (mouseDown)
        {
            if (_isPanning)
            {
                // Compute screen-space pixel delta.
                var dx = currentPos.Position.X - _lastMouseScreen.Position.X;
                var dy = currentPos.Position.Y - _lastMouseScreen.Position.Y;

                if (dx != 0 || dy != 0)
                {
                    // Convert pixel delta to world delta.
                    // Zoom = pixels per world unit conceptually.
                    // Higher zoom = more zoomed in = smaller world delta for the same pixel delta.
                    var zoom = _eye.Zoom;
                    // Screen pixels to world units. In RT, smaller zoom = more zoomed in,
                    // so multiply by zoom to get consistent drag speed at all zoom levels.
                    var ppm = EyeManager.PixelsPerMeter;
                    var worldDx = -dx * zoom.X / ppm;
                    var worldDy = dy * zoom.Y / ppm;

                    var pos = _eye.Position;
                    _eye.Position = new MapCoordinates(
                        new Vector2(pos.Position.X + worldDx, pos.Position.Y + worldDy),
                        pos.MapId);
                }
            }
            else
            {
                _isPanning = true;
            }

            _lastMouseScreen = currentPos;
        }
        else
        {
            _isPanning = false;
        }
    }

    private void OnViewportScroll(float delta)
    {
        // delta > 0 = scroll up = zoom in
        var factor = delta > 0 ? 0.8f : 1.25f;
        var zoom = _eye.Zoom;
        var newZoom = Math.Clamp(zoom.X * factor, MinZoom, MaxZoom);
        _eye.Zoom = new Vector2(newZoom, newZoom);
    }

    private void OnResetZoomPressed()
    {
        _eye.Zoom = Vector2.One;
    }

    #endregion

    #region Keyboard Shortcuts

    /// <summary>
    ///     Polls key states each frame and fires actions on press edges.
    ///     B = Paint, E = Erase, I = Eyedropper, Ctrl+Z = Undo, Ctrl+Y / Ctrl+Shift+Z = Redo.
    /// </summary>
    private void UpdateKeyboardShortcuts()
    {
        // Don't process shortcuts while a tool stroke is in progress.
        if (_isToolActive)
        {
            UpdatePreviousKeyState();
            return;
        }

        var ctrl = _input.IsKeyDown(Keyboard.Key.Control);

        // --- Undo / Redo ---
        var zDown = _input.IsKeyDown(Keyboard.Key.Z);
        if (zDown && !_wasZDown && ctrl)
        {
            if (_input.IsKeyDown(Keyboard.Key.Shift))
                _commandStack.Redo();
            else
                _commandStack.Undo();
        }

        var yDown = _input.IsKeyDown(Keyboard.Key.Y);
        if (yDown && !_wasYDown && ctrl)
        {
            _commandStack.Redo();
        }

        // --- Tool shortcuts (only without modifiers) ---
        if (!ctrl)
        {
            var bDown = _input.IsKeyDown(Keyboard.Key.B);
            if (bDown && !_wasBDown)
                OnToolSelected("paint");

            var eDown = _input.IsKeyDown(Keyboard.Key.E);
            if (eDown && !_wasEDown)
                OnToolSelected("erase");

            var iDown = _input.IsKeyDown(Keyboard.Key.I);
            if (iDown && !_wasIDown)
                OnToolSelected("eyedropper");
        }

        UpdatePreviousKeyState();
    }

    private void UpdatePreviousKeyState()
    {
        _wasBDown = _input.IsKeyDown(Keyboard.Key.B);
        _wasEDown = _input.IsKeyDown(Keyboard.Key.E);
        _wasIDown = _input.IsKeyDown(Keyboard.Key.I);
        _wasZDown = _input.IsKeyDown(Keyboard.Key.Z);
        _wasYDown = _input.IsKeyDown(Keyboard.Key.Y);
    }

    #endregion

    #region Tool Dispatch

    /// <summary>
    ///     Sets the active editor tool (Paint, Erase, Eyedropper, etc.).
    /// </summary>
    public void SetActiveTool(IEditorTool tool, string toolKey)
    {
        // End any in-progress stroke before switching.
        if (_isToolActive)
        {
            _activeTool.OnMouseUp(_toolContext);
            _isToolActive = false;
        }

        _activeTool = tool;
        _activeToolKey = toolKey;
        _screen.SetActiveToolButton(toolKey);
    }

    private void OnToolSelected(string toolKey)
    {
        IEditorTool tool = toolKey switch
        {
            "paint" => new PaintTool(),
            "erase" => new EraseTool(),
            "eyedropper" => new EyedropperTool(),
            _ => new PaintTool(),
        };

        SetActiveTool(tool, toolKey);
    }

    private void OnTileSelected(int tileId)
    {
        _toolContext.SelectedTile = new Tile(tileId);

        // If user selects a tile, auto-switch to paint tool for convenience.
        if (_activeToolKey != "paint")
        {
            SetActiveTool(new PaintTool(), "paint");
        }
    }

    /// <summary>
    ///     Polls left mouse button each frame to dispatch tool start/drag/end.
    /// </summary>
    private void UpdateToolInput()
    {
        var leftDown = _input.IsKeyDown(Keyboard.Key.MouseLeft);
        var screenPos = _input.MouseScreenPosition;

        if (leftDown && !_wasLeftDown)
        {
            // Only start a tool stroke if the click is on the viewport, not on UI panels.
            if (!_isPanning && IsMouseOverViewport(screenPos) && TryResolveGridTile(screenPos, out var tilePos))
            {
                _isToolActive = true;
                _lastToolTilePos = tilePos;
                _activeTool.OnMouseDown(_toolContext, tilePos);

                if (_activeToolKey == "eyedropper")
                    _screen.SelectTileInPalette(_toolContext.SelectedTile.TypeId);
            }
        }
        else if (leftDown && _isToolActive)
        {
            // Left mouse held — drag.
            if (TryResolveGridTile(screenPos, out var tilePos))
            {
                if (tilePos != _lastToolTilePos)
                {
                    _lastToolTilePos = tilePos;
                    _activeTool.OnMouseDrag(_toolContext, tilePos);
                }
            }
        }
        else if (!leftDown && _isToolActive)
        {
            // Left mouse released — end stroke.
            _isToolActive = false;
            _activeTool.OnMouseUp(_toolContext);
        }

        _wasLeftDown = leftDown;
    }

    /// <summary>
    ///     Returns true if the mouse position is within the viewport control bounds.
    /// </summary>
    private bool IsMouseOverViewport(ScreenCoordinates screenPos)
    {
        var viewport = _screen.MainViewport;
        var vpRect = viewport.GlobalPixelRect;
        return vpRect.Contains((int) screenPos.Position.X, (int) screenPos.Position.Y);
    }

    /// <summary>
    ///     Converts a screen position to tile coordinates on the active grid.
    ///     Returns false if no active grid is set or the position is in nullspace.
    /// </summary>
    private bool TryResolveGridTile(ScreenCoordinates screenPos, out Vector2i tilePos)
    {
        tilePos = default;

        if (_activeGridUid == EntityUid.Invalid)
            return false;

        var mapCoords = _eyeManager.PixelToMap(screenPos.Position);
        if (mapCoords.MapId == MapId.Nullspace)
            return false;

        var gridComp = _entityManager.GetComponent<MapGridComponent>(_activeGridUid);
        var mapSystem = _toolContext.MapSystem;
        tilePos = mapSystem.CoordinatesToTile(_activeGridUid, gridComp, mapCoords);
        return true;
    }

    private void OnUndoPressed()
    {
        _commandStack.Undo();
    }

    private void OnRedoPressed()
    {
        _commandStack.Redo();
    }

    #endregion

    #region File Open / Save

    private void OnFileOpenPressed()
    {
        OpenMapAsync();
    }

    private void OnFileSavePressed()
    {
        SaveMapAsync();
    }

    private void OnFileExitPressed()
    {
        IoCManager.Resolve<Robust.Client.IGameController>().Shutdown("Editor closed");
    }

    private async void OpenMapAsync()
    {
        try
        {
            var filters = new FileDialogFilters(new FileDialogFilters.Group("yml", "yaml"));
            var stream = await _fileDialog.OpenFile(filters, FileAccess.Read, FileShare.Read);
            if (stream == null)
                return;

            using var reader = new StreamReader(stream);
            var source = "opened map";

            var mapLoader = _entityManager.System<MapLoaderSystem>();

            // Use TryLoadMap which creates a new map entity automatically.
            var options = new DeserializationOptions
            {
                InitializeMaps = true,
            };

            if (!mapLoader.TryLoadMap(reader, source, out var map, out var grids, options))
            {
                _sawmill.Error("Failed to load map file.");
                _screen.SetStatusInfo("Failed to load map");
                return;
            }

            // Successfully loaded — record the map.
            _loadedMapId = map.Value.Comp.MapId;
            _loadedFileName = "loaded map";
            _screen.SetStatusInfo($"Loaded map ({grids!.Count} grid(s))");

            // Move the eye to the loaded map and center on the first grid.
            CenterOnMap(map.Value, grids);

            // Populate grid tabs and set active grid to the first one.
            PopulateGridTabs(_loadedMapId);

            _sawmill.Info($"Map loaded: {grids.Count} grids on map {_loadedMapId}");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Error opening map: {ex}");
            _screen.SetStatusInfo("Error loading map");
        }
    }

    private void CenterOnMap(Entity<MapComponent> map, System.Collections.Generic.HashSet<Entity<MapGridComponent>> grids)
    {
        // Find bounding box center of the first grid.
        if (grids.Count == 0)
        {
            _eye.Position = new MapCoordinates(Vector2.Zero, map.Comp.MapId);
            return;
        }

        var firstGrid = grids.First();
        var aabb = firstGrid.Comp.LocalAABB;
        var center = aabb.Center;

        // Get the grid's world position.
        var xform = _entityManager.GetComponent<TransformComponent>(firstGrid.Owner);
        var worldPos = xform.LocalPosition + center;

        _eye.Position = new MapCoordinates(worldPos, map.Comp.MapId);

        // Set a reasonable zoom level based on grid size.
        var maxDim = Math.Max(aabb.Width, aabb.Height);
        if (maxDim > 0)
        {
            // Try to fit the grid into roughly 80% of the viewport.
            // Viewport is ~21 tiles wide, so zoom to fit.
            var fitZoom = 21f / maxDim * 0.8f;
            fitZoom = Math.Clamp(fitZoom, MinZoom, 2f);
            _eye.Zoom = new Vector2(fitZoom, fitZoom);
        }
    }

    private async void SaveMapAsync()
    {
        try
        {
            // Determine which map to save based on the current eye position.
            var mapId = _eye.Position.MapId;
            var mapUid = _mapManager.GetMapEntityId(mapId);

            if (mapUid == EntityUid.Invalid)
            {
                _sawmill.Warning("No map to save (eye is in null space).");
                _screen.SetStatusInfo("No map to save");
                return;
            }

            var filters = new FileDialogFilters(new FileDialogFilters.Group("yml", "yaml"));
            var result = await _fileDialog.SaveFile(filters);
            if (result == null)
                return;

            var (stream, _) = result.Value;

            await using (stream)
            {
                using var writer = new StreamWriter(stream);

                var mapLoader = _entityManager.System<MapLoaderSystem>();
                if (!mapLoader.TrySaveMap(mapUid, writer))
                {
                    _sawmill.Error("Failed to save map.");
                    _screen.SetStatusInfo("Failed to save map");
                    return;
                }
            }

            _screen.SetStatusInfo("Map saved");
            _sawmill.Info($"Map {mapId} saved.");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Error saving map: {ex}");
            _screen.SetStatusInfo("Error saving map");
        }
    }

    #endregion

    #region Status Bar

    private void UpdateStatusBar()
    {
        var pos = _eye.Position.Position;
        _screen.SetStatusCoords($"({pos.X:F1}, {pos.Y:F1})");

        var zoom = _eye.Zoom.X;
        _screen.SetStatusZoom($"Zoom: {zoom:F2}x");

        _screen.SetStatusTool(_activeTool.Name);
    }

    #endregion
}
