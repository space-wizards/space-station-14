using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Clickable;
using Content.Client.ContextMenu.UI;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Gameplay
{
    // OH GOD.
    // Ok actually it's fine.
    // Instantiated dynamically through the StateManager, Dependencies will be resolved.
    [Virtual]
    public class GameplayStateBase : State, IEntityEventSubscriber
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] protected readonly IUserInterfaceManager UserInterfaceManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IViewVariablesManager _vvm = default!;

        private ClickableEntityComparer _comparer = default!;

        private (ViewVariablesPath? path, string[] segments) ResolveVVHoverObject(string path)
        {
            // VVs the currently hovered entity. For a nifty vv keybinding you can use:
            //
            // /bind v command "vv /c/enthover"
            // /svbind
            //
            // Though you probably want to include a modifier like alt, as otherwise this would open VV even when typing
            // a message into chat containing the letter v.

            var segments = path.Split('/');

            EntityUid? uid = null;
            if (UserInterfaceManager.CurrentlyHovered is IViewportControl vp && _inputManager.MouseScreenPosition.IsValid)
                uid = GetEntityUnderPosition(vp.ScreenToMap(_inputManager.MouseScreenPosition.Position));
            else if (UserInterfaceManager.CurrentlyHovered is EntityMenuElement element)
                uid = element.Entity;

            return (uid != null ? new ViewVariablesInstancePath(uid) : null, segments);
        }

        private IEnumerable<string>? ListVVHoverPaths(string[] segments)
        {
            return null;
        }

        protected override void Startup()
        {
            _vvm.RegisterDomain("enthover", ResolveVVHoverObject, ListVVHoverPaths);
            _inputManager.KeyBindStateChanged += OnKeyBindStateChanged;
            _comparer = new ClickableEntityComparer();
        }

        protected override void Shutdown()
        {
            _vvm.UnregisterDomain("enthover");
            _inputManager.KeyBindStateChanged -= OnKeyBindStateChanged;
        }

        public EntityUid? GetEntityUnderPosition(MapCoordinates coordinates)
        {
            var entitiesUnderPosition = GetEntitiesUnderPosition(coordinates);
            return entitiesUnderPosition.Count > 0 ? entitiesUnderPosition[0] : null;
        }

        public IList<EntityUid> GetEntitiesUnderPosition(EntityCoordinates coordinates)
        {
            return GetEntitiesUnderPosition(coordinates.ToMap(_entityManager));
        }

        public IList<EntityUid> GetEntitiesUnderPosition(MapCoordinates coordinates)
        {
            // Find all the entities intersecting our click
            var entities = _entityManager.EntitySysManager.GetEntitySystem<EntityLookupSystem>().GetEntitiesIntersecting(coordinates.MapId,
                Box2.CenteredAround(coordinates.Position, (1, 1)), LookupFlags.Uncontained | LookupFlags.Approximate);

            // Check the entities against whether or not we can click them
            var foundEntities = new List<(EntityUid clicked, int drawDepth, uint renderOrder, float bottom)>();
            var clickQuery = _entityManager.GetEntityQuery<ClickableComponent>();
            var metaQuery = _entityManager.GetEntityQuery<MetaDataComponent>();
            var spriteQuery = _entityManager.GetEntityQuery<SpriteComponent>();
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            // TODO: Smelly
            var eye = _eyeManager.CurrentEye;

            foreach (var entity in entities)
            {
                if (clickQuery.TryGetComponent(entity, out var component) &&
                    spriteQuery.TryGetComponent(entity, out var sprite) &&
                    component.CheckClick(sprite, xformQuery, coordinates.Position, eye,  out var drawDepthClicked, out var renderOrder, out var bottom))
                {
                    foundEntities.Add((entity, drawDepthClicked, renderOrder, bottom));
                }
            }

            if (foundEntities.Count == 0)
                return Array.Empty<EntityUid>();

            foundEntities.Sort(_comparer);
            // 0 is the top element.
            return foundEntities.Select(a => a.clicked).ToList();
        }

        private sealed class ClickableEntityComparer : IComparer<(EntityUid clicked, int depth, uint renderOrder, float bottom)>
        {
            public int Compare((EntityUid clicked, int depth, uint renderOrder, float bottom) x,
                (EntityUid clicked, int depth, uint renderOrder, float bottom) y)
            {
                var cmp = y.depth.CompareTo(x.depth);
                if (cmp != 0)
                {
                    return cmp;
                }

                cmp = y.renderOrder.CompareTo(x.renderOrder);

                if (cmp != 0)
                {
                    return cmp;
                }

                cmp = y.bottom.CompareTo(x.bottom);

                if (cmp != 0)
                {
                    return cmp;
                }

                return y.clicked.CompareTo(x.clicked);
            }
        }

        /// <summary>
        ///     Converts a state change event from outside the simulation to inside the simulation.
        /// </summary>
        /// <param name="args">Event data values for a bound key state change.</param>
        protected virtual void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
        {
            // If there is no InputSystem, then there is nothing to forward to, and nothing to do here.
            if(!_entitySystemManager.TryGetEntitySystem(out InputSystem? inputSys))
                return;

            var kArgs = args.KeyEventArgs;
            var func = kArgs.Function;
            var funcId = _inputManager.NetworkBindMap.KeyFunctionID(func);

            EntityCoordinates coordinates = default;
            EntityUid? entityToClick = null;
            if (args.Viewport is IViewportControl vp)
            {
                var mousePosWorld = vp.ScreenToMap(kArgs.PointerLocation.Position);
                entityToClick = GetEntityUnderPosition(mousePosWorld);

                coordinates = _mapManager.TryFindGridAt(mousePosWorld, out var grid) ? grid.MapToGrid(mousePosWorld) :
                    EntityCoordinates.FromMap(_mapManager, mousePosWorld);
            }

            var message = new FullInputCmdMessage(_timing.CurTick, _timing.TickFraction, funcId, kArgs.State,
                coordinates , kArgs.PointerLocation,
                entityToClick ?? default); // TODO make entityUid nullable

            // client side command handlers will always be sent the local player session.
            var session = _playerManager.LocalPlayer?.Session;
            if (inputSys.HandleInputCommand(session, func, message))
            {
                kArgs.Handle();
            }
        }
    }
}
