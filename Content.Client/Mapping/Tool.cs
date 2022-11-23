using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Players;
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

    public virtual void Shutdown()
    {

    }
}

public abstract class DrawingLikeTool : Tool
{
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

    /// <summary>
    /// Called when the rotate keybind is hit, handle as you will.
    /// Recommended to have a "by how much" option in the tool config panel.
    /// </summary>
    public virtual void Rotate()
    {
    }


    public override void Startup()
    {
        base.Startup();
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EditorPlaceObject, new PointerStateInputCmdHandler(PlaceKeyDown, PlaceKeyUp))
            .Bind(EngineKeyFunctions.EditorLinePlace, new PointerInputCmdHandler(LinePlaceTriggered))
            .Bind(EngineKeyFunctions.EditorGridPlace, new PointerInputCmdHandler(GridPlaceTriggered))
            .Bind(EngineKeyFunctions.EditorRotateObject, new PointerInputCmdHandler(RotateTriggered))
            .Register<DrawingLikeTool>();
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
        throw new NotImplementedException();
    }

    private bool LinePlaceTriggered(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        throw new NotImplementedException();
    }

    private bool GridPlaceTriggered(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        throw new NotImplementedException();
    }

    private bool PlaceKeyUp(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        throw new NotImplementedException();
    }

    private bool PlaceKeyDown(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        throw new NotImplementedException();
    }
}
