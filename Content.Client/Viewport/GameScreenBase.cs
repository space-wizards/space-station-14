using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Content.Client.Clickable;
using Content.Client.ContextMenu.UI;
using Content.Client.Interactable;
using Content.Client.Interactable.Components;
using Content.Client.State;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Viewport
{
    // OH GOD.
    // Ok actually it's fine.
    // Instantiated dynamically through the StateManager, Dependencies will be resolved.
    public partial class GameScreenBase : Robust.Client.State.State, IEntityEventSubscriber
    {
        [Dependency] protected readonly IClientEntityManager EntityManager = default!;
        [Dependency] protected readonly IInputManager InputManager = default!;
        [Dependency] protected readonly IPlayerManager PlayerManager = default!;
        [Dependency] protected readonly IEntitySystemManager EntitySystemManager = default!;
        [Dependency] protected readonly IGameTiming Timing = default!;
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] protected readonly IUserInterfaceManager UserInterfaceManager = default!;
        [Dependency] protected readonly IConfigurationManager ConfigurationManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        private IEventBus _eventBus => _entityManager.EventBus;

        private EntityUid? _lastHoveredEntity;

        private bool _outlineEnabled = true;

        public override void Startup()
        {
            InputManager.KeyBindStateChanged += OnKeyBindStateChanged;
            _eventBus.SubscribeEvent<OutlineToggleMessage>(EventSource.Local, this, HandleOutlineToggle);
        }

        public override void Shutdown()
        {
            InputManager.KeyBindStateChanged -= OnKeyBindStateChanged;
            _eventBus.UnsubscribeEvent<OutlineToggleMessage>(EventSource.Local, this);
        }

        private void HandleOutlineToggle(OutlineToggleMessage message)
        {
            _outlineEnabled = message.Enabled;
        }

        /// <summary>
        ///     Highlight the currently hovered entity.
        /// </summary>
        public override void FrameUpdate(FrameEventArgs e)
        {
            base.FrameUpdate(e);

            // If there is no local player, there is no session, and therefore nothing to do here.
            var localPlayer = PlayerManager.LocalPlayer;
            if (localPlayer == null)
                return;

            // TODO InteractionOutlineComponent
            // BUG: The logic that gets the renderScale here assumes that the entity is only visible in a single
            // viewport. The entity will be highlighted in ALL viewport where it is visible, regardless of which
            // viewport is being used to hover over it. If these Viewports have very different render scales, this may
            // lead to extremely thick outlines in the other viewports. Fixing this probably requires changing how the
            // hover outline works, so that it only highlights the entity in a single viewport.

            EntityUid? entityToClick = null;
            var renderScale = 1;
            if (UserInterfaceManager.CurrentlyHovered is IViewportControl vp)
            {
                var mousePosWorld = vp.ScreenToMap(InputManager.MouseScreenPosition.Position);
                entityToClick = GetEntityUnderPosition(mousePosWorld);

                if (vp is ScalingViewport svp)
                {
                    renderScale = svp.CurrentRenderScale;
                }
            }
            else if (UserInterfaceManager.CurrentlyHovered is EntityMenuElement element)
            {
                entityToClick = element.Entity;
                // TODO InteractionOutlineComponent
                // Currently we just take the renderscale from the main viewport. In the future, when the bug mentioned
                // above is fixed, the viewport should probably be the one that was clicked on to open the entity menu
                // in the first place.
                renderScale = _eyeManager.MainViewport.GetRenderScale();
            }

            var inRange = false;
            if (localPlayer.ControlledEntity != null && entityToClick != null)
            {
                inRange = localPlayer.InRangeUnobstructed(entityToClick.Value, ignoreInsideBlocker: true);
            }

            InteractionOutlineComponent? outline;
            if(!_outlineEnabled || !ConfigurationManager.GetCVar(CCVars.OutlineEnabled))
            {
                if(entityToClick != null && _entityManager.TryGetComponent(entityToClick, out outline))
                {
                    outline.OnMouseLeave(); //Prevent outline remains from persisting post command.
                }
                return;
            }

            if (entityToClick == _lastHoveredEntity)
            {
                if (entityToClick != null && _entityManager.TryGetComponent(entityToClick, out outline))
                {
                    outline.UpdateInRange(inRange, renderScale);
                }

                return;
            }

            if (_lastHoveredEntity != null && !_entityManager.Deleted(_lastHoveredEntity) &&
                _entityManager.TryGetComponent(_lastHoveredEntity, out outline))
            {
                outline.OnMouseLeave();
            }

            _lastHoveredEntity = entityToClick;

            if (_lastHoveredEntity != null && _entityManager.TryGetComponent(_lastHoveredEntity, out outline))
            {
                outline.OnMouseEnter(inRange, renderScale);
            }
        }

        public EntityUid? GetEntityUnderPosition(MapCoordinates coordinates)
        {
            var entitiesUnderPosition = GetEntitiesUnderPosition(coordinates);
            return entitiesUnderPosition.Count > 0 ? entitiesUnderPosition[0] : null;
        }

        public IList<EntityUid> GetEntitiesUnderPosition(EntityCoordinates coordinates)
        {
            return GetEntitiesUnderPosition(coordinates.ToMap(EntityManager));
        }

        public IList<EntityUid> GetEntitiesUnderPosition(MapCoordinates coordinates)
        {
            // Find all the entities intersecting our click
            var entities = IoCManager.Resolve<IEntityLookup>().GetEntitiesIntersecting(coordinates.MapId,
                Box2.CenteredAround(coordinates.Position, (1, 1)));

            // Check the entities against whether or not we can click them
            var foundEntities = new List<(EntityUid clicked, int drawDepth, uint renderOrder)>();
            foreach (var entity in entities)
            {
                if (_entityManager.TryGetComponent<ClickableComponent?>(entity, out var component)
                    && !entity.IsInContainer()
                    && component.CheckClick(coordinates.Position, out var drawDepthClicked, out var renderOrder))
                {
                    foundEntities.Add((entity, drawDepthClicked, renderOrder));
                }
            }

            if (foundEntities.Count == 0)
                return Array.Empty<EntityUid>();

            foundEntities.Sort(new ClickableEntityComparer(_entityManager));
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
        public static IList<EntityUid> GetEntitiesUnderPosition(IStateManager stateManager, EntityCoordinates coordinates)
        {
            if (stateManager.CurrentState is GameScreenBase gameScreenBase)
            {
                return gameScreenBase.GetEntitiesUnderPosition(coordinates);
            }

            return ImmutableList<EntityUid>.Empty;
        }

        internal class ClickableEntityComparer : IComparer<(EntityUid clicked, int depth, uint renderOrder)>
        {
            private readonly IEntityManager _entities;

            public ClickableEntityComparer(IEntityManager entities)
            {
                _entities = entities;
            }

            public int Compare((EntityUid clicked, int depth, uint renderOrder) x,
                (EntityUid clicked, int depth, uint renderOrder) y)
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

                var transX = _entities.GetComponent<TransformComponent>(x.clicked);
                var transY = _entities.GetComponent<TransformComponent>(y.clicked);
                val = transX.Coordinates.Y.CompareTo(transY.Coordinates.Y);
                if (val != 0)
                {
                    return val;
                }

                return x.clicked.CompareTo(y.clicked);
            }
        }

        /// <summary>
        ///     Converts a state change event from outside the simulation to inside the simulation.
        /// </summary>
        /// <param name="args">Event data values for a bound key state change.</param>
        protected virtual void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
        {
            // If there is no InputSystem, then there is nothing to forward to, and nothing to do here.
            if(!EntitySystemManager.TryGetEntitySystem(out InputSystem? inputSys))
                return;

            var kArgs = args.KeyEventArgs;
            var func = kArgs.Function;
            var funcId = InputManager.NetworkBindMap.KeyFunctionID(func);

            EntityCoordinates coordinates = default;
            EntityUid? entityToClick = null;
            if (args.Viewport is IViewportControl vp)
            {
                var mousePosWorld = vp.ScreenToMap(kArgs.PointerLocation.Position);
                entityToClick = GetEntityUnderPosition(mousePosWorld);

                coordinates = MapManager.TryFindGridAt(mousePosWorld, out var grid) ? grid.MapToGrid(mousePosWorld) :
                    EntityCoordinates.FromMap(MapManager, mousePosWorld);
            }

            var message = new FullInputCmdMessage(Timing.CurTick, Timing.TickFraction, funcId, kArgs.State,
                coordinates , kArgs.PointerLocation,
                entityToClick ?? default); // TODO make entityUid nullable

            // client side command handlers will always be sent the local player session.
            var session = PlayerManager.LocalPlayer?.Session;
            if (inputSys.HandleInputCommand(session, func, message))
            {
                kArgs.Handle();
            }
        }
    }
}
