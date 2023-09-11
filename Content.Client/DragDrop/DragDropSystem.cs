using Content.Client.CombatMode;
using Content.Client.Gameplay;
using Content.Client.Outline;
using Content.Shared.ActionBlocker;
using Content.Shared.CCVar;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.DragDrop;

/// <summary>
/// Handles clientside drag and drop logic
/// </summary>
[UsedImplicitly]
public sealed class DragDropSystem : SharedDragDropSystem
{
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfgMan = default!;
    [Dependency] private readonly InteractionOutlineSystem _outline = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly CombatModeSystem _combatMode = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private ISawmill _sawmill = default!;

    // how often to recheck possible targets (prevents calling expensive
    // check logic each update)
    private const float TargetRecheckInterval = 0.25f;

    // if a drag ends up being cancelled and it has been under this
    // amount of time since the mousedown, we will "replay" the original
    // mousedown event so it can be treated like a regular click
    private const float MaxMouseDownTimeForReplayingClick = 0.85f;

    [ValidatePrototypeId<ShaderPrototype>]
    private const string ShaderDropTargetInRange = "SelectionOutlineInrange";

    [ValidatePrototypeId<ShaderPrototype>]
    private const string ShaderDropTargetOutOfRange = "SelectionOutline";

    /// <summary>
    /// Current entity being dragged around.
    /// </summary>
    private EntityUid? _draggedEntity;

    /// <summary>
    /// If an entity is being dragged is there a drag shadow.
    /// </summary>
    private EntityUid? _dragShadow;

    /// <summary>
    /// Time since mouse down over the dragged entity
    /// </summary>
    private float _mouseDownTime;

    /// <summary>
    /// how much time since last recheck of all possible targets
    /// </summary>
    private float _targetRecheckTime;

    /// <summary>
    /// Reserved initial mousedown event so we can replay it if no drag ends up being performed
    /// </summary>
    private PointerInputCmdHandler.PointerInputCmdArgs? _savedMouseDown;

    /// <summary>
    /// Whether we are currently replaying the original mouse down, so we
    /// can ignore any events sent to this system
    /// </summary>
    private bool _isReplaying;

    private float _deadzone;

    private DragState _state = DragState.NotDragging;

    /// <summary>
    /// screen pos where the mouse down began for the drag
    /// </summary>
    private ScreenCoordinates? _mouseDownScreenPos;

    private ShaderInstance? _dropTargetInRangeShader;
    private ShaderInstance? _dropTargetOutOfRangeShader;

    private readonly List<SpriteComponent> _highlightedSprites = new();

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("drag_drop");
        UpdatesOutsidePrediction = true;
        UpdatesAfter.Add(typeof(SharedEyeSystem));

        _cfgMan.OnValueChanged(CCVars.DragDropDeadZone, SetDeadZone, true);

