using System.Linq;
using Content.Shared.Tabletop;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.Tabletop
{
    [UsedImplicitly]
    public class TabletopDragDropSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

        private IEntity? _draggedEntity;

        public override void Initialize()
        {
            CommandBinds.Builder
                        .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(OnUse, false))
                        .Register<TabletopDragDropSystem>();
        }

        public override void Update(float frameTime)
        {
            if (_draggedEntity == null) return;

            var worldPos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            EntityCoordinates coords = new(_mapManager.GetMapEntityId(worldPos.MapId), worldPos.Position);

            RaiseNetworkEvent(new TabletopMoveEvent(_draggedEntity.Uid, coords));
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
