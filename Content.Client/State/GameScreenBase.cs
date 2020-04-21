using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components;
using Content.Shared.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.State
{
    // OH GOD.
    // Ok actually it's fine.
    // Instantiated dynamically through the StateManager, Dependencies will be resolved.
    public partial class GameScreenBase : StateBase
    {
#pragma warning disable 649
        [Dependency] private readonly IClientEntityManager _entityManager;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IGameTiming _timing;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        private IEntity _lastHoveredEntity;

        public override void Startup()
        {
            _inputManager.KeyBindStateChanged += OnKeyBindStateChanged;
        }

        public override void Shutdown()
        {
            _inputManager.KeyBindStateChanged -= OnKeyBindStateChanged;
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            base.FrameUpdate(e);

            var mousePosWorld = _eyeManager.ScreenToWorld(new ScreenCoordinates(_inputManager.MouseScreenPosition));
            var entityToClick = GetEntityUnderPosition(mousePosWorld);

            var inRange = false;
            if (_playerManager.LocalPlayer.ControlledEntity != null && entityToClick != null)
            {
                var playerPos = _playerManager.LocalPlayer.ControlledEntity.Transform.WorldPosition;
                var entityPos = entityToClick.Transform.WorldPosition;
                inRange = (entityPos - playerPos).Length <= VerbUtility.InteractionRange;
            }

            InteractionOutlineComponent outline;
            if (entityToClick == _lastHoveredEntity)
            {
                if (entityToClick != null && entityToClick.TryGetComponent(out outline))
                {
                    outline.UpdateInRange(inRange);
                }
                return;
            }

            if (_lastHoveredEntity != null && !_lastHoveredEntity.Deleted &&
                _lastHoveredEntity.TryGetComponent(out outline))
            {
                outline.OnMouseLeave();
            }

            _lastHoveredEntity = entityToClick;

            if (_lastHoveredEntity != null && _lastHoveredEntity.TryGetComponent(out outline))
            {
                outline.OnMouseEnter(inRange);
            }
        }

        public IEntity GetEntityUnderPosition(GridCoordinates coordinates)
        {
            var entitiesUnderPosition = GetEntitiesUnderPosition(coordinates);
            return entitiesUnderPosition.Count > 0 ? entitiesUnderPosition[0] : null;
        }

        public IList<IEntity> GetEntitiesUnderPosition(GridCoordinates coordinates)
        {
            // Find all the entities intersecting our click
            var mapCoords = coordinates.ToMap(_mapManager);
            var entities = _entityManager.GetEntitiesIntersecting(mapCoords.MapId, mapCoords.Position);

            // Check the entities against whether or not we can click them
            var foundEntities = new List<(IEntity clicked, int drawDepth)>();
            foreach (var entity in entities)
            {
                if (entity.TryGetComponent<IClientClickableComponent>(out var component)
                    && entity.Transform.IsMapTransform
                    && component.CheckClick(coordinates.Position, out var drawDepthClicked))
                {
                    foundEntities.Add((entity, drawDepthClicked));
                }
            }

            if (foundEntities.Count == 0)
                return new List<IEntity>();

            foundEntities.Sort(new ClickableEntityComparer());
            // 0 is the top element.
            foundEntities.Reverse();
            return foundEntities.Select(a => a.clicked).ToList();
        }

        internal class ClickableEntityComparer : IComparer<(IEntity clicked, int depth)>
        {
            public int Compare((IEntity clicked, int depth) x, (IEntity clicked, int depth) y)
            {
                var val = x.depth.CompareTo(y.depth);
                if (val != 0)
                {
                    return val;
                }

                var transx = x.clicked.Transform;
                var transy = y.clicked.Transform;
                return transx.GridPosition.Y.CompareTo(transy.GridPosition.Y);
            }
        }

        /// <summary>
        ///     Converts a state change event from outside the simulation to inside the simulation.
        /// </summary>
        /// <param name="args">Event data values for a bound key state change.</param>
        private void OnKeyBindStateChanged(BoundKeyEventArgs args)
        {
            var inputSys = _entitySystemManager.GetEntitySystem<InputSystem>();

            var func = args.Function;
            var funcId = _inputManager.NetworkBindMap.KeyFunctionID(func);

            var mousePosWorld = _eyeManager.ScreenToWorld(args.PointerLocation);
            var entityToClick = GetEntityUnderPosition(mousePosWorld);
            var message = new FullInputCmdMessage(_timing.CurTick, funcId, args.State, mousePosWorld,
                args.PointerLocation, entityToClick?.Uid ?? EntityUid.Invalid);

            // client side command handlers will always be sent the local player session.
            var session = _playerManager.LocalPlayer.Session;
            inputSys.HandleInputCommand(session, func, message);
        }
    }
}
