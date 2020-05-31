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
        private const float WaitForDragTime = 0.25f;
        // mouse must remain within this distance of
        // mousedown screen position in order for drag to be triggered.
        // any movement beyond this will cancel the drag
        private const float DragDeadzone = 0.05f;
        // how often to recheck possible targets (prevents calling expensive
        // check logic each update)
        private const float TargetRecheckInterval = 0.1f;

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

        private ShaderInstance _dropTargetInRangeShader;
        private ShaderInstance _dropTargetOutOfRangeShader;
        private SharedInteractionSystem _interactionSystem;

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
            // needs to fire on mouseup and mousedown so we can detect a drag / drop,
            // TODO: Do we need to force it to run before constructor system
            CommandBinds.Builder
                .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, false))
                .Register<DragDropSystem>();

        }

        public override void Shutdown()
        {
            CancelDrag();
            CommandBinds.Unregister<DragDropSystem>();
            base.Shutdown();
        }

        private bool OnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
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
            // cancel any current dragging if there is one
            CancelDrag();

            // possibly initiating a drag, check if there are any draggable entities
            // under the mouse
            if (_interactionSystem.InRangeUnobstructed(dragger.Transform.MapPosition,
                    args.Coordinates.ToMap(_mapManager), ignoredEnt: dragger) == false)
            {
                {
                    return false;
                }
            }

            var entities = GameScreenBase.GetEntitiesUnderPosition(_stateManager, args.Coordinates);
            foreach (var entity in entities)
            {
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
                        // don't want anything else to process the click
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool OnUseMouseUp(PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_state != DragState.DRAGGING) return false;

            //TODO: Some of this logic is duplicated in HighlightTargets

            // tell the server we are dropping if we are over a valid drop target in range
            //TODO: use the new way for this
            if (_interactionSystem.InRangeUnobstructed(_dragger.Transform.MapPosition,
                args.Coordinates.ToMap(_mapManager), ignoredEnt: _dragger) == false)
            {
                {
                    CancelDrag();
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

                    CancelDrag();
                }
            }

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

        private void CancelDrag()
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
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (_state == DragState.MOUSEDOWN)
            {
                var screenPos = _inputManager.MouseScreenPosition;
                if (_draggedEntity == null || _draggedEntity.Deleted || (_mouseDownScreenPos - screenPos).Length > DragDeadzone)
                {
                    CancelDrag();
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
                // TODO: Check if still in range of dragged entity
                if (_draggedEntity == null || _draggedEntity.Deleted)
                {
                    CancelDrag();
                    return;
                }
                // still in range of the thing we are dragging?
                if (_interactionSystem.InRangeUnobstructed(_dragger.Transform.MapPosition,
                        _draggedEntity.Transform.MapPosition, ignoredEnt: _dragger) == false)
                {
                    CancelDrag();
                    return;
                }

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
