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

    // Tool system
    private readonly CommandStack _commandStack = new();
    private ToolContext _toolContext = default!;
    private IEditorTool _activeTool = new PaintTool();
    private bool _isToolActive; // true while left mouse is held and tool is in a stroke
    private Vector2i _lastToolTilePos;
    private EntityUid _lastToolGridUid;

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
        _screen.OnViewportLeftDown += OnViewportLeftDown;
        _screen.OnViewportLeftUp += OnViewportLeftUp;
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
        _screen.OnViewportLeftDown -= OnViewportLeftDown;
        _screen.OnViewportLeftUp -= OnViewportLeftUp;

        _uiManager.UnloadScreen();
        _sawmill.Info("MapEditorState shutdown");
    }

    #region Camera

    public override void FrameUpdate(FrameEventArgs e)
    {
        UpdatePan();
        UpdateToolDrag();
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

    #region Tool Dispatch

    /// <summary>
    ///     Sets the active editor tool (Paint, Erase, Eyedropper, etc.).
    /// </summary>
    public void SetActiveTool(IEditorTool tool)
    {
        // End any in-progress stroke before switching.
        if (_isToolActive)
        {
            _activeTool.OnMouseUp(_toolContext);
            _isToolActive = false;
        }

        _activeTool = tool;
    }

    private void OnViewportLeftDown(ScreenCoordinates screenPos)
    {
        if (_isPanning)
            return;

        if (!TryResolveGridTile(screenPos, out var gridUid, out var tilePos))
            return;

        _isToolActive = true;
        _lastToolTilePos = tilePos;
        _lastToolGridUid = gridUid;
        _activeTool.OnMouseDown(_toolContext, tilePos, gridUid);
    }

    private void OnViewportLeftUp(ScreenCoordinates screenPos)
    {
        if (!_isToolActive)
            return;

        _isToolActive = false;
        _activeTool.OnMouseUp(_toolContext);
    }

    /// <summary>
    ///     Called every frame to dispatch drag events while left mouse is held.
    /// </summary>
    private void UpdateToolDrag()
    {
        if (!_isToolActive)
            return;

        // If left mouse was released without going through our event (e.g. focus lost), clean up.
        if (!_input.IsKeyDown(Keyboard.Key.MouseLeft))
        {
            _isToolActive = false;
            _activeTool.OnMouseUp(_toolContext);
            return;
        }

        var currentScreenPos = _input.MouseScreenPosition;
        if (!TryResolveGridTile(currentScreenPos, out var gridUid, out var tilePos))
            return;

        // Only dispatch drag if the tile position changed.
        if (tilePos == _lastToolTilePos && gridUid == _lastToolGridUid)
            return;

        _lastToolTilePos = tilePos;
        _lastToolGridUid = gridUid;
        _activeTool.OnMouseDrag(_toolContext, tilePos, gridUid);
    }

    /// <summary>
    ///     Converts a screen position to a grid entity + tile position.
    ///     Returns false if no grid is found under the cursor.
    /// </summary>
    private bool TryResolveGridTile(ScreenCoordinates screenPos, out EntityUid gridUid, out Vector2i tilePos)
    {
        gridUid = default;
        tilePos = default;

        var mapCoords = _eyeManager.PixelToMap(screenPos);
        if (mapCoords.MapId == MapId.Nullspace)
            return false;

        var mapSystem = _toolContext.MapSystem;

        // Find grids at the cursor position (point query via tiny AABB).
        var worldPos = mapCoords.Position;
        var pointBox = new Box2(worldPos, worldPos);
        var grids = new List<Entity<MapGridComponent>>();
        _mapManager.FindGridsIntersecting(mapCoords.MapId, pointBox, ref grids);

        if (grids.Count == 0)
            return false;

        // Use the first grid found (most maps have one main grid).
        var grid = grids[0];
        gridUid = grid.Owner;
        tilePos = mapSystem.CoordinatesToTile(gridUid, grid.Comp, mapCoords);
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
