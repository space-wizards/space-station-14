using System;
using Content.Client.Viewport;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.Tabletop
{
    [UsedImplicitly]
    public class TabletopSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManger = default!;

        // Time in seconds to wait until sending the location of a dragged entity to the server again
        private const float Delay = 1f / 10; // 10 Hz

        // Time passed since last update sent to the server.
        private float _timePassed;

        // Entity being dragged
        private IEntity? _draggedEntity;

        // Viewport being used
        private ScalingViewport? _viewport;

        public override void Initialize()
        {
            Console.WriteLine("abc");

            CommandBinds.Builder
                        .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, false))
                        .Register<TabletopSystem>();

            SubscribeNetworkEvent<TabletopPlayEvent>(TabletopPlayHandler);
        }

        public override void Update(float frameTime)
        {
            // If no entity is being dragged or no viewport is clicked, just return
            if (_draggedEntity == null || _viewport == null) return;

            if (!_draggedEntity.HasComponent<TabletopDraggableComponent>())
            {
                return;
            }

            // Map mouse position to EntityCoordinates
            var coords = _viewport.ScreenToMap(_inputManager.MouseScreenPosition.Position);

            // Clamp coordinates to viewport
            var clampedCoords = ClampPositionToViewport(coords, _viewport);
            if (clampedCoords.Equals(MapCoordinates.Nullspace)) return;

            // Move the entity locally every update
            _draggedEntity.Transform.LocalPosition = clampedCoords.Position;

            // Increment total time passed
            _timePassed += frameTime;

            // Only send new position to server when Delay is reached
            if (_timePassed >= Delay)
            {
                RaiseNetworkEvent(new TabletopMoveEvent(_draggedEntity.Uid, clampedCoords));
                _timePassed = 0f;
            }
        }

        /**
         * <summary>Clamps coordinates within a viewport. ONLY ACCOUNTS FOR 90 DEGREE ROTATIONS!</summary>
         */
        private static MapCoordinates ClampPositionToViewport(MapCoordinates coordinates, ScalingViewport viewport)
        {
            if (viewport.Eye == null) return MapCoordinates.Nullspace;

            var size = (Vector2) viewport.ViewportSize / 32; // Convert to tiles instead of pixels
            var eyePosition = viewport.Eye.Position.Position;
            var rotation = viewport.Eye.Rotation;
            var scale = viewport.Eye.Scale;

            var min = (eyePosition - size / 2) / scale;
            var max = (eyePosition + size / 2) / scale;

            // If 90/270 degrees rotated, flip X and Y
            if (MathHelper.CloseTo(rotation.Degrees % 180d, 90d) || MathHelper.CloseTo(rotation.Degrees % 180d, -90d))
            {
                (min.Y, min.X) = (min.X, min.Y);
                (max.Y, max.X) = (max.X, max.Y);
            }

            var clampedPosition = Vector2.Clamp(coordinates.Position, min, max);

            return new MapCoordinates(clampedPosition, coordinates.MapId);
        }

        /**
         * <summary>
         * Runs when the player presses the "Play Game" verb on a tabletop game.
         * Opens a viewport where they can then play the game.
         * </summary>
         */
        private void TabletopPlayHandler(TabletopPlayEvent msg)
        {
            var camera = EntityManager.GetEntity(msg.CameraUid);

            var window = new SS14Window
            {
                MinWidth = 500,
                MinHeight = 400 + 26,
                Title = msg.Title
            };

            if (!camera.TryGetComponent<EyeComponent>(out var eyeComponent))
            {
                throw new Exception("Camera does not have EyeComponent.");
            }

            var viewport = new ScalingViewport
            {
                Eye = eyeComponent.Eye,
                ViewportSize = (msg.Size.X, msg.Size.Y),
                MouseFilter = Control.MouseFilterMode.Stop, // Make the mouse interact with the viewport
                RenderScaleMode = ScalingViewportRenderScaleMode.CeilInt // Nearest neighbor scaling
            };

            window.Contents.AddChild(viewport);
            window.OpenCentered();
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
            // Set the entity being dragged and the viewport under the mouse
            if (!EntityManager.TryGetEntity(args.EntityUid, out _draggedEntity))
            {
                return false;
            }

            if (!_draggedEntity.HasComponent<TabletopDraggableComponent>())
            {
                return false;
            }

            _viewport = _uiManger.MouseGetControl(args.ScreenCoordinates) as ScalingViewport;

            return true;
        }

        private bool OnMouseUp(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            // Unset the dragged entity and viewport
            _draggedEntity = null;
            _viewport = null;

            return true;
        }
    }
}
