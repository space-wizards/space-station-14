using System;
using System.Linq;
using Content.Client.Viewport;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.CustomControls;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Client.GameObjects;
using EyeComponent = Robust.Client.GameObjects.EyeComponent;

namespace Content.Client.Tabletop
{
    [UsedImplicitly]
    public class ClientTabletopSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        /**
         * Time in seconds to wait until sending the location of a dragged entity to the server again.
         */
        private const float Delay = 0.1f;

        // Entity being dragged
        private IEntity? _draggedEntity;

        // Time passed since last update sent to the server.
        private float _timePassed = 0f;

        public override void Initialize()
        {
            CommandBinds.Builder
                        .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, false))
                        .Register<ClientTabletopSystem>();

            SubscribeNetworkEvent<TabletopPlayEvent>(TabletopPlayHandler);
        }

        /**
         * <summary>
         * Runs when the player presses the "Play Game" verb on a tabletop game.
         * Opens a viewport where they can then play the game.
         * </summary>
         */
        private void TabletopPlayHandler(TabletopPlayEvent msg)
        {
            // TODO: remove log message
            Logger.Info("Game started: " + msg.Title);

            var camera = EntityManager.GetEntity(msg.CameraUid);

            var window = new SS14Window
            {
                MinWidth = 400,
                MinHeight = 400
            };

            if (!camera.TryGetComponent<EyeComponent>(out var eyeComponent))
            {
                throw new Exception("Camera does not have EyeComponent.");
            }

            var viewport = new ScalingViewport
            {
                Eye = eyeComponent.Eye,
                ViewportSize = (400, 400)
            };

            window.Contents.AddChild(viewport);
            window.OpenCentered();
        }

        public override void Update(float frameTime)
        {
            // If no entity is being dragged, just return
            if (_draggedEntity == null) return;

            // Map mouse position to EntityCoordinates
            var worldPos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            EntityCoordinates coords = new(_mapManager.GetMapEntityId(worldPos.MapId), worldPos.Position);

            // Move the entity locally every update
            _draggedEntity.Transform.Coordinates = coords;

            // Increment total time passed
            _timePassed += frameTime;

            // Only send new position to server when Delay is reached
            if (_timePassed >= Delay)
            {
                RaiseNetworkEvent(new TabletopMoveEvent(_draggedEntity.Uid, coords));
                _timePassed = 0f;
            }
        }

        private bool OnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            return args.State switch
            {
                BoundKeyState.Down => OnMouseDown(args),
                BoundKeyState.Up => OnMouseUp(args),
                _ => false
            };
        }

        private bool OnMouseDown(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (!EntityManager.TryGetEntity(args.EntityUid, out var entity))
            {
                return false;
            }

            if (!entity.GetAllComponents<TabletopDraggableComponent>().Any(x => x.CanStartDrag()))
            {
                return false;
            }

            // Set the dragged entity
            _draggedEntity = entity;

            return true;
        }

        private bool OnMouseUp(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            // Unset the dragged entity
            _draggedEntity = null;

            return true;
        }

    }
}
