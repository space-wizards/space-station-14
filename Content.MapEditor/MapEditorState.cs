using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.MapEditor.Commands;
using Content.MapEditor.Systems;
using Content.MapEditor.Tools;
using Content.MapEditor.UI;
using Robust.Client;
using Robust.Client.GameObjects;
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
using Content.Client.Power.Visualizers;
using Content.Shared.SubFloor;
using Content.Shared.Wires;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;

    private ISawmill _sawmill = default!;

    // Headless server process for proper entity system support (NodeGroupSystem, etc.)
    private Process? _serverProcess;
    private const int ServerPort = 1212;
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

    // Hover highlight overlay
    private EditorOverlay _editorOverlay = default!;

    // Keyboard shortcut edge detection (tracks previous frame state to detect press edges)
    private bool _wasBDown;
    private bool _wasEDown;
    private bool _wasIDown;
    private bool _wasFDown;
    private bool _wasRDown;
    private bool _wasLDown;
    private bool _wasCDown;
    private bool _wasSDown;
    private bool _wasXDown;
    private bool _wasVDown;
    private bool _wasZDown;
    private bool _wasYDown;
    private bool _wasDeleteDown;
    private bool _wasGDown;
    private bool _wasQDown;

    // Entity outline shader for selection highlight.
    private ShaderInstance? _selectionOutlineShader;
    private EntityUid? _outlinedEntity;

    public MapEditorState()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Startup()
    {
        _sawmill = _logManager.GetSawmill("map_editor");
        _sawmill.Info("MapEditorState started");

        // Launch headless server in background. Connection happens later when user
        // loads a map (the server handles map loading for proper cable/pipe visualization).
        LaunchServer();

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

        // Wire entity palette events.
        _screen.OnEntityPrototypeSelected += OnEntityPrototypeSelected;

        // Wire view toggle events.
        _screen.ViewShowEntitiesButton.OnPressed += OnToggleShowEntities;
        _screen.ViewShowSubfloorButton.OnPressed += OnToggleShowSubfloor;

        // Wire entity info panel button events.
        _screen.OnEntityRotateCW += OnEntityInfoRotateCW;
        _screen.OnEntityRotateCCW += OnEntityInfoRotateCCW;
        _screen.OnEntityDelete += OnEntityInfoDelete;
        _screen.OnEntityDeselect += OnEntityInfoDeselect;

        // Populate the tile palette.
        _screen.PopulateTilePalette(_tileDefs);

        // Populate the entity palette.
        _screen.PopulateEntityPalette(_prototypeManager);

        // Show subfloor entities by default (pipes, cables visible through floors).
        // We toggle visibility directly on sprites rather than using SubFloorHideSystem.ShowAll
        // because ShowAll tries to send network events and interact with sandbox UI.

        // Register hover highlight overlay.
        _editorOverlay = new EditorOverlay();
        IoCManager.Resolve<IOverlayManager>().AddOverlay(_editorOverlay);

        // Prepare the selection outline shader (uses the game's existing outline shader).
        // Set fullbright since the editor has DrawLight=false — otherwise the outline is invisible.
        _selectionOutlineShader = _prototypeManager.Index<ShaderPrototype>("SelectionOutlineInrange").InstanceUnique();
        _selectionOutlineShader.SetParameter("outline_fullbright", true);
        _selectionOutlineShader.SetParameter("outline_width", 4.0f);
        _selectionOutlineShader.SetParameter("outline_color", new Robust.Shared.Maths.Color(0.1f, 1.0f, 0.3f, 0.8f));

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
        _screen.OnEntityPrototypeSelected -= OnEntityPrototypeSelected;
        _screen.OnEntityRotateCW -= OnEntityInfoRotateCW;
        _screen.OnEntityRotateCCW -= OnEntityInfoRotateCCW;
        _screen.OnEntityDelete -= OnEntityInfoDelete;
        _screen.OnEntityDeselect -= OnEntityInfoDeselect;
        _screen.ViewShowEntitiesButton.OnPressed -= OnToggleShowEntities;
        _screen.ViewShowSubfloorButton.OnPressed -= OnToggleShowSubfloor;

        // Remove outline from any selected entity.
        if (_outlinedEntity != null
            && _entityManager.EntityExists(_outlinedEntity.Value)
            && _entityManager.TryGetComponent<SpriteComponent>(_outlinedEntity.Value, out var outlinedSprite))
        {
            outlinedSprite.PostShader = null;
            outlinedSprite.RenderOrder = 0;
        }
        _outlinedEntity = null;

        IoCManager.Resolve<IOverlayManager>().RemoveOverlay(_editorOverlay);

        _uiManager.UnloadScreen();

        // Kill the headless server process.
        KillServer();

        _sawmill.Info("MapEditorState shutdown");
    }

    #region Server Process

    private void LaunchServer()
    {
        // Find the Content.Server executable relative to our binary.
        var editorDir = AppDomain.CurrentDomain.BaseDirectory;
        var serverExe = Path.Combine(editorDir, "..", "Content.Server", "Content.Server.exe");
        serverExe = Path.GetFullPath(serverExe);

        if (!File.Exists(serverExe))
        {
            _sawmill.Error($"Content.Server.exe not found at {serverExe}");
            return;
        }

        _sawmill.Info($"Launching headless server: {serverExe}");

        _serverProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = serverExe,
                Arguments = $"--cvar net.port={ServerPort} --cvar net.bindto=127.0.0.1",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
            EnableRaisingEvents = true,
        };

        try
        {
            _serverProcess.Start();
            _sawmill.Info($"Server process started (PID {_serverProcess.Id})");

            // Give the server a moment to start listening.
            // A proper implementation would poll the port, but a simple delay works for now.
            System.Threading.Thread.Sleep(3000);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to start server: {ex.Message}");
            _serverProcess = null;
        }
    }

    private void KillServer()
    {
        if (_serverProcess == null)
            return;

        try
        {
            if (!_serverProcess.HasExited)
            {
                _sawmill.Info($"Killing server process (PID {_serverProcess.Id})");
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(3000);
            }
        }
        catch (Exception ex)
        {
            _sawmill.Warning($"Error killing server: {ex.Message}");
        }
        finally
        {
            _serverProcess.Dispose();
            _serverProcess = null;
        }
    }

    #endregion

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
        UpdateHoverHighlight();
        UpdateShapePreview();
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
        // If EntitySelectTool is active and has a selection, scroll cycles through entities
        // at the selected tile instead of zooming (matching ss14editor behavior).
        if (_activeTool is EntitySelectTool entitySelect && entitySelect.SelectedEntity != null)
        {
            var screenPos = _input.MouseScreenPosition;
            if (TryResolveGridTile(screenPos, out var tilePos))
            {
                if (entitySelect.OnScroll(_toolContext, tilePos, delta))
                    return; // Consumed — don't zoom.
            }
        }

        // Default: zoom in/out.
        var factor = delta > 0 ? 0.8f : 1.25f;
        var zoom = _eye.Zoom;
        var newZoom = Math.Clamp(zoom.X * factor, MinZoom, MaxZoom);
        _eye.Zoom = new Vector2(newZoom, newZoom);
    }

    private void OnResetZoomPressed()
    {
        _eye.Zoom = Vector2.One;
    }

    private void UpdateHoverHighlight()
    {
        var screenPos = _input.MouseScreenPosition;

        if (_activeGridUid == EntityUid.Invalid || !IsMouseOverViewport(screenPos))
        {
            _editorOverlay.HoveredTile = null;
            return;
        }

        var mapCoords = _eyeManager.PixelToMap(screenPos.Position);
        if (mapCoords.MapId == MapId.Nullspace)
        {
            _editorOverlay.HoveredTile = null;
            return;
        }

        var gridComp = _entityManager.GetComponent<MapGridComponent>(_activeGridUid);
        var tilePos = _toolContext.MapSystem.CoordinatesToTile(_activeGridUid, gridComp, mapCoords);
        _editorOverlay.HoveredTile = tilePos;

        // Set grid world transform so the highlight renders at the correct position.
        var xformSystem = _entityManager.System<SharedTransformSystem>();
        _editorOverlay.GridWorldMatrix = xformSystem.GetWorldMatrix(_activeGridUid);
    }

    /// <summary>
    ///     Computes preview tiles for shape tools during a drag and sends them to the overlay.
    ///     Also computes dimension labels for display in the status bar.
    /// </summary>
    private void UpdateShapePreview()
    {
        List<Vector2i>? preview = null;
        string dimensionLabel = "";

        if (_isToolActive)
        {
            preview = _activeTool switch
            {
                // SelectTool uses SelectionBox overlay instead of individual preview tiles.
                RectangleTool rect when rect.DragStart != null && rect.DragEnd != null
                    => ComputeRectanglePreview(rect.DragStart.Value, rect.DragEnd.Value),
                LineTool line when line.DragStart != null && line.DragEnd != null
                    => ComputeLinePreview(line.DragStart.Value, line.DragEnd.Value),
                CircleTool circle when circle.DragStart != null && circle.DragEnd != null
                    => ComputeCirclePreview(circle.DragStart.Value, circle.DragEnd.Value),
                _ => null,
            };

            // Compute dimension labels for shape tools during drag.
            dimensionLabel = _activeTool switch
            {
                RectangleTool rect when rect.DragStart != null && rect.DragEnd != null
                    => ComputeRectDimLabel(rect.DragStart.Value, rect.DragEnd.Value),
                LineTool line when line.DragStart != null && line.DragEnd != null
                    => ComputeLineDimLabel(line.DragStart.Value, line.DragEnd.Value),
                CircleTool circle when circle.DragStart != null && circle.DragEnd != null
                    => ComputeCircleDimLabel(circle.DragStart.Value, circle.DragEnd.Value),
                SelectTool sel when sel.DragStart != null && sel.DragEnd != null
                    => ComputeRectDimLabel(sel.DragStart.Value, sel.DragEnd.Value),
                _ => "",
            };
        }

        _screen.SetStatusDimension(dimensionLabel);
        _editorOverlay.PreviewTiles = preview;

        // Update preview colors to match the active tool's highlight color.
        if (preview != null)
        {
            var fill = _editorOverlay.HighlightColor;
            _editorOverlay.PreviewFillColor = new Color(fill.R, fill.G, fill.B, 0.2f);
            _editorOverlay.PreviewBorderColor = new Color(fill.R, fill.G, fill.B, 0.5f);
        }

        // Update entity selection outline shader (PostShader on the entity's SpriteComponent).
        EntityUid? currentSelection = null;
        if (_activeTool is EntitySelectTool entitySelect)
            currentSelection = entitySelect.SelectedEntity;

        if (currentSelection != _outlinedEntity)
        {
            // Remove outline from previously selected entity.
            if (_outlinedEntity != null
                && _entityManager.EntityExists(_outlinedEntity.Value)
                && _entityManager.TryGetComponent<SpriteComponent>(_outlinedEntity.Value, out var oldSprite))
            {
                oldSprite.PostShader = null;
                oldSprite.RenderOrder = 0;
            }

            // Apply outline to newly selected entity.
            if (currentSelection != null
                && _entityManager.EntityExists(currentSelection.Value)
                && _entityManager.TryGetComponent<SpriteComponent>(currentSelection.Value, out var newSprite))
            {
                newSprite.PostShader = _selectionOutlineShader;
                newSprite.RenderOrder = unchecked((uint)Environment.TickCount);
            }

            _outlinedEntity = currentSelection;
        }

        // Update the selection box for the SelectTool — both during drag and after.
        if (_activeTool is SelectTool selectTool)
        {
            if (selectTool.DragStart != null && selectTool.DragEnd != null)
            {
                // Show the in-progress selection rectangle during drag.
                var s = selectTool.DragStart.Value;
                var e = selectTool.DragEnd.Value;
                var minX = Math.Min(s.X, e.X);
                var minY = Math.Min(s.Y, e.Y);
                var maxX = Math.Max(s.X, e.X);
                var maxY = Math.Max(s.Y, e.Y);
                _editorOverlay.SelectionBox = new Box2i(minX, minY, maxX + 1, maxY + 1);
                _editorOverlay.IsDraggingSelection = true;
            }
            else if (selectTool.Selection != null)
            {
                _editorOverlay.SelectionBox = selectTool.Selection;
                _editorOverlay.IsDraggingSelection = selectTool.IsMoving;
            }
            else
            {
                _editorOverlay.SelectionBox = null;
                _editorOverlay.IsDraggingSelection = false;
            }

            // Ghost tiles during move.
            _editorOverlay.MoveGhostTiles = selectTool.MoveGhostTiles;
            _editorOverlay.MoveGhostOffset = selectTool.MoveOffset;
        }
        else
        {
            _editorOverlay.SelectionBox = null;
            _editorOverlay.IsDraggingSelection = false;
            _editorOverlay.MoveGhostTiles = null;
        }
    }

    private static List<Vector2i> ComputeRectanglePreview(Vector2i start, Vector2i end)
    {
        var minX = Math.Min(start.X, end.X);
        var maxX = Math.Max(start.X, end.X);
        var minY = Math.Min(start.Y, end.Y);
        var maxY = Math.Max(start.Y, end.Y);

        var tiles = new List<Vector2i>((maxX - minX + 1) * (maxY - minY + 1));
        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                tiles.Add(new Vector2i(x, y));
            }
        }
        return tiles;
    }

    private static List<Vector2i> ComputeLinePreview(Vector2i start, Vector2i end)
    {
        var tiles = new List<Vector2i>();
        foreach (var pos in LineTool.GetLinePoints(start, end))
        {
            tiles.Add(pos);
        }
        return tiles;
    }

    private static List<Vector2i> ComputeCirclePreview(Vector2i center, Vector2i end)
    {
        var dx = end.X - center.X;
        var dy = end.Y - center.Y;
        var radiusSq = dx * dx + dy * dy;
        var radius = (int) Math.Ceiling(Math.Sqrt(radiusSq));

        var tiles = new List<Vector2i>();
        for (var x = center.X - radius; x <= center.X + radius; x++)
        {
            for (var y = center.Y - radius; y <= center.Y + radius; y++)
            {
                var distSq = (x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y);
                if (distSq <= radiusSq)
                    tiles.Add(new Vector2i(x, y));
            }
        }
        return tiles;
    }

    private static string ComputeRectDimLabel(Vector2i start, Vector2i end)
    {
        var w = Math.Abs(end.X - start.X) + 1;
        var h = Math.Abs(end.Y - start.Y) + 1;
        return $"{w}x{h}";
    }

    private static string ComputeLineDimLabel(Vector2i start, Vector2i end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = (int) Math.Ceiling(Math.Sqrt(dx * dx + dy * dy)) + 1;
        return $"L:{length}";
    }

    private static string ComputeCircleDimLabel(Vector2i center, Vector2i end)
    {
        var dx = end.X - center.X;
        var dy = end.Y - center.Y;
        var radius = (int) Math.Ceiling(Math.Sqrt(dx * dx + dy * dy));
        return $"R:{radius}";
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

        // --- EntitySelectTool operations (Delete, R/Shift+R for rotate) ---
        if (_activeTool is EntitySelectTool entitySelectTool)
        {
            var deleteDown = _input.IsKeyDown(Keyboard.Key.Delete);
            if (deleteDown && !_wasDeleteDown)
                entitySelectTool.DeleteSelected(_toolContext);

            if (!ctrl)
            {
                var rDown = _input.IsKeyDown(Keyboard.Key.R);
                if (rDown && !_wasRDown)
                {
                    if (_input.IsKeyDown(Keyboard.Key.Shift))
                        entitySelectTool.RotateSelectedCCW(_toolContext);
                    else
                        entitySelectTool.RotateSelectedCW(_toolContext);
                }
            }
        }

        // --- SelectTool operations (Ctrl+C, Ctrl+X, Ctrl+V, Delete) ---
        if (_activeTool is SelectTool selectTool)
        {
            var deleteDown = _input.IsKeyDown(Keyboard.Key.Delete);
            if (deleteDown && !_wasDeleteDown)
                selectTool.DeleteSelection(_toolContext);

            if (ctrl)
            {
                var cDown = _input.IsKeyDown(Keyboard.Key.C);
                if (cDown && !_wasCDown)
                    selectTool.CopySelection(_toolContext);

                var xDown = _input.IsKeyDown(Keyboard.Key.X);
                // Reuse _wasEDown isn't right — need dedicated tracking; but X has no prior tracker.
                // We'll check edge via a simple approach: X maps to no prior tool shortcut, safe to use fresh.
                if (xDown && !_wasXDown)
                    selectTool.CutSelection(_toolContext);

                var vDown = _input.IsKeyDown(Keyboard.Key.V);
                if (vDown && !_wasVDown)
                {
                    var screenPos = _input.MouseScreenPosition;
                    if (TryResolveGridTile(screenPos, out var pastePos))
                        selectTool.PasteClipboard(_toolContext, pastePos);
                }
            }
        }

        // --- Tool shortcuts (only without modifiers, and not when entity select tool
        //     is active since R is used for rotation there) ---
        if (!ctrl && _activeTool is not EntitySelectTool)
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

            var fDown = _input.IsKeyDown(Keyboard.Key.F);
            if (fDown && !_wasFDown)
                OnToolSelected("fill");

            var rDown = _input.IsKeyDown(Keyboard.Key.R);
            if (rDown && !_wasRDown)
                OnToolSelected("rectangle");

            var lDown = _input.IsKeyDown(Keyboard.Key.L);
            if (lDown && !_wasLDown)
                OnToolSelected("line");

            var cDown = _input.IsKeyDown(Keyboard.Key.C);
            if (cDown && !_wasCDown)
                OnToolSelected("circle");

            var sDown = _input.IsKeyDown(Keyboard.Key.S);
            if (sDown && !_wasSDown)
                OnToolSelected("select");

            var gDown = _input.IsKeyDown(Keyboard.Key.G);
            if (gDown && !_wasGDown)
                OnToolSelected("entityplace");

            var qDown = _input.IsKeyDown(Keyboard.Key.Q);
            if (qDown && !_wasQDown)
                OnToolSelected("entityselect");
        }

        UpdatePreviousKeyState();
    }

    private void UpdatePreviousKeyState()
    {
        _wasBDown = _input.IsKeyDown(Keyboard.Key.B);
        _wasEDown = _input.IsKeyDown(Keyboard.Key.E);
        _wasIDown = _input.IsKeyDown(Keyboard.Key.I);
        _wasFDown = _input.IsKeyDown(Keyboard.Key.F);
        _wasRDown = _input.IsKeyDown(Keyboard.Key.R);
        _wasLDown = _input.IsKeyDown(Keyboard.Key.L);
        _wasCDown = _input.IsKeyDown(Keyboard.Key.C);
        _wasSDown = _input.IsKeyDown(Keyboard.Key.S);
        _wasXDown = _input.IsKeyDown(Keyboard.Key.X);
        _wasVDown = _input.IsKeyDown(Keyboard.Key.V);
        _wasZDown = _input.IsKeyDown(Keyboard.Key.Z);
        _wasYDown = _input.IsKeyDown(Keyboard.Key.Y);
        _wasDeleteDown = _input.IsKeyDown(Keyboard.Key.Delete);
        _wasGDown = _input.IsKeyDown(Keyboard.Key.G);
        _wasQDown = _input.IsKeyDown(Keyboard.Key.Q);
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

        // Update hover highlight color per tool.
        UpdateHighlightColorForTool(toolKey);
    }

    private void UpdateHighlightColorForTool(string toolKey)
    {
        switch (toolKey)
        {
            case "erase":
                _editorOverlay.HighlightColor = new Color(1.0f, 0.3f, 0.3f, 0.3f);
                _editorOverlay.BorderColor = new Color(1.0f, 0.3f, 0.3f, 0.7f);
                break;
            case "eyedropper":
                _editorOverlay.HighlightColor = new Color(0.3f, 1.0f, 0.4f, 0.3f);
                _editorOverlay.BorderColor = new Color(0.3f, 1.0f, 0.4f, 0.7f);
                break;
            case "fill":
                _editorOverlay.HighlightColor = new Color(1.0f, 1.0f, 0.2f, 0.3f);
                _editorOverlay.BorderColor = new Color(1.0f, 1.0f, 0.2f, 0.7f);
                break;
            case "rectangle":
                _editorOverlay.HighlightColor = new Color(0.2f, 1.0f, 1.0f, 0.3f);
                _editorOverlay.BorderColor = new Color(0.2f, 1.0f, 1.0f, 0.7f);
                break;
            case "line":
                _editorOverlay.HighlightColor = new Color(1.0f, 0.6f, 0.2f, 0.3f);
                _editorOverlay.BorderColor = new Color(1.0f, 0.6f, 0.2f, 0.7f);
                break;
            case "circle":
                _editorOverlay.HighlightColor = new Color(1.0f, 0.3f, 1.0f, 0.3f);
                _editorOverlay.BorderColor = new Color(1.0f, 0.3f, 1.0f, 0.7f);
                break;
            case "select":
                _editorOverlay.HighlightColor = new Color(1.0f, 1.0f, 1.0f, 0.15f);
                _editorOverlay.BorderColor = new Color(1.0f, 1.0f, 1.0f, 0.8f);
                break;
            case "entityplace":
                _editorOverlay.HighlightColor = new Color(0.4f, 1.0f, 0.6f, 0.3f);
                _editorOverlay.BorderColor = new Color(0.4f, 1.0f, 0.6f, 0.7f);
                break;
            case "entityselect":
                _editorOverlay.HighlightColor = new Color(0.3f, 0.8f, 1.0f, 0.25f);
                _editorOverlay.BorderColor = new Color(0.3f, 0.8f, 1.0f, 0.7f);
                break;
            default: // paint
                _editorOverlay.HighlightColor = new Color(0.3f, 0.6f, 1.0f, 0.3f);
                _editorOverlay.BorderColor = new Color(0.3f, 0.6f, 1.0f, 0.7f);
                break;
        }
    }

    private void OnToolSelected(string toolKey)
    {
        IEditorTool tool = toolKey switch
        {
            "paint" => new PaintTool(),
            "erase" => new EraseTool(),
            "eyedropper" => new EyedropperTool(),
            "fill" => new FillTool(),
            "rectangle" => new RectangleTool(),
            "line" => new LineTool(),
            "circle" => new CircleTool(),
            "select" => new SelectTool(),
            "entityplace" => new EntityPlaceTool(),
            "entityselect" => new EntitySelectTool(),
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

    private void OnEntityPrototypeSelected(string protoId)
    {
        _toolContext.SelectedEntityPrototype = protoId;

        // Auto-switch to entity place tool when an entity is selected.
        if (_activeToolKey != "entityplace")
        {
            SetActiveTool(new EntityPlaceTool(), "entityplace");
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

    /// <summary>
    ///     Shows a popup listing all entities at a tile so the user can pick one.
    /// </summary>
    private void ShowEntityStackPicker(EntitySelectTool tool, List<EntityUid> entities, ScreenCoordinates mousePos)
    {
        var items = new List<(EntityUid Uid, string Label, Robust.Client.Graphics.Texture? Icon)>();

        foreach (var uid in entities)
        {
            if (!_entityManager.EntityExists(uid))
                continue;

            var meta = _entityManager.GetComponent<MetaDataComponent>(uid);
            var protoId = meta.EntityPrototype?.ID ?? "unknown";
            var label = $"{protoId} [uid={uid}]";

            // Try to get a sprite icon for the entity.
            Robust.Client.Graphics.Texture? icon = null;
            if (_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            {
                try
                {
                    icon = sprite.Icon?.Default;
                }
                catch
                {
                    // Icon access can fail — fall back to no icon.
                }
            }

            items.Add((uid, label, icon));
        }

        if (items.Count == 0)
        {
            tool.CancelPick();
            return;
        }

        _screen.ShowEntityPicker(
            items,
            mousePos.Position,
            selectedUid =>
            {
                tool.ConfirmPick(selectedUid);
            },
            () =>
            {
                tool.CancelPick();
            });
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
                PauseMaps = false, // Load unpaused so entity systems (node groups, etc.) run
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

            // Initialize the map to run entity startup events (node groups, icon smoothing, etc.).
            // Without this, cables/pipes render as dots because NodeGroupSystem never builds
            // connection data on paused entities. After init, re-pause to stop game logic.
            try
            {
                _mapManager.DoMapInitialize(_loadedMapId);
                _mapManager.SetMapPaused(_loadedMapId, true);
            }
            catch (Exception initEx)
            {
                _sawmill.Warning($"Map init/pause error (non-fatal): {initEx.Message}");
            }

            // Move the eye to the loaded map and center on the first grid.
            CenterOnMap(map.Value, grids);

            // Populate grid tabs and set active grid to the first one.
            PopulateGridTabs(_loadedMapId);

            // Compute cable connection masks so cables render properly.
            // Normally done by server-side NodeGroupSystem, but we do it client-side
            // by checking cardinal neighbors for matching cable types.
            ComputeCableConnections();

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

        // Show entity info when EntitySelectTool has a selection.
        if (_activeTool is EntitySelectTool { SelectedEntity: { } selectedUid } entitySel &&
            _entityManager.EntityExists(selectedUid))
        {
            var meta = _entityManager.GetComponent<MetaDataComponent>(selectedUid);
            var xform = _entityManager.GetComponent<TransformComponent>(selectedUid);
            var entPos = xform.Coordinates.Position;
            var protoId = meta.EntityPrototype?.ID ?? "unknown";
            var cycleInfo = entitySel.CycleCount > 1
                ? $" [{entitySel.CyclePosition}/{entitySel.CycleCount} — scroll to cycle]"
                : "";
            _screen.SetStatusInfo($"Entity: {protoId} @ ({entPos.X:F1}, {entPos.Y:F1}){cycleInfo}");

            // Update the entity info panel.
            string? displayName = null;
            try { displayName = meta.EntityName; } catch { /* localization may fail */ }
            var rotDeg = (float)(xform.LocalRotation.Degrees);
            _screen.UpdateEntityInfoPanel(protoId, displayName, entPos, rotDeg);
        }
        else
        {
            _screen.HideEntityInfoPanel();
        }
    }

    private void OnEntityInfoRotateCW()
    {
        if (_activeTool is EntitySelectTool entitySelect)
            entitySelect.RotateSelectedCW(_toolContext);
    }

    private void OnEntityInfoRotateCCW()
    {
        if (_activeTool is EntitySelectTool entitySelect)
            entitySelect.RotateSelectedCCW(_toolContext);
    }

    private void OnEntityInfoDelete()
    {
        if (_activeTool is EntitySelectTool entitySelect)
            entitySelect.DeleteSelected(_toolContext);
    }

    private void OnEntityInfoDeselect()
    {
        if (_activeTool is EntitySelectTool entitySelect)
            entitySelect.Deselect();
    }

    #endregion

    #region View Toggles

    private void OnToggleShowEntities()
    {
        var show = _screen.ShowEntities;
        var query = _entityManager.AllEntityQueryEnumerator<SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var sprite, out var xform))
        {
            if (xform.MapID != _loadedMapId)
                continue;
            // Skip grid and map entities — we only toggle placed entities.
            if (_entityManager.HasComponent<MapGridComponent>(uid) || _entityManager.HasComponent<MapComponent>(uid))
                continue;

            sprite.Visible = show;
        }
    }

    private void OnToggleShowSubfloor()
    {
        ApplySubfloorVisibility(_screen.ShowSubfloor);
    }

    /// <summary>
    ///     Controls subfloor entity visibility using the engine's SubFloorHideSystem.ShowAll.
    ///     Wrapped in try-catch because the setter tries to send network events and update
    ///     sandbox UI, which may fail in the editor's standalone environment.
    /// </summary>
    private void ApplySubfloorVisibility(bool showAll)
    {
        // Set the ShowAll flag. The setter tries to send a network event (which fails
        // in standalone editor mode) but the _showAll field still gets set correctly.
        try
        {
            var subFloorSystem = _entityManager.System<Content.Client.SubFloor.SubFloorHideSystem>();
            subFloorSystem.ShowAll = showAll;
        }
        catch (Exception ex)
        {
            _sawmill.Warning($"SubFloorHideSystem.ShowAll setter error (expected): {ex.Message}");
        }

        // The setter's UpdateAll() normally runs via a network round-trip that doesn't
        // exist in the editor. Manually queue appearance updates for all subfloor entities
        // so the visibility changes actually take effect.
        var appearanceSystem = _entityManager.System<AppearanceSystem>();
        var query = _entityManager.AllEntityQueryEnumerator<SubFloorHideComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var appearance))
        {
            appearanceSystem.QueueUpdate(uid, appearance);
        }
    }

    #endregion

    #region Cable Connection Fix

    /// <summary>
    ///     Computes cable connection masks client-side after map load.
    ///     Normally done by server-side NodeGroupSystem, but since we're client-only,
    ///     we check cardinal neighbors for matching cable types and set the appearance data.
    /// </summary>
    private void ComputeCableConnections()
    {
        var appearanceSys = _entityManager.System<AppearanceSystem>();

        // Build a lookup: tile position → list of (EntityUid, StatePrefix) for cables on that tile.
        var cableTiles = new Dictionary<(EntityUid Grid, Vector2i Tile), List<(EntityUid Uid, string Prefix)>>();

        var query = _entityManager.AllEntityQueryEnumerator<CableVisualizerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var cableVis, out var xform))
        {
            if (xform.MapID != _loadedMapId)
                continue;

            var gridUid = xform.GridUid;
            if (gridUid == null || !_entityManager.TryGetComponent<MapGridComponent>(gridUid.Value, out var grid))
                continue;

            var prefix = cableVis.StatePrefix ?? "cable";
            var tile = _entityManager.System<SharedMapSystem>().CoordinatesToTile(gridUid.Value, grid, xform.Coordinates);
            var key = (gridUid.Value, tile);

            if (!cableTiles.TryGetValue(key, out var list))
            {
                list = new List<(EntityUid, string)>();
                cableTiles[key] = list;
            }
            list.Add((uid, prefix));
        }

        // For each cable, check cardinal neighbors for same-prefix cables.
        var directions = new (Vector2i Offset, WireVisDirFlags Flag)[]
        {
            (new Vector2i(0, 1), WireVisDirFlags.North),
            (new Vector2i(0, -1), WireVisDirFlags.South),
            (new Vector2i(1, 0), WireVisDirFlags.East),
            (new Vector2i(-1, 0), WireVisDirFlags.West),
        };

        foreach (var ((gridUid, tile), cables) in cableTiles)
        {
            foreach (var (uid, prefix) in cables)
            {
                var mask = WireVisDirFlags.None;

                foreach (var (offset, flag) in directions)
                {
                    var neighborKey = (gridUid, tile + offset);
                    if (cableTiles.TryGetValue(neighborKey, out var neighbors))
                    {
                        foreach (var (_, neighborPrefix) in neighbors)
                        {
                            if (neighborPrefix == prefix)
                            {
                                mask |= flag;
                                break;
                            }
                        }
                    }
                }

                // Set the appearance data so the CableVisualizerSystem picks it up.
                if (_entityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
                {
                    appearanceSys.SetData(uid, WireVisVisuals.ConnectedMask, mask, appearance);
                }
            }
        }

        _sawmill.Info($"Computed cable connections for {cableTiles.Count} tile positions");
    }

    #endregion
}
