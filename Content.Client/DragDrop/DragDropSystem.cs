using System.Collections.Generic;
using Content.Client.State;
using Content.Client.Viewport;
using Content.Shared.ActionBlocker;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.DragDrop
{
    /// <summary>
    /// Handles clientside drag and drop logic
    /// </summary>
    [UsedImplicitly]
    public class DragDropSystem : EntitySystem
    {
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly InputSystem _inputSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        // how often to recheck possible targets (prevents calling expensive
        // check logic each update)
        private const float TargetRecheckInterval = 0.25f;

        // if a drag ends up being cancelled and it has been under this
        // amount of time since the mousedown, we will "replay" the original
        // mousedown event so it can be treated like a regular click
        private const float MaxMouseDownTimeForReplayingClick = 0.85f;

        private const string ShaderDropTargetInRange = "SelectionOutlineInrange";
        private const string ShaderDropTargetOutOfRange = "SelectionOutline";

        // entity performing the drag action

        private IEntity? _dragger;
        private readonly List<IDraggable> _draggables = new();
        private IEntity? _dragShadow;

        // time since mouse down over the dragged entity
        private float _mouseDownTime;
        // how much time since last recheck of all possible targets
        private float _targetRecheckTime;
        // reserved initial mousedown event so we can replay it if no drag ends up being performed
        private PointerInputCmdHandler.PointerInputCmdArgs? _savedMouseDown;
        // whether we are currently replaying the original mouse down, so we
        // can ignore any events sent to this system
        private bool _isReplaying;

        private DragDropHelper<IEntity> _dragDropHelper = default!;

        private ShaderInstance? _dropTargetInRangeShader;
        private ShaderInstance? _dropTargetOutOfRangeShader;

        private readonly List<ISpriteComponent> _highlightedSprites = new();

        public override void Initialize()
        {
            _dragDropHelper = new DragDropHelper<IEntity>(OnBeginDrag, OnContinueDrag, OnEndDrag);

            _dropTargetInRangeShader = _prototypeManager.Index<ShaderPrototype>(ShaderDropTargetInRange).Instance();
            _dropTargetOutOfRangeShader = _prototypeManager.Index<ShaderPrototype>(ShaderDropTargetOutOfRange).Instance();
            // needs to fire on mouseup and mousedown so we can detect a drag / drop
            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, false))
                .Register<DragDropSystem>();

        }

        public override void Shutdown()
        {
            _dragDropHelper.EndDrag();
            CommandBinds.Unregister<DragDropSystem>();
            base.Shutdown();
        }

        private bool OnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            // not currently predicted
            if (_inputSystem.Predicted) return false;

            // currently replaying a saved click, don't handle this because
            // we already decided this click doesn't represent an actual drag attempt
            if (_isReplaying) return false;

            if (args.State == BoundKeyState.Down)
            {
                return OnUseMouseDown(args);
            }
            else if (args.State == BoundKeyState.Up)
            {
                return OnUseMouseUp(args);
            }

            return false;
        }

        private bool OnUseMouseDown(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.Session?.AttachedEntity == null)
            {
                return false;
            }

            var dragger = args.Session.AttachedEntity;
            // cancel any current dragging if there is one (shouldn't be because they would've had to have lifted
            // the mouse, canceling the drag, but just being cautious)
            _dragDropHelper.EndDrag();

            // possibly initiating a drag
            // check if the clicked entity is draggable
            if (!EntityManager.TryGetEntity(args.EntityUid, out var entity))
            {
                return false;
            }

            // check if the entity is reachable
            if (!_interactionSystem.InRangeUnobstructed(dragger, entity))
            {
                return false;
            }

            var canDrag = false;
            foreach (var draggable in entity.GetAllComponents<IDraggable>())
            {
                var dragEventArgs = new StartDragDropEvent(dragger, entity);

                if (!draggable.CanStartDrag(dragEventArgs))
                {
                    continue;
                }

                _draggables.Add(draggable);
                canDrag = true;
            }

            if (!canDrag)
            {
                return false;
            }

            // wait to initiate a drag
            _dragDropHelper.MouseDown(entity);
            _dragger = dragger;
            _mouseDownTime = 0;

            // don't want anything else to process the click,
            // but we will save the event so we can "re-play" it if this drag does
            // not turn into an actual drag so the click can be handled normally
            _savedMouseDown = args;

            return true;

        }

        private bool OnBeginDrag()
        {
            if (_dragDropHelper.Dragged == null || _dragDropHelper.Dragged.Deleted)
            {
                // something happened to the clicked entity or we moved the mouse off the target so
                // we shouldn't replay the original click
                return false;
            }

            if (_dragDropHelper.Dragged.TryGetComponent<SpriteComponent>(out var draggedSprite))
            {
                // pop up drag shadow under mouse
                var mousePos = _eyeManager.ScreenToMap(_dragDropHelper.MouseScreenPosition);
                _dragShadow = EntityManager.SpawnEntity("dragshadow", mousePos);
                var dragSprite = _dragShadow.GetComponent<SpriteComponent>();
                dragSprite.CopyFrom(draggedSprite);
                dragSprite.RenderOrder = EntityManager.CurrentTick.Value;
                dragSprite.Color = dragSprite.Color.WithAlpha(0.7f);
                // keep it on top of everything
                dragSprite.DrawDepth = (int) DrawDepth.Overlays;
                if (!dragSprite.NoRotation)
                {
                    _dragShadow.Transform.WorldRotation = _dragDropHelper.Dragged.Transform.WorldRotation;
                }

                HighlightTargets();
                EntityManager.EventBus.RaiseEvent(EventSource.Local, new OutlineToggleMessage(false));

                // drag initiated
                return true;
            }

            Logger.Warning("Unable to display drag shadow for {0} because it" +
                           " has no sprite component.", _dragDropHelper.Dragged.Name);
            return false;
        }

        private bool OnContinueDrag(float frameTime)
        {
            if (_dragDropHelper.Dragged == null || _dragDropHelper.Dragged.Deleted)
            {
                return false;
            }

            DebugTools.AssertNotNull(_dragger);

            // still in range of the thing we are dragging?
            if (!_interactionSystem.InRangeUnobstructed(_dragger!, _dragDropHelper.Dragged))
            {
                return false;
            }

            // keep dragged entity under mouse
            var mousePos = _eyeManager.ScreenToMap(_dragDropHelper.MouseScreenPosition);
            // TODO: would use MapPosition instead if it had a setter, but it has no setter.
            // is that intentional, or should we add a setter for Transform.MapPosition?
            if (_dragShadow == null)
                return false;

            _dragShadow.Transform.WorldPosition = mousePos.Position;

            _targetRecheckTime += frameTime;
            if (_targetRecheckTime > TargetRecheckInterval)
            {
                HighlightTargets();
                _targetRecheckTime = 0;
            }

            return true;
        }

        private void OnEndDrag()
        {
            RemoveHighlights();
            if (_dragShadow != null)
            {
                EntityManager.DeleteEntity(_dragShadow);
            }

            EntityManager.EventBus.RaiseEvent(EventSource.Local, new OutlineToggleMessage(true));
            _dragShadow = null;
            _draggables.Clear();
            _dragger = null;
            _mouseDownTime = 0;
            _savedMouseDown = null;
        }

        private bool OnUseMouseUp(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_dragDropHelper.IsDragging == false || _dragDropHelper.Dragged == null)
            {
                // haven't started the drag yet, quick mouseup, definitely treat it as a normal click by
                // replaying the original cmd
                if (_savedMouseDown.HasValue && _mouseDownTime < MaxMouseDownTimeForReplayingClick)
                {
                    var savedValue = _savedMouseDown.Value;
                    _isReplaying = true;
                    // adjust the timing info based on the current tick so it appears as if it happened now
                    var replayMsg = savedValue.OriginalMessage;
                    var adjustedInputMsg = new FullInputCmdMessage(args.OriginalMessage.Tick, args.OriginalMessage.SubTick,
                        replayMsg.InputFunctionId, replayMsg.State, replayMsg.Coordinates, replayMsg.ScreenCoordinates, replayMsg.Uid);

                    if (savedValue.Session != null)
                    {
                        _inputSystem.HandleInputCommand(savedValue.Session, EngineKeyFunctions.Use, adjustedInputMsg, true);
                    }

                    _isReplaying = false;
                }
                _dragDropHelper.EndDrag();
                return false;
            }

            if (_dragger == null)
            {
                _dragDropHelper.EndDrag();
                return false;
            }

            // now when ending the drag, we will not replay the click because
            // by this time we've determined the input was actually a drag attempt
            var range = (args.Coordinates.ToMapPos(EntityManager) - _dragger.Transform.MapPosition.Position).Length + 0.01f;
            // tell the server we are dropping if we are over a valid drop target in range.
            // We don't use args.EntityUid here because drag interactions generally should
            // work even if there's something "on top" of the drop target
            if (!_interactionSystem.InRangeUnobstructed(_dragger,
                args.Coordinates, range, ignoreInsideBlocker: true))
            {
                _dragDropHelper.EndDrag();
                return false;
            }

            var entities = GameScreenBase.GetEntitiesUnderPosition(_stateManager, args.Coordinates);
            var outOfRange = false;

            foreach (var entity in entities)
            {
                if (entity == _dragDropHelper.Dragged) continue;

                // check if it's able to be dropped on by current dragged entity
                var dropArgs = new DragDropEvent(_dragger, args.Coordinates, _dragDropHelper.Dragged, entity);

                // TODO: Cache valid CanDragDrops
                if (ValidDragDrop(dropArgs) != true) continue;

                if (!dropArgs.InRangeUnobstructed(ignoreInsideBlocker: true))
                {
                    outOfRange = true;
                    continue;
                }

                foreach (var draggable in _draggables)
                {
                    if (!draggable.CanDrop(dropArgs)) continue;

                    // tell the server about the drop attempt
                    RaiseNetworkEvent(new DragDropRequestEvent(args.Coordinates, _dragDropHelper.Dragged!.Uid,
                        entity.Uid));

                    draggable.Drop(dropArgs);

                    _dragDropHelper.EndDrag();
                    return true;
                }
            }

            if (outOfRange)
            {
                _playerManager.LocalPlayer?.ControlledEntity?.PopupMessage(Loc.GetString("drag-drop-system-out-of-range-text"));
            }

            _dragDropHelper.EndDrag();
            return false;
        }

        private void HighlightTargets()
        {
            if (_dragDropHelper.Dragged == null ||
                _dragDropHelper.Dragged.Deleted ||
                _dragShadow == null ||
                _dragShadow.Deleted)
            {
                Logger.Warning("Programming error. Can't highlight drag and drop targets, not currently " +
                               "dragging anything or dragged entity / shadow was deleted.");
                return;
            }

            // highlights the possible targets which are visible
            // and able to be dropped on by the current dragged entity

            // remove current highlights
            RemoveHighlights();

            // find possible targets on screen even if not reachable
            // TODO: Duplicated in SpriteSystem
            var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition).Position;
            var bounds = new Box2(mousePos - 1.5f, mousePos + 1.5f);
            var pvsEntities = IoCManager.Resolve<IEntityLookup>().GetEntitiesIntersecting(_eyeManager.CurrentMap, bounds, LookupFlags.Approximate | LookupFlags.IncludeAnchored);
            foreach (var pvsEntity in pvsEntities)
            {
                if (!pvsEntity.TryGetComponent(out ISpriteComponent? inRangeSprite) ||
                    !inRangeSprite.Visible ||
                    pvsEntity == _dragDropHelper.Dragged) continue;

                // check if it's able to be dropped on by current dragged entity
                var dropArgs = new DragDropEvent(_dragger!, pvsEntity.Transform.Coordinates, _dragDropHelper.Dragged, pvsEntity);

                var valid = ValidDragDrop(dropArgs);
                if (valid == null) continue;

                // We'll do a final check given server-side does this before any dragdrop can take place.
                if (valid.Value)
                {
                    valid = dropArgs.InRangeUnobstructed(ignoreInsideBlocker: true);
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
                highlightedSprite.PostShader = null;
                highlightedSprite.RenderOrder = 0;
            }

            _highlightedSprites.Clear();
        }

        /// <summary>
        ///     Are these args valid for drag-drop?
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns>null if the target doesn't support IDragDropOn</returns>
        private bool? ValidDragDrop(DragDropEvent eventArgs)
        {
            if (!_actionBlockerSystem.CanInteract(eventArgs.User))
            {
                return false;
            }

            bool? valid = null;

            foreach (var comp in eventArgs.Target.GetAllComponents<IDragDropOn>())
            {
                if (!comp.CanDragDropOn(eventArgs))
                {
                    valid = false;
                    // dragDropOn.Add(comp);
                    continue;
                }

                valid = true;
                break;
            }

            if (valid != true) return valid;

            // Need at least one IDraggable to return true or else we can't do shit
            valid = false;

            foreach (var comp in eventArgs.User.GetAllComponents<IDraggable>())
            {
                if (!comp.CanDrop(eventArgs)) continue;
                valid = true;
                break;
            }

            return valid;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _dragDropHelper.Update(frameTime);
        }
    }
}
