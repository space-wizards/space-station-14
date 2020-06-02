using System.Collections.Generic;
using Content.Client.Interfaces.GameObjects.Components.Interaction;
using Content.Client.State;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.State;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.EntitySystems
{
    /// <summary>
    /// Handles clientside drag and drop logic
    /// </summary>
    [UsedImplicitly]
    public class DragDropSystem : EntitySystem
    {
        // how long to wait on mousedown until initiating a drag
        private const float WaitForDragTime = 0.5f;
        // mouse must remain within this distance of
        // mousedown screen position in order for drag to be triggered.
        // any movement beyond this will cancel the drag
        private const float DragDeadzone = 0.05f;
        // how often to recheck possible targets (prevents calling expensive
        // check logic each update)
        private const float TargetRecheckInterval = 0.25f;

        // if a drag ends up being cancelled and it has been under this
        // amount of time since the mousedown, we will "replay" the original
        // mousedown event so it can be treated like a regular click
        private const float MaxMouseDownTimeForReplayingClick = 0.1f;

        private const string ShaderDropTargetInRange = "SelectionOutlineInrange";
        private const string ShaderDropTargetOutOfRange = "SelectionOutline";

#pragma warning disable 649
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        // entity performing the drag action
        private IEntity _dragger;
        private IEntity _draggedEntity;
        private IClientDraggable _draggable;
        private IEntity _dragShadow;
        private DragState _state;
        // time since mouse down over the dragged entity
        private float _mouseDownTime;
        // screen pos where the mouse down began
        private Vector2 _mouseDownScreenPos;
        // how much time since last recheck of all possible targets
        private float _targetRecheckTime;
        // reserved initial mousedown event so we can replay it if no drag ends up being performed
        private PointerInputCmdHandler.PointerInputCmdArgs? _savedMouseDown;
        // whether we are currently replaying the original mouse down, so we
        // can ignore any events sent to this system
        private bool _isReplaying;

        private ShaderInstance _dropTargetInRangeShader;
        private ShaderInstance _dropTargetOutOfRangeShader;
        private SharedInteractionSystem _interactionSystem;
        private InputSystem _inputSystem;

        private List<SpriteComponent> highlightedSprites = new List<SpriteComponent>();

        private enum DragState
        {
            NOT_DRAGGING,
            // not dragging yet, waiting to see
            // if they hold for long enough
            MOUSEDOWN,
            // currently dragging something
            DRAGGING,
        }


        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            _state = DragState.NOT_DRAGGING;

            _dropTargetInRangeShader = _prototypeManager.Index<ShaderPrototype>(ShaderDropTargetInRange).Instance();
            _dropTargetOutOfRangeShader = _prototypeManager.Index<ShaderPrototype>(ShaderDropTargetOutOfRange).Instance();
            _interactionSystem = EntitySystem.Get<SharedInteractionSystem>();
            _inputSystem = EntitySystem.Get<InputSystem>();
            // needs to fire on mouseup and mousedown so we can detect a drag / drop,
            // TODO: Do we need to force it to run before constructor system
            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, false))
                .Register<DragDropSystem>();

        }

        public override void Shutdown()
        {
            CancelDrag(false);
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

        private bool OnUseMouseDown(PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            //TODO: Should we be using args.EntityUid to see what was clicked?

            var dragger = args.Session.AttachedEntity;
            // cancel any current dragging if there is one (shouldn't be because they would've had to have lifted
            // the mouse, canceling the drag, but just being cautious)
            CancelDrag(false);

            // possibly initiating a drag
            // check if the clicked entity is draggable
            if (_entityManager.TryGetEntity(args.EntityUid, out var entity))
            {
                //TODO: Refactor to use a shared InteractionChecks
                // check if the entity is reachable
                if (_interactionSystem.InRangeUnobstructed(dragger.Transform.MapPosition,
                    entity.Transform.MapPosition, ignoredEnt: dragger) == false)
                {
                    {
                        return false;
                    }
                }
                foreach (var draggable in entity.GetAllComponents<IClientDraggable>())
                {
                    var dragEventArgs = new CanDragEventArgs(args.Session.AttachedEntity, entity);
                    if (draggable.ClientCanDrag(dragEventArgs))
                    {
                        // wait to initiate a drag
                        _dragger = dragger;
                        _draggedEntity = entity;
                        _draggable = draggable;
                        _mouseDownTime = 0;
                        _state = DragState.MOUSEDOWN;
                        _mouseDownScreenPos = _inputManager.MouseScreenPosition;
                        // don't want anything else to process the click,
                        // but we will save the event so we can "re-play" it if this drag does
                        // not turn into an actual drag so the click can be handled normally
                        _savedMouseDown = args;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool OnUseMouseUp(PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_state == DragState.MOUSEDOWN)
            {
                // quick mouseup, definitely treat it as a normal click by
                // replaying the original
                CancelDrag(true);
                return false;
            }
            if (_state != DragState.DRAGGING) return false;

            // remaining CancelDrag calls will not replay the click because
            // by this time we've determined the input was actually a drag attempt


            //TODO: Some of this logic is duplicated in HighlightTargets

            // tell the server we are dropping if we are over a valid drop target in range.
            // We don't use args.EntityUid here because drag interactions generally should
            // work even if there's something "on top" of the drop target
            //TODO: Refactor to use a shared InteractionChecks
            if (_interactionSystem.InRangeUnobstructed(_dragger.Transform.MapPosition,
                    args.Coordinates.ToMap(_mapManager), ignoredEnt: _dragger) == false)
            {
                {
                    CancelDrag(false);
                    return false;
                }
            }

            var entities = GameScreenBase.GetEntitiesUnderPosition(_stateManager, args.Coordinates);
            foreach (var entity in entities)
            {
                // check if it's able to be dropped on by current dragged entity
                if (_draggable.ClientCanDropOn(new CanDropEventArgs(_dragger, _draggedEntity, entity)))
                {
                    // tell the server about the drop attempt
                    // TODO: implement
                    Logger.Info("Dropped {0} on {1}", _draggedEntity.Name, entity.Name);

                    CancelDrag(false);
                    return true;
                }
            }
            CancelDrag(false);
            return false;
        }

        private void StartDragging()
        {
            // this is checked elsewhere but adding this as a failsafe
            if (_draggedEntity == null || _draggedEntity.Deleted)
            {
                Logger.Warning("Programming error. Cannot initiate drag, no dragged entity or entity" +
                               " was deleted.");
                return;
            }

            if (_draggedEntity.TryGetComponent<SpriteComponent>(out var draggedSprite))
            {
                _state = DragState.DRAGGING;
                // pop up drag shadow under mouse
                var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
                _dragShadow = _entityManager.SpawnEntity("dragshadow", mousePos);
                var dragSprite = _dragShadow.GetComponent<SpriteComponent>();
                dragSprite.CopyFrom(draggedSprite);
                // TODO: apparently this ensures its drawn on top? Maybe refactor to method?
                dragSprite.RenderOrder = EntityManager.CurrentTick.Value;
                HighlightTargets();
            }
            else
            {
                Logger.Warning("Unable to display drag shadow for {0} because it" +
                               " has no sprite component.", _draggedEntity.Name);
            }
        }

        private void HighlightTargets()
        {
            if (_state != DragState.DRAGGING || _draggedEntity == null ||
                _draggedEntity.Deleted || _dragShadow == null || _dragShadow.Deleted)
            {
                Logger.Warning("Programming error. Can't highlight drag and drop targets, not currently " +
                               "dragging anything or dragged entity / shadow was deleted.");
                return;
            }

            // highlights the possible targets which are visible
            // and able to be dropped on by the current dragged entity

            // remove current highlights
            RemoveHighlights();

            //TODO: Do we really want to highlight out of range stuff?
            // find possible targets on screen even if not reachable
            // TODO: Duplicated in SpriteSystem
            var pvsBounds = _eyeManager.GetWorldViewport().Enlarged(5);
            var pvsEntities = EntityManager.GetEntitiesIntersecting(_eyeManager.CurrentMap, pvsBounds, true);
            foreach (var pvsEntity in pvsEntities)
            {
                if (pvsEntity.TryGetComponent<SpriteComponent>(out var inRangeSprite))
                {
                    // can't highlight if there's no sprite or it's not visible
                    if (inRangeSprite.Visible == false) continue;

                    // check if it's able to be dropped on by current dragged entity
                    if (_draggable.ClientCanDropOn(new CanDropEventArgs(_dragger, _draggedEntity, pvsEntity)))
                    {
                        // highlight depending on whether its in or out of range
                        // TODO: Concerned about the cost of doing this
                        //TODO: Refactor to use a shared InteractionChecks
                        var inRange = _interactionSystem.InRangeUnobstructed(_dragger.Transform.MapPosition,
                            pvsEntity.Transform.MapPosition, ignoredEnt: _dragger);
                        //TODO: Duplicated in InteractionOutline
                        inRangeSprite.PostShader = inRange ? _dropTargetInRangeShader : _dropTargetOutOfRangeShader;
                        inRangeSprite.RenderOrder = EntityManager.CurrentTick.Value;
                        highlightedSprites.Add(inRangeSprite);
                    }
                }
            }
        }

        private void RemoveHighlights()
        {
            foreach (var highlightedSprite in highlightedSprites)
            {
                //TODO: Duplicated in InteractionOutline
                highlightedSprite.PostShader = null;
                highlightedSprite.RenderOrder = 0;
            }
            highlightedSprites.Clear();
        }

        /// <summary>
        /// Cancels the drag, firing our saved drag event if instructed to do so and
        /// we are within the threshold for replaying the click
        /// (essentially reverting the drag attempt and allowing the original click
        /// to proceed as if no drag was performed)
        /// </summary>
        private void CancelDrag(bool fireSavedCmd)
        {
            RemoveHighlights();
            if (_dragShadow != null)
            {
                _entityManager.DeleteEntity(_dragShadow);
            }

            _dragShadow = null;
            _draggedEntity = null;
            _draggable = null;
            _dragger = null;
            _state = DragState.NOT_DRAGGING;

            _mouseDownTime = 0;

            if (fireSavedCmd && _savedMouseDown.HasValue && _mouseDownTime < MaxMouseDownTimeForReplayingClick)
            {
                var savedValue = _savedMouseDown.Value;
                _isReplaying = true;
                _inputSystem.HandleInputCommand(savedValue.Session, EngineKeyFunctions.Use,
                    savedValue.OriginalMessage);
                _isReplaying = false;
            }

            _savedMouseDown = null;

        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (_state == DragState.MOUSEDOWN)
            {
                var screenPos = _inputManager.MouseScreenPosition;
                if (_draggedEntity == null || _draggedEntity.Deleted || (_mouseDownScreenPos - screenPos).Length > DragDeadzone)
                {
                    // something happened to the clicked entity or we moved the mouse off the target so
                    // we shouldn't replay the original click
                    CancelDrag(false);
                    return;
                }
                else
                {
                    // tick down
                    _mouseDownTime += frameTime;
                    if (_mouseDownTime > WaitForDragTime)
                    {
                        // initiate actual drag
                        StartDragging();
                        _mouseDownTime = 0;
                    }
                }
            }
            else if (_state == DragState.DRAGGING)
            {
                if (_draggedEntity == null || _draggedEntity.Deleted)
                {
                    CancelDrag(false);
                    return;
                }
                // still in range of the thing we are dragging?
                //TODO: Refactor to use a shared InteractionChecks
                if (_interactionSystem.InRangeUnobstructed(_dragger.Transform.MapPosition,
                        _draggedEntity.Transform.MapPosition, ignoredEnt: _dragger) == false)
                {
                    CancelDrag(false);
                    return;
                }

                // keep dragged entity under mouse
                var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
                // TODO: would use MapPosition instead if it had a setter, but it has no setter.
                // Is this intentional, or should we add a setter for Transform.MapPosition?
                _dragShadow.Transform.WorldPosition = mousePos.Position;

                _targetRecheckTime += frameTime;
                if (_targetRecheckTime > TargetRecheckInterval)
                {
                    HighlightTargets();
                    _targetRecheckTime = 0;
                }

            }
        }
    }
}
