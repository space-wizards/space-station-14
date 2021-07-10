using System.Linq;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;

namespace Content.Client.Tabletop
{
    [UsedImplicitly]
    public class TabletopDragDropSystem : EntitySystem
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
                        .Register<TabletopDragDropSystem>();

            EntityManager.EventBus.SubscribeEvent<TabletopPlayEvent>(EventSource.Network, this, TabletopPlayHandler);
        }

        private void TabletopPlayHandler(TabletopPlayEvent msg)
        {
            Logger.Info("Game started");
        }

        public override void Update(float frameTime)
        {
            if (_draggedEntity == null) return;

            var worldPos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            EntityCoordinates coords = new(_mapManager.GetMapEntityId(worldPos.MapId), worldPos.Position);

            // Move the entity locally every update
            _draggedEntity.Transform.Coordinates = coords;

            _timePassed += frameTime;

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

            if (!entity.GetAllComponents<ITabletopDraggable>().Any(x => x.CanStartDrag()))
            {
                return false;
            }

            _draggedEntity = entity;

            return true;
        }

        private bool OnMouseUp(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            _draggedEntity = null;

            return true;
        }

    }
}
