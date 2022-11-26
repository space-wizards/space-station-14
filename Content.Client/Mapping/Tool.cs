using Content.Client.Mapping.Snapping;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Sandboxing;
using Robust.Shared.Utility;

namespace Content.Client.Mapping;

/// <summary>
/// A "tool" used within the map editor.
/// This is allowed to be stateful.
/// </summary>
public abstract class Tool
{
    /// <summary>
    /// Used in the mapping activity overlay to indicate the active tool.
    /// </summary>
    public abstract SpriteSpecifier ToolActivityIcon { get; }

    /// <summary>
    /// The type of the UI element used to configure this tool.
    /// This should be a UIWidget, as the config is not passed to the tool directly during validation.
    /// </summary>
    public abstract Type ToolConfigurationControl { get; }

    public virtual void Startup()
    {

    }

    public virtual void Draw(in OverlayDrawArgs args)
    {

    }

    public virtual void FrameUpdate(float delta)
    {

    }

    public virtual void Shutdown()
    {

    }
}

public interface IDrawingLikeToolConfiguration
{
    /// <summary>
    /// The angle in degrees.
    /// </summary>
    /// <remarks>Degrees was used to avoid tiny error buildups over time with radians.</remarks>
    public float Rotation { get; set; }
    public float RotationAdjust { get; set; }
    public string Prototype { get; }

    public SnappingModeImpl? SnappingMode { get; }

    protected Dictionary<string, SnappingModeImpl?> Modes { get;}

    public void SetupModes(List<string> modePrototypes)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        Modes.Add("Freehand", null);
        foreach (var modeTy in modePrototypes)
        {
            var proto = protoMan.Index<SnappingModePrototype>(modeTy);
            Modes.Add(proto.Name, proto.Config.Clone());
        }
    }
}

