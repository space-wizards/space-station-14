using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Content.Client.GameObjects.Components;
using Content.Client.Utility;
using Content.Shared.Utility;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.State
{
    // OH GOD.
    // Ok actually it's fine.
    // Instantiated dynamically through the StateManager, Dependencies will be resolved.
    public partial class GameScreenBase : Robust.Client.State.State
    {
        [Dependency] protected readonly IClientEntityManager EntityManager = default!;
        [Dependency] protected readonly IInputManager InputManager = default!;
        [Dependency] protected readonly IPlayerManager PlayerManager = default!;
        [Dependency] protected readonly IEyeManager EyeManager = default!;
        [Dependency] protected readonly IEntitySystemManager EntitySystemManager = default!;
        [Dependency] protected readonly IGameTiming Timing = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] protected readonly IUserInterfaceManager UserInterfaceManager = default!;
        [Dependency] protected readonly IConfigurationManager ConfigurationManager = default!;

        private IEntity _lastHoveredEntity;

        public override void Startup()
        {
            InputManager.KeyBindStateChanged += OnKeyBindStateChanged;
        }

        public override void Shutdown()
        {
            InputManager.KeyBindStateChanged -= OnKeyBindStateChanged;
        }

        public override void FrameUpdate(FrameEventArgs e)
        {
            base.FrameUpdate(e);

            // If there is no local player, there is no session, and therefore nothing to do here.
            var localPlayer = PlayerManager.LocalPlayer;
            if (localPlayer == null)
                return;

            var mousePosWorld = EyeManager.ScreenToMap(InputManager.MouseScreenPosition);
            var entityToClick = UserInterfaceManager.CurrentlyHovered != null
                ? null
                : GetEntityUnderPosition(mousePosWorld);

            var inRange = false;
            if (localPlayer.ControlledEntity != null && entityToClick != null)
            {
                inRange = localPlayer.InRangeUnobstructed(entityToClick, ignoreInsideBlocker: true);
            }

            InteractionOutlineComponent outline;
            if(!ConfigurationManager.GetCVar<bool>("outline.enabled"))
            {
                if(entityToClick != null && entityToClick.TryGetComponent(out outline))
                {
                    outline.OnMouseLeave(); //Prevent outline remains from persisting post command.
                }
                return;
            }

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

        public IEntity GetEntityUnderPosition(MapCoordinates coordinates)
        {
            var entitiesUnderPosition = GetEntitiesUnderPosition(coordinates);
            return entitiesUnderPosition.Count > 0 ? entitiesUnderPosition[0] : null;
        }

        public IList<IEntity> GetEntitiesUnderPosition(GridCoordinates coordinates)
        {
            return GetEntitiesUnderPosition(coordinates.ToMap(MapManager));
        }

        public IList<IEntity> GetEntitiesUnderPosition(MapCoordinates coordinates)
        {
            // Find all the entities intersecting our click
            var entities = EntityManager.GetEntitiesIntersecting(coordinates.MapId,
                Box2.CenteredAround(coordinates.Position, (1, 1)));

            // Check the entities against whether or not we can click them
            var foundEntities = new List<(IEntity clicked, int drawDepth, uint renderOrder)>();
            foreach (var entity in entities)
            {
                if (entity.TryGetComponent<ClickableComponent>(out var component)
                    && entity.Transform.IsMapTransform
                    && component.CheckClick(coordinates.Position, out var drawDepthClicked, out var renderOrder))
                {
                    foundEntities.Add((entity, drawDepthClicked, renderOrder));
                }
            }

            if (foundEntities.Count == 0)
                return new List<IEntity>();

            foundEntities.Sort(new ClickableEntityComparer());
            // 0 is the top element.
            foundEntities.Reverse();
            return foundEntities.Select(a => a.clicked).ToList();
        }

        /// <summary>
        /// Gets all entities intersecting the given position.
        ///
        /// Static alternative to GetEntitiesUnderPosition to cut out
        /// some of the boilerplate needed to get state manager and check the current state.
        /// </summary>
        /// <param name="stateManager">state manager to use to get the current game screen</param>
        /// <param name="coordinates">coordinates to check</param>
        /// <returns>the entities under the position, empty list if none found</returns>
        public static IList<IEntity> GetEntitiesUnderPosition(IStateManager stateManager, GridCoordinates coordinates)
        {
            if (stateManager.CurrentState is GameScreenBase gameScreenBase)
            {
                return gameScreenBase.GetEntitiesUnderPosition(coordinates);
            }

            return ImmutableList<IEntity>.Empty;
        }

        internal class ClickableEntityComparer : IComparer<(IEntity clicked, int depth, uint renderOrder)>
        {
            public int Compare((IEntity clicked, int depth, uint renderOrder) x,
                (IEntity clicked, int depth, uint renderOrder) y)
            {
                var val = x.depth.CompareTo(y.depth);
                if (val != 0)
                {
                    return val;
                }

                // Turning this off it can make picking stuff out of lockers and such up a bit annoying.
                /*
                val = x.renderOrder.CompareTo(y.renderOrder);
                if (val != 0)
                {
                    return val;
                }
                */

                var transx = x.clicked.Transform;
                var transy = y.clicked.Transform;
                val = transx.GridPosition.Y.CompareTo(transy.GridPosition.Y);
                if (val != 0)
                {
                    return val;
                }

                return x.clicked.Uid.CompareTo(y.clicked.Uid);
            }
        }

        /// <summary>
        ///     Converts a state change event from outside the simulation to inside the simulation.
        /// </summary>
        /// <param name="args">Event data values for a bound key state change.</param>
        private void OnKeyBindStateChanged(BoundKeyEventArgs args)
        {
            // If there is no InputSystem, then there is nothing to forward to, and nothing to do here.
            if(!EntitySystemManager.TryGetEntitySystem(out InputSystem inputSys))
                return;

            var func = args.Function;
            var funcId = InputManager.NetworkBindMap.KeyFunctionID(func);

            var mousePosWorld = EyeManager.ScreenToMap(args.PointerLocation);
            var entityToClick = GetEntityUnderPosition(mousePosWorld);

            if (!MapManager.TryFindGridAt(mousePosWorld, out var grid))
                grid = MapManager.GetDefaultGrid(mousePosWorld.MapId);

            var message = new FullInputCmdMessage(Timing.CurTick, Timing.TickFraction, funcId, args.State,
                grid.MapToGrid(mousePosWorld), args.PointerLocation,
                entityToClick?.Uid ?? EntityUid.Invalid);

            // client side command handlers will always be sent the local player session.
            var session = PlayerManager.LocalPlayer.Session;
            if (inputSys.HandleInputCommand(session, func, message))
            {
                args.Handle();
            }
        }
    }
}
