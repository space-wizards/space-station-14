using Content.Client.Clickable;
using Content.Client.UserInterface;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using YamlDotNet.Serialization.TypeInspectors;

namespace Content.Client.Gameplay
{
    // OH GOD.
    // Ok actually it's fine.
    // Instantiated dynamically through the StateManager, Dependencies will be resolved.
    [Virtual]
    public partial class GameplayStateBase : State, IEntityEventSubscriber
    {
        [Dependency] private IEyeManager _eyeManager = default!;
        [Dependency] private IInputManager _inputManager = default!;
        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] protected IUserInterfaceManager UserInterfaceManager = default!;
        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IViewVariablesManager _vvm = default!;
        [Dependency] private IConsoleHost _conHost = default!;
        [Dependency] private IConfigurationManager _configurationManager = default!;

        private (ViewVariablesPath? path, string[] segments) ResolveVvHoverObject(string path)
        {
            var segments = path.Split('/');
            var uid = RecursivelyFindUiEntity(UserInterfaceManager.CurrentlyHovered);
            var netUid = _entityManager.GetNetEntity(uid);
            return (netUid != null ? new ViewVariablesInstancePath(netUid) : null, segments);
        }

        private EntityUid? RecursivelyFindUiEntity(Control? control)
        {
            if (control == null)
                return null;

            switch (control)
            {
                case IViewportControl vp:
                    if (_inputManager.MouseScreenPosition.IsValid)
                        return GetClickedEntity(vp.PixelToMap(_inputManager.MouseScreenPosition.Position));
                    return null;
                case SpriteView sprite:
                    return sprite.Entity;
                case IEntityControl ui:
                    return ui.UiEntity;
            }

            return RecursivelyFindUiEntity(control.Parent);
        }

        private IEnumerable<string>? ListVVHoverPaths(string[] segments)
        {
            return null;
        }

        protected override void Startup()
        {
            _vvm.RegisterDomain("enthover", ResolveVvHoverObject, ListVVHoverPaths);
            _inputManager.KeyBindStateChanged += OnKeyBindStateChanged;
            CommandBinds.Builder
                .Bind(ContentKeyFunctions.InspectEntity, new PointerInputCmdHandler(HandleInspect, outsidePrediction: true))
                .Bind(ContentKeyFunctions.InspectServerComponent, new PointerInputCmdHandler(HandleInspectServerComponent, outsidePrediction: true))
                .Bind(ContentKeyFunctions.InspectClientComponent, new PointerInputCmdHandler(HandleInspectClientComponent, outsidePrediction: true))
                .Register<GameplayStateBase>();
        }

        protected override void Shutdown()
        {
            _vvm.UnregisterDomain("enthover");
            _inputManager.KeyBindStateChanged -= OnKeyBindStateChanged;
            CommandBinds.Unregister<GameplayStateBase>();
        }

        private bool HandleInspect(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            _conHost.ExecuteCommand($"vv /c/enthover");
            return true;
        }

        private bool HandleInspectServerComponent(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            var component = _configurationManager.GetCVar(CCVars.DebugQuickInspect);
            if (_entityManager.TryGetNetEntity(uid, out var net))
                _conHost.ExecuteCommand($"vv /entity/{net.Value.Id}/{component}");
            return true;
        }

        private bool HandleInspectClientComponent(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            var component = _configurationManager.GetCVar(CCVars.DebugQuickInspect);
            _conHost.ExecuteCommand($"vv /c/entity/{uid}/{component}");
            return true;
        }

        public EntityUid? GetClickedEntity(MapCoordinates coordinates)
        {
            return GetClickedEntity(coordinates, _eyeManager.CurrentEye);
        }

        public EntityUid? GetClickedEntity(MapCoordinates coordinates, IEye? eye)
        {
            return _entitySystemManager.GetEntitySystem<ClickableSystem>().GetClickedEntity(coordinates, eye);
        }

        public IReadOnlyList<EntityUid> GetClickableEntities(EntityCoordinates coordinates, bool excludeFaded = true)
        {
            var transformSystem = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();
            return GetClickableEntities(transformSystem.ToMapCoordinates(coordinates), excludeFaded);
        }

        public IReadOnlyList<EntityUid> GetClickableEntities(MapCoordinates coordinates, bool excludeFaded = true)
        {
            return GetClickableEntities(coordinates, _eyeManager.CurrentEye, excludeFaded);
        }

        public IReadOnlyList<EntityUid> GetClickableEntities(MapCoordinates coordinates, IEye? eye, bool excludeFaded = true)
        {
            return _entitySystemManager.GetEntitySystem<ClickableSystem>().GetClickableEntities(coordinates, eye, excludeFaded);
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
            if (args.Viewport is IViewportControl vp && kArgs.PointerLocation.IsValid)
            {
                var mousePosWorld = vp.PixelToMap(kArgs.PointerLocation.Position);

                if (vp is ScalingViewport svp)
                {
                    entityToClick = GetClickedEntity(mousePosWorld, svp.Eye);
                }
                else
                {
                    entityToClick = GetClickedEntity(mousePosWorld);
                }
                var transformSystem = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();
                var mapSystem = _entitySystemManager.GetEntitySystem<MapSystem>();

                coordinates = mapSystem.TryFindGridAt(mousePosWorld, out var uid, out _) ?
                    mapSystem.MapToGrid(uid, mousePosWorld) :
                    transformSystem.ToCoordinates(mousePosWorld);
            }
            else
            {
                coordinates = EntityCoordinates.Invalid;
            }

            var message = new ClientFullInputCmdMessage(_timing.CurTick, _timing.TickFraction, funcId)
            {
                State = kArgs.State,
                Coordinates = coordinates,
                ScreenCoordinates = kArgs.PointerLocation,
                Uid = entityToClick ?? default,
            }; // TODO make entityUid nullable

            // client side command handlers will always be sent the local player session.
            var session = _playerManager.LocalSession;
            if (inputSys.HandleInputCommand(session, func, message))
            {
                kArgs.Handle();
            }
        }
    }
}