/// <summary>
/// For tools that function an awful lot like drawing something.
/// </summary>
public abstract class DrawingLikeTool : Tool
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IInputManager _input = default!;

    protected EntityCoordinates? InitialClickPoint = null;

    /// <summary>
    /// Checks if the input point is a valid new location to draw at in point-placement.
    /// This is used to make placement function like a freehand draw tool when not in line or rect mode.
    /// You may wish to defer to the active snapping mode.
    /// </summary>
    /// <param name="new">The point to validate.</param>
    /// <returns>Success.</returns>
    protected abstract bool ValidateNewInitialPoint(EntityCoordinates @new);

    private enum Mode
    {
        Point,
        Line,
        Rect,
    }

    /// <summary>
    /// Validates whether or not the user can draw at the given coordinates.
    /// </summary>
    /// <param name="point">The coordinates to attempt at.</param>
    /// <returns>Success.</returns>
    public abstract bool ValidateDrawPoint(EntityCoordinates point);
    /// <summary>
    /// Validates whether or not the user can draw at the given coordinates.
    /// </summary>
    /// <param name="start">The start coordinates to attempt at.</param>
    /// <param name="end">The end coordinates to attempt at.</param>
    /// <returns>Success.</returns>
    public abstract bool ValidateDrawLine(EntityCoordinates start, EntityCoordinates end);
    /// <summary>
    /// Validates whether or not the user can draw at the given coordinates.
    /// </summary>
    /// <param name="start">The start coordinates to attempt at.</param>
    /// <param name="end">The end coordinates to attempt at.</param>
    /// <returns>Success.</returns>
    public abstract bool ValidateDrawRect(EntityCoordinates start, EntityCoordinates end);

    public abstract void PreviewDrawPoint(EntityCoordinates point, in OverlayDrawArgs args);
    public abstract void PreviewDrawLine(EntityCoordinates start, EntityCoordinates end, in OverlayDrawArgs args);
    public abstract void PreviewDrawRect(EntityCoordinates start, EntityCoordinates end, in OverlayDrawArgs args);

    public abstract bool DoDrawPoint(EntityCoordinates point);
    public abstract bool DoDrawLine(EntityCoordinates start, EntityCoordinates end);
    public abstract bool DoDrawRect(EntityCoordinates start, EntityCoordinates end);

    private IDrawingLikeToolConfiguration GetConfig()
    {
        return (IDrawingLikeToolConfiguration) _userInterface.ActiveScreen![ToolConfigurationControl]!;
    }

    /// <summary>
    /// Called when the rotate keybind is hit, handle as you will.
    /// Recommended to have a "by how much" option in the tool config panel.
    /// </summary>
    public virtual void Rotate()
    {
        var config = GetConfig();
        config.Rotation = (config.Rotation + config.RotationAdjust) % 360;
    }

    public DrawingLikeTool()
    {
        IoCManager.InjectDependencies(this);

        if (!typeof(IDrawingLikeToolConfiguration).IsAssignableFrom(ToolConfigurationControl))
            throw new Exception($"The tool of type {this.GetType().FullName} has an invalid configuration control");
    }

    public override void Startup()
    {
        base.Startup();
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EditorPlaceObject, new PointerStateInputCmdHandler(PlaceKeyDown, PlaceKeyUp))
            .Bind(EngineKeyFunctions.EditorLinePlace, new PointerInputCmdHandler(LinePlaceTriggered))
            .Bind(EngineKeyFunctions.EditorGridPlace, new PointerInputCmdHandler(GridPlaceTriggered))
            .Bind(EngineKeyFunctions.EditorRotateObject, new PointerInputCmdHandler(RotateTriggered))
            .Bind(EngineKeyFunctions.EditorCancelPlace, new PointerInputCmdHandler(CancelPlaceTriggered))
            .Register<DrawingLikeTool>();
    }

    // i know this sounds like it's drawing objects but no this is for rendering on the screen and i'm just following naming convention
    public override void Draw(in OverlayDrawArgs args)
    {
        var mapCoords = _eye.ScreenToMap(_input.MouseScreenPosition);
        if (mapCoords == default)
        {
            return; // Left viewport.
        }

        var coords = Reanchor(mapCoords);

        if (!_activeDrawing)
        {
            PreviewDrawPoint(coords, args);
            return;
        }

        switch (_mode)
        {
            case Mode.Point:
            {
                PreviewDrawPoint(coords, args);
                break;
            }
            case Mode.Line:
            {
                PreviewDrawLine(InitialClickPoint!.Value, coords, args);
                break;
            }
            case Mode.Rect:
            {
                PreviewDrawRect(InitialClickPoint!.Value, coords, args);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(_mode), $"Did you add a new mode? Forgot to account for it in {nameof(Draw)}");
        }
    }

    public virtual EntityCoordinates Reanchor(EntityCoordinates coords, bool snap = false)
    {
        if (false)
        {
            // TODO: blah blah attach to anchor entity instead of grid.
        }

        var mapCoords = coords.ToMap(_entity);
        var newCoords = coords;
        if (_map.TryFindGridAt(mapCoords, out var grid))
        {
            newCoords = EntityCoordinates.FromMap(grid.Owner, mapCoords);
        }

        var cfg = GetConfig();
        if (cfg.SnappingMode is { } mode)
            return mode.Snap(newCoords);

        return newCoords;
    }

    public virtual EntityCoordinates Reanchor(MapCoordinates mapCoords, bool snap = false)
    {
        if (false)
        {
            // TODO: blah blah attach to anchor entity instead of grid.
        }

        EntityCoordinates entCoords;

        if (_map.TryFindGridAt(mapCoords, out var grid))
        {
            entCoords = EntityCoordinates.FromMap(grid.Owner, mapCoords);
        }
        else
        {
            entCoords = EntityCoordinates.FromMap(_map, mapCoords);
        }

        var cfg = GetConfig();
        if (cfg.SnappingMode is { } mode)
            return mode.Snap(entCoords);

        return entCoords;
    }

    public override void FrameUpdate(float delta)
    {
        base.FrameUpdate(delta);
        if (!_activeDrawing)
            return;

        var mapCoords = _eye.ScreenToMap(_input.MouseScreenPosition);
        if (mapCoords == default)
        {
            QuitDrawing();
            return; // Left viewport.
        }

        var coords = Reanchor(mapCoords);

        if (_mode == Mode.Point)
        {
            // We're scribbling.

            if (!ValidateNewInitialPoint(coords))
                return; // we didn't scribble hard enough :(

            var oldInitial = InitialClickPoint;
            InitialClickPoint = coords;
            if (ValidateDrawPoint(coords))
                DoDrawPoint(coords);
        }
        // if we're not scribbling there's nothing to do.
    }

    public override void Shutdown()
    {
        base.Shutdown();
        // i swear to god if you startup another tool before shutting down the one you already had i will show up on your doorstep
        // --moony
        CommandBinds.Unregister<DrawingLikeTool>();
    }

    private bool RotateTriggered(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        Rotate();
        return true;
    }

    private bool CancelPlaceTriggered(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        QuitDrawing();
        return true;
    }

    private bool LinePlaceTriggered(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (_activeDrawing)
            return false;

        InitialClickPoint = Reanchor(args.Coordinates);
        StartDrawing(Mode.Line);
        return true;
    }

    private bool GridPlaceTriggered(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (_activeDrawing)
            return false;

        InitialClickPoint = Reanchor(args.Coordinates);
        StartDrawing(Mode.Rect);
        return true;
    }

    private bool _activeDrawing = false;
    private Mode _mode = Mode.Point;

    private void StartDrawing(Mode mode)
    {
        _activeDrawing = true;
        _mode = mode;
    }

    private void QuitDrawing()
    {
        _activeDrawing = false;
    }

    private bool PlaceKeyUp(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        switch (_mode)
        {
            case Mode.Point:
            {
                break;
            }
            case Mode.Line:
            {
                if (ValidateDrawLine(InitialClickPoint!.Value, coords))
                    DoDrawLine(InitialClickPoint!.Value, coords);
                break;
            }
            case Mode.Rect:
            {
                if (ValidateDrawRect(InitialClickPoint!.Value, coords))
                    DoDrawRect(InitialClickPoint!.Value, coords);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(_mode), $"Did you add a new mode? Forgot to account for it in {nameof(PlaceKeyUp)}");
        }

        QuitDrawing();
        return true;
    }

    private bool PlaceKeyDown(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        InitialClickPoint = Reanchor(coords);
        StartDrawing(Mode.Point);
        if (ValidateDrawPoint(InitialClickPoint!.Value))
            DoDrawPoint(InitialClickPoint!.Value);
        return true;
    }
}