        _dropTargetInRangeShader = _prototypeManager.Index<ShaderPrototype>(ShaderDropTargetInRange).Instance();
        _dropTargetOutOfRangeShader = _prototypeManager.Index<ShaderPrototype>(ShaderDropTargetOutOfRange).Instance();
        // needs to fire on mouseup and mousedown so we can detect a drag / drop
        CommandBinds.Builder
            .BindBefore(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, false, true), new[] { typeof(SharedInteractionSystem) })
            .Register<DragDropSystem>();
    }

    private void SetDeadZone(float deadZone)
    {
        _deadzone = deadZone;
    }

    public override void Shutdown()
    {
        _cfgMan.UnsubValueChanged(CCVars.DragDropDeadZone, SetDeadZone);
        CommandBinds.Unregister<DragDropSystem>();
        base.Shutdown();
    }

    private bool OnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        // not currently predicted
        if (_inputSystem.Predicted)
            return false;

        // currently replaying a saved click, don't handle this because
        // we already decided this click doesn't represent an actual drag attempt
        if (_isReplaying)
            return false;

        if (args.State == BoundKeyState.Down)
        {
            return OnUseMouseDown(args);
        }

        if (args.State == BoundKeyState.Up)
        {
            return OnUseMouseUp(args);
        }

        return false;
    }

    private void EndDrag()
    {
        if (_state == DragState.NotDragging)
            return;

        if (_dragShadow != null)
        {
            Del(_dragShadow.Value);
            _dragShadow = null;
        }

        _draggedEntity = null;
        _state = DragState.NotDragging;
        _mouseDownScreenPos = null;

        RemoveHighlights();
        _outline.SetEnabled(true);
        _mouseDownTime = 0;
        _savedMouseDown = null;
    }

    private bool OnUseMouseDown(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not {Valid: true} dragger ||
            _combatMode.IsInCombatMode())
        {
            return false;
        }

        // cancel any current dragging if there is one (shouldn't be because they would've had to have lifted
        // the mouse, canceling the drag, but just being cautious)
        EndDrag();

        var entity = args.EntityUid;

        // possibly initiating a drag
        // check if the clicked entity is draggable
        if (!Exists(entity))
        {
            return false;
        }

        // check if the entity is reachable
        if (!_interactionSystem.InRangeUnobstructed(dragger, entity))
        {
            return false;
        }

        var ev = new CanDragEvent();

        RaiseLocalEvent(entity, ref ev);

        if (ev.Handled != true)
            return false;

        _draggedEntity = entity;
        _state = DragState.MouseDown;
        _mouseDownScreenPos = _inputManager.MouseScreenPosition;
        _mouseDownTime = 0;

        // don't want anything else to process the click,
        // but we will save the event so we can "re-play" it if this drag does
        // not turn into an actual drag so the click can be handled normally
        _savedMouseDown = args;

        return true;

    }

    private void StartDrag()
    {
        if (!Exists(_draggedEntity))
        {
            // something happened to the clicked entity or we moved the mouse off the target so
            // we shouldn't replay the original click
            return;
        }

        _state = DragState.Dragging;
        DebugTools.Assert(_dragShadow == null);
        _outline.SetEnabled(false);
        HighlightTargets();

        if (TryComp<SpriteComponent>(_draggedEntity, out var draggedSprite))
        {
            // pop up drag shadow under mouse
            var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
            _dragShadow = EntityManager.SpawnEntity("dragshadow", mousePos);
            var dragSprite = Comp<SpriteComponent>(_dragShadow.Value);
            dragSprite.CopyFrom(draggedSprite);
            dragSprite.RenderOrder = EntityManager.CurrentTick.Value;
            dragSprite.Color = dragSprite.Color.WithAlpha(0.7f);
            // keep it on top of everything
            dragSprite.DrawDepth = (int) DrawDepth.Overlays;
            if (!dragSprite.NoRotation)
            {
                Transform(_dragShadow.Value).WorldRotation = Transform(_draggedEntity.Value).WorldRotation;
            }

            // drag initiated
            return;
        }

        _sawmill.Warning($"Unable to display drag shadow for {ToPrettyString(_draggedEntity.Value)} because it has no sprite component.");
    }

    private bool UpdateDrag(float frameTime)
    {
        if (!Exists(_draggedEntity) || _combatMode.IsInCombatMode())
        {
            EndDrag();
            return false;
        }

        var player = _playerManager.LocalPlayer?.ControlledEntity;

        // still in range of the thing we are dragging?
        if (player == null || !_interactionSystem.InRangeUnobstructed(player.Value, _draggedEntity.Value))
        {
            return false;
        }

        if (_dragShadow == null)
            return false;

        _targetRecheckTime += frameTime;

        if (_targetRecheckTime > TargetRecheckInterval)
        {
            HighlightTargets();
            _targetRecheckTime -= TargetRecheckInterval;
        }

        return true;
    }

    private bool OnUseMouseUp(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (_state == DragState.MouseDown)
        {
            // haven't started the drag yet, quick mouseup, definitely treat it as a normal click by
            // replaying the original cmd
            try
            {
                if (_savedMouseDown.HasValue && _mouseDownTime < MaxMouseDownTimeForReplayingClick)
                {
                    var savedValue = _savedMouseDown.Value;
                    _isReplaying = true;
                    // adjust the timing info based on the current tick so it appears as if it happened now
                    var replayMsg = savedValue.OriginalMessage;

                    switch (replayMsg)
                    {
                        case ClientFullInputCmdMessage clientInput:
                            replayMsg = new ClientFullInputCmdMessage(args.OriginalMessage.Tick,
                                args.OriginalMessage.SubTick,
                                replayMsg.InputFunctionId)
                            {
                                State = replayMsg.State,
                                Coordinates = clientInput.Coordinates,
                                ScreenCoordinates = clientInput.ScreenCoordinates,
                                Uid = clientInput.Uid,
                            };
                            break;
                        case FullInputCmdMessage fullInput:
                            replayMsg = new FullInputCmdMessage(args.OriginalMessage.Tick,
                                args.OriginalMessage.SubTick,
                                replayMsg.InputFunctionId, replayMsg.State, fullInput.Coordinates, fullInput.ScreenCoordinates,
                                fullInput.Uid);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (savedValue.Session != null)
                    {
                        _inputSystem.HandleInputCommand(savedValue.Session, EngineKeyFunctions.Use, replayMsg,
                            true);
                    }

                    _isReplaying = false;
                }
            }
            finally
            {
                EndDrag();
            }

            return false;
        }

        var localPlayer = _playerManager.LocalPlayer?.ControlledEntity;

        if (localPlayer == null || !Exists(_draggedEntity))
        {
            EndDrag();
            return false;
        }

        IEnumerable<EntityUid> entities;
        var coords = args.Coordinates;

        if (_stateManager.CurrentState is GameplayState screen)
        {
            entities = screen.GetClickableEntities(coords);
        }
        else
        {
            entities = Array.Empty<EntityUid>();
        }

        var outOfRange = false;
        var user = localPlayer.Value;

        foreach (var entity in entities)
        {
            if (entity == _draggedEntity)
                continue;

            // check if it's able to be dropped on by current dragged entity
            var valid = ValidDragDrop(user, _draggedEntity.Value, entity);

            if (valid != true) continue;

            if (!_interactionSystem.InRangeUnobstructed(user, entity)
                || !_interactionSystem.InRangeUnobstructed(user, _draggedEntity.Value))
            {
                outOfRange = true;
                continue;
            }

            // tell the server about the drop attempt
            RaiseNetworkEvent(new DragDropRequestEvent(GetNetEntity(_draggedEntity.Value), GetNetEntity(entity)));
            EndDrag();
            return true;
        }

        if (outOfRange)
        {
            _popup.PopupEntity(Loc.GetString("drag-drop-system-out-of-range-text"), _draggedEntity.Value, Filter.Local(), true);
        }

        EndDrag();
        return false;
    }

    // TODO make this just use TargetOutlineSystem
    private void HighlightTargets()
    {
        if (!Exists(_draggedEntity) ||
            !Exists(_dragShadow))
        {
            return;
        }

        var user = _playerManager.LocalPlayer?.ControlledEntity;

        if (user == null)
            return;

        // highlights the possible targets which are visible
        // and able to be dropped on by the current dragged entity

        // remove current highlights
        RemoveHighlights();

        // find possible targets on screen even if not reachable
        // TODO: Duplicated in SpriteSystem and TargetOutlineSystem. Should probably be cached somewhere for a frame?
        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        var expansion = new Vector2(1.5f, 1.5f);

        var bounds = new Box2(mousePos.Position - expansion, mousePos.Position + expansion);
        var pvsEntities = _lookup.GetEntitiesIntersecting(mousePos.MapId, bounds);

        var spriteQuery = GetEntityQuery<SpriteComponent>();

        foreach (var entity in pvsEntities)
        {
            if (!spriteQuery.TryGetComponent(entity, out var inRangeSprite) ||
                !inRangeSprite.Visible ||
                entity == _draggedEntity)
            {
                continue;
            }

            var valid = ValidDragDrop(user.Value, _draggedEntity.Value, entity);

            // check if it's able to be dropped on by current dragged entity
            if (valid == null)
                continue;

            // We'll do a final check given server-side does this before any dragdrop can take place.
            if (valid.Value)
            {
                valid = _interactionSystem.InRangeUnobstructed(user.Value, _draggedEntity.Value)
                        && _interactionSystem.InRangeUnobstructed(user.Value, entity);
            }

            if (inRangeSprite.PostShader != null &&
                inRangeSprite.PostShader != _dropTargetInRangeShader &&
                inRangeSprite.PostShader != _dropTargetOutOfRangeShader)
            {
                continue;
            }

            // highlight depending on whether its in or out of range
            inRangeSprite.PostShader = valid.Value ? _dropTargetInRangeShader : _dropTargetOutOfRangeShader;
            inRangeSprite.RenderOrder = EntityManager.CurrentTick.Value;
            _highlightedSprites.Add(inRangeSprite);
        }
    }

    private void RemoveHighlights()
    {
        foreach (var highlightedSprite in _highlightedSprites)
        {
            if (highlightedSprite.PostShader != _dropTargetInRangeShader && highlightedSprite.PostShader != _dropTargetOutOfRangeShader)
                continue;

            highlightedSprite.PostShader = null;
            highlightedSprite.RenderOrder = 0;
        }

        _highlightedSprites.Clear();
    }

    /// <summary>
    ///     Are these args valid for drag-drop?
    /// </summary>
    /// <returns>
    /// Returns null if no interactions are available or the user / target cannot interact with each other.
    /// Returns false if interactions exist but are not available currently.
    /// </returns>
    private bool? ValidDragDrop(EntityUid user, EntityUid dragged, EntityUid target)
    {
        if (!_actionBlockerSystem.CanInteract(user, target))
            return null;

        // CanInteract() doesn't support checking a second "target" entity.
        // Doing so manually:
        var ev = new GettingInteractedWithAttemptEvent(user, dragged);
        RaiseLocalEvent(dragged, ev, true);

        if (ev.Cancelled)
            return false;

        var dropEv = new CanDropDraggedEvent(user, target);

        RaiseLocalEvent(dragged, ref dropEv);

        if (dropEv.Handled)
        {
            if (!dropEv.CanDrop)
                return false;
        }

        var dropEv2 = new CanDropTargetEvent(user, dragged);

        RaiseLocalEvent(target, ref dropEv2);

        if (dropEv2.Handled)
            return dropEv2.CanDrop;

        return null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        switch (_state)
        {
            // check if dragging should begin
            case DragState.MouseDown:
            {
                var screenPos = _inputManager.MouseScreenPosition;
                if ((_mouseDownScreenPos!.Value.Position - screenPos.Position).Length() > _deadzone)
                {
                    StartDrag();
                }

                break;
            }
            case DragState.Dragging:
                UpdateDrag(frameTime);
                break;
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // Update position every frame to make it smooth.
        if (Exists(_dragShadow))
        {
            var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
            Transform(_dragShadow.Value).WorldPosition = mousePos.Position;
        }
    }
}

public enum DragState : byte
{
    NotDragging,

    /// <summary>
    /// Not dragging yet, waiting to see
    /// if they hold for long enough
    /// </summary>
    MouseDown,

    /// <summary>
    /// Currently dragging something
    /// </summary>
    Dragging,
}
