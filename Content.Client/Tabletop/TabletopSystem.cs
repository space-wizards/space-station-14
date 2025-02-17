using System.Numerics;
using Content.Client.Tabletop.UI;
using Content.Client.Viewport;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client.Tabletop;

[UsedImplicitly]
public sealed class TabletopSystem : SharedTabletopSystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManger = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    // Time in seconds to wait until sending the location of a dragged entity to the server again
    private const float Delay = 1f / 10; // 10 Hz

    private float _timePassed; // Time passed since last update sent to the server.
    private EntityUid? _draggedEntity; // Entity being dragged
    private ScalingViewport? _viewport; // Viewport currently being used
    private DefaultWindow? _window; // Current open tabletop window (only allow one at a time)
    private EntityUid? _table; // The table entity of the currently open game session

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, false, true))
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(OnUseSecondary, true, true))
            .Register<TabletopSystem>();

        SubscribeNetworkEvent<TabletopPlayEvent>(OnTabletopPlay);
        SubscribeLocalEvent<TabletopDraggableComponent, ComponentRemove>(HandleDraggableRemoved);
    }

    private void HandleDraggableRemoved(Entity<TabletopDraggableComponent> entity, ref ComponentRemove args)
    {
        if (_draggedEntity == entity)
            StopDragging(false);
    }

    public override void FrameUpdate(float frameTime)
    {
        if (_window == null)
            return;

        // If there is no player entity, return
        if (_playerManager.LocalEntity is not { } playerEntity)
            return;

        if (!CanSeeTable(playerEntity, _table))
        {
            StopDragging();
            _window?.Close();
            return;
        }

        // If no entity is being dragged or no viewport is clicked, return
        if (_draggedEntity is not { } draggedEntity || _viewport == null)
            return;

        if (!CanDrag(playerEntity, draggedEntity, out var draggableComponent))
        {
            StopDragging();
            return;
        }

        // If the dragged entity has another dragging player, drop the item
        // This should happen if the local player is dragging an item, and another player grabs it out of their hand
        if (draggableComponent.DraggingPlayer != null &&
            draggableComponent.DraggingPlayer != _playerManager.LocalSession!.UserId)
        {
            StopDragging(false);
            return;
        }

        // Map mouse position to EntityCoordinates
        var coords = _viewport.PixelToMap(_inputManager.MouseScreenPosition.Position);

        // Clamp coordinates to viewport
        if (ClampPositionToViewport(coords, _viewport) is not { } clampedCoords)
            return;

        // Move the entity locally every update
        _transformSystem.SetWorldPosition(draggedEntity, clampedCoords.Position);

        // Increment total time passed
        _timePassed += frameTime;

        // Only send new position to server when Delay is reached
        if (_timePassed >= Delay && _table is { } table)
        {
            RaisePredictiveEvent(new TabletopMoveEvent(GetNetEntity(draggedEntity),
                clampedCoords,
                GetNetEntity(table)));
            _timePassed -= Delay;
        }
    }

    #region Event handlers

    /// <summary>
    /// Runs when the player presses the "Play Game" verb on a tabletop game.
    /// Opens a viewport where they can then play the game.
    /// </summary>
    private void OnTabletopPlay(TabletopPlayEvent msg)
    {
        // Close the currently opened window, if it exists
        _window?.Close();

        _table = GetEntity(msg.TableUid);

        // Get the camera entity that the server has created for us
        var camera = GetEntity(msg.CameraUid);

        if (!TryComp<EyeComponent>(camera, out var eyeComponent))
        {
            // If there is no eye, print error and do not open any window
            Log.Error("Camera entity does not have eye component!");
            return;
        }

        // Create a window to contain the viewport
        _window = new TabletopWindow(eyeComponent.Eye, (msg.Size.X, msg.Size.Y))
        {
            MinWidth = 500,
            MinHeight = 436,
            Title = msg.Title,
        };

        _window.OnClose += OnWindowClose;
    }

    private void OnWindowClose()
    {
        if (_table is { } table)
        {
            RaiseNetworkEvent(new TabletopStopPlayingEvent(GetNetEntity(table)));
        }

        StopDragging();
        _window = null;
    }

    private bool OnUse(in PointerInputCmdArgs args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return false;

        return args.State switch
        {
            BoundKeyState.Down => OnMouseDown(args),
            BoundKeyState.Up => OnMouseUp(),
            _ => false,
        };

        bool OnMouseDown(in PointerInputCmdArgs args)
        {
            // Return if no player entity
            if (_playerManager.LocalEntity is not { } playerEntity)
                return false;

            // Return if can not see table or stunned/no hands
            if (!CanSeeTable(playerEntity, _table) || !CanDrag(playerEntity, args.EntityUid, out _))
                return false;

            // Try to get the viewport under the cursor
            if (_uiManger.MouseGetControl(args.ScreenCoordinates) as ScalingViewport is not { } viewport)
                return false;

            StartDragging(args.EntityUid, viewport);
            return true;
        }

        bool OnMouseUp()
        {
            StopDragging();
            return false;
        }
    }

    private bool OnUseSecondary(in PointerInputCmdArgs args)
    {
        if (_draggedEntity is { } draggedEntity && _table is { } table)
        {
            RaiseNetworkEvent(new TabletopRequestTakeOut
            {
                Entity = GetNetEntity(draggedEntity),
                TableUid = GetNetEntity(table)
            });
        }

        return false;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Start dragging an entity in a specific viewport.
    /// </summary>
    /// <param name="draggedEntity">The entity that we start dragging.</param>
    /// <param name="viewport">The viewport in which we are dragging.</param>
    private void StartDragging(EntityUid draggedEntity, ScalingViewport viewport)
    {
        RaisePredictiveEvent(new TabletopDraggingPlayerChangedEvent(GetNetEntity(draggedEntity), true));

        _draggedEntity = draggedEntity;
        _viewport = viewport;
    }

    /// <summary>
    /// Stop dragging the entity.
    /// </summary>
    /// <param name="broadcast">Whether to tell other clients that we stopped dragging.</param>
    private void StopDragging(bool broadcast = true)
    {
        // Set the dragging player on the component to noone
        if (broadcast && _draggedEntity is { } draggedEntity && HasComp<TabletopDraggableComponent>(draggedEntity))
        {
            RaisePredictiveEvent(new TabletopMoveEvent(GetNetEntity(draggedEntity),
                Transforms.GetMapCoordinates(draggedEntity),
                GetNetEntity(_table!.Value)));
            RaisePredictiveEvent(new TabletopDraggingPlayerChangedEvent(GetNetEntity(draggedEntity), false));
        }

        _draggedEntity = null;
        _viewport = null;
    }

    /// <summary>
    /// Clamps coordinates within a viewport. ONLY WORKS FOR 90 DEGREE ROTATIONS!
    /// </summary>
    /// <param name="coordinates">The coordinates to be clamped.</param>
    /// <param name="viewport">The viewport to clamp the coordinates to.</param>
    /// <returns>Coordinates clamped to the viewport.</returns>
    private static MapCoordinates? ClampPositionToViewport(MapCoordinates coordinates, ScalingViewport viewport)
    {
        if (coordinates == MapCoordinates.Nullspace || viewport.Eye is not { } eye)
            return null;

        var size = (Vector2)viewport.ViewportSize / EyeManager.PixelsPerMeter; // Convert to tiles instead of pixels
        var eyePosition = eye.Position.Position;
        var eyeRotation = eye.Rotation;
        var eyeScale = eye.Scale;

        var min = (eyePosition - size / 2) / eyeScale;
        var max = (eyePosition + size / 2) / eyeScale;

        // If 90/270 degrees rotated, flip X and Y
        if (MathHelper.CloseToPercent(eyeRotation.Degrees % 180d, 90d) ||
            MathHelper.CloseToPercent(eyeRotation.Degrees % 180d, -90d))
        {
            (min.Y, min.X) = (min.X, min.Y);
            (max.Y, max.X) = (max.X, max.Y);
        }

        var clampedPosition = Vector2.Clamp(coordinates.Position, min, max);

        // Use the eye's map ID, we don't want anything moving to a different map!
        return new MapCoordinates(clampedPosition, eye.Position.MapId);
    }

    #endregion
}
