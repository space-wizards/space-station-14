using System.Numerics;
using Content.Client.Tabletop.UI;
using Content.Client.Verbs.UI;
using Content.Client.Viewport;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopSystem : SharedTabletopSystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private EntityUid? _draggedEntity; // Entity being dragged
    private ScalingViewport? _viewport; // Viewport currently being used
    private TabletopWindow? _window; // Current open tabletop window (only allow one at a time)
    private EntityUid? _table; // The table entity of the currently open game session

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, false, true))
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(OnUseSecondary, true, true))
            .Register<TabletopSystem>();
    }

    #region Overrides
    /// <inheritdoc />
    protected override void CopyEntity(EntityUid target, Entity<TabletopGameComponent> ent, EntityUid user)
    {
        if (ent.Comp.Position is not { } position)
            return;

        // Delay count check - prints should happen last.
        if (ent.Comp.Entities.Count >= MaxTabletopPieces)
        {
            Popup.PopupEntity(Loc.GetString("tabletop-cant-add-more"), ent, user);
            return;
        }

        var meta = MetaData(target);

        var hologram = EntityManager.PredictedSpawn(GamePiecePrototype, position.Offset(-1, 0));

        // Make sure the entity can be dragged and can be removed, move it into the board game world and add it to the Entities hashmap.
        EnsureComp<TabletopDraggableComponent>(hologram);
        EnsureComp<TabletopHologramComponent>(hologram);
        Meta.SetEntityName(hologram, Name(target, meta));

        // Try to get existing tabletop visuals if we can (copying existing pieces), otherwise get this entity's prototype of this object.
        if (AppearanceQuery.TryComp(target, out AppearanceComponent? appearance)
            && Appearance.TryGetData<string>(target, TabletopItemVisuals.Prototype, out var appearProto, appearance))
        {
            Appearance.SetData(hologram, TabletopItemVisuals.Prototype, appearProto);
        }
        else if (meta.EntityPrototype is { } metaProto)
        {
            Appearance.SetData(hologram, TabletopItemVisuals.Prototype, metaProto.ID);
        }

        Popup.PopupEntity(Loc.GetString("tabletop-added-piece"), ent, user);
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (!_gameTiming.IsFirstTimePredicted)
            return;
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
        if (_draggedEntity == null || _viewport == null) return;

        if (!CanDrag(playerEntity, _draggedEntity.Value, out var draggableComponent))
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
        var clampedCoords = ClampPositionToViewport(coords, _viewport);
        if (clampedCoords.Equals(MapCoordinates.Nullspace)) return;

        // Only send new position to server when Delay is reached
        if (_table != null)
        {
            RaisePredictiveEvent(new TabletopMoveEvent(GetNetEntity(_draggedEntity.Value), clampedCoords, GetNetEntity(_table.Value)));
        }
    }
    #endregion Overrides

    #region Event handlers
    /// <summary>
    /// Basic left click handler.
    /// </summary>
    private bool OnUse(in PointerInputCmdArgs args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return false;

        return args.State switch
        {
            BoundKeyState.Down => OnMouseDown(args),
            BoundKeyState.Up => OnMouseUp(),
            _ => false
        };
    }

    /// <summary>
    /// Basic right click handler.
    /// </summary>
    private bool OnUseSecondary(in PointerInputCmdArgs args)
    {
        if (_table == null || _draggedEntity != null)
            return false;

        if (args.State == BoundKeyState.Down)
            return OnRightMouseDown(args);

        return false;
    }

    /// <summary>
    /// Left click down: starts a drag.
    /// </summary>
    private bool OnMouseDown(in PointerInputCmdArgs args)
    {
        // Return if no player entity
        if (_playerManager.LocalEntity is not { } playerEntity)
            return false;

        var entity = args.EntityUid;

        // Return if can not see table or stunned/no hands
        if (!CanSeeTable(playerEntity, _table) || !CanDrag(playerEntity, entity, out _))
        {
            return false;
        }

        // Try to get the viewport under the cursor
        if (_uiManager.MouseGetControl(args.ScreenCoordinates) as ScalingViewport is not { } viewport)
        {
            return false;
        }

        StartDragging(entity, viewport);
        return true;
    }

    /// <summary>
    /// Left click up: releases a dragged piece.
    /// </summary>
    private bool OnMouseUp()
    {
        StopDragging();
        return false;
    }

    /// <summary>
    /// Right click down: opens a context menu if not dragging.
    /// </summary>
    private bool OnRightMouseDown(in PointerInputCmdArgs args)
    {
        // Return if no player entity
        if (_playerManager.LocalEntity is not { } playerEntity)
            return false;

        if (_draggedEntity != null)
            return false;

        var entity = args.EntityUid;

        // Return if can not see table or stunned/no hands
        if (!CanSeeTable(playerEntity, _table) || !CanDrag(playerEntity, entity, out _))
        {
            return false;
        }

        // Need to force the verb menu, our piece is in the middle of goddamn nowhere.
        _uiManager.GetUIController<VerbMenuUIController>().OpenVerbMenu(entity, force: true);
        return true;
    }

    [SubscribeLocalEvent]
    private void HandleDraggableRemoved(Entity<TabletopDraggableComponent> ent, ref ComponentRemove args)
    {
        if (_draggedEntity == ent)
            StopDragging(false);
    }

    /// <summary>
    /// Hologram handler: sets up the hologram to mimic another entity's sprite.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnHologramAppearanceChange(Entity<TabletopHologramComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // TODO: maybe this can work more nicely, by maybe only having to set the item to "being dragged", and have
        //  the appearance handle the rest
        if (Appearance.TryGetData<string>(ent, TabletopItemVisuals.Prototype, out var protoId, args.Component)
            && ent.Comp.LastPrototype != protoId)
        {
            ent.Comp.LastPrototype = protoId;

            if (ProtoMan.TryIndex(protoId, out var proto)
                && proto.TryComp<SpriteComponent>(out var protoSprite, Factory))
            {
                // HACK: we don't actually have an entity to pass, but the first parameter here is unused.
                var outSprite = CopyComp(EntityUid.Invalid, ent, protoSprite);
                outSprite.NoRotation = true;
            }

            // Reset our scale/draw depth after copying our new sprite, if the data exists.
            if (Appearance.TryGetData<Vector2>(ent, TabletopItemVisuals.Scale, out var scale, args.Component))
                _sprite.SetScale((ent, args.Sprite), scale);

            if (Appearance.TryGetData<int>(ent, TabletopItemVisuals.DrawDepth, out var drawDepth, args.Component))
                _sprite.SetDrawDepth((ent, args.Sprite), drawDepth);
        }
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<TabletopDraggableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // TODO: maybe this can work more nicely, by maybe only having to set the item to "being dragged", and have
        //  the appearance handle the rest
        if (Appearance.TryGetData<Vector2>(ent, TabletopItemVisuals.Scale, out var scale, args.Component))
            _sprite.SetScale((ent, args.Sprite), scale);

        if (Appearance.TryGetData<int>(ent, TabletopItemVisuals.DrawDepth, out var drawDepth, args.Component))
            _sprite.SetDrawDepth((ent, args.Sprite), drawDepth);
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
        if (broadcast && _draggedEntity != null && HasComp<TabletopDraggableComponent>(_draggedEntity.Value))
        {
            RaisePredictiveEvent(new TabletopMoveEvent(GetNetEntity(_draggedEntity.Value), Xform.GetMapCoordinates(_draggedEntity.Value), GetNetEntity(_table!.Value)));
            RaisePredictiveEvent(new TabletopDraggingPlayerChangedEvent(GetNetEntity(_draggedEntity.Value), false));
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
    private static MapCoordinates ClampPositionToViewport(MapCoordinates coordinates, ScalingViewport viewport)
    {
        if (coordinates == MapCoordinates.Nullspace) return MapCoordinates.Nullspace;

        var eye = viewport.Eye;
        if (eye == null)
            return MapCoordinates.Nullspace;

        var size = (Vector2)viewport.ViewportSize / EyeManager.PixelsPerMeter; // Convert to tiles instead of pixels
        var eyePosition = eye.Position.Position;
        var eyeRotation = eye.Rotation;
        var eyeScale = eye.Scale;

        var min = (eyePosition - size / 2) / eyeScale;
        var max = (eyePosition + size / 2) / eyeScale;

        // If 90/270 degrees rotated, flip X and Y
        if (MathHelper.CloseToPercent(eyeRotation.Degrees % 180d, 90d) || MathHelper.CloseToPercent(eyeRotation.Degrees % 180d, -90d))
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
