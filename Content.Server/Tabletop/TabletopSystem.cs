using System.Collections.Generic;
using Content.Server.Tabletop.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public partial class TabletopSystem : SharedTabletopSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;

        /// <summary>
        /// All tabletop games currently in progress. Sessions are associated with an entity UID, which acts as a
        /// key, such that an entity can only have one running tabletop game session.
        /// </summary>
        private readonly Dictionary<EntityUid, TabletopSession> _gameSessions = new();

        public override void Initialize()
        {
            SubscribeNetworkEvent<TabletopMoveEvent>(OnTabletopMove);
            SubscribeNetworkEvent<TabletopDraggingPlayerChangedEvent>(OnDraggingPlayerChanged);
            SubscribeNetworkEvent<TabletopStopPlayingEvent>(OnStopPlaying);
            SubscribeLocalEvent<TabletopGameComponent, ActivateInWorldEvent>(OnTabletopActivate);
            SubscribeLocalEvent<TabletopGameComponent, ComponentShutdown>(OnGameShutdown);
            SubscribeLocalEvent<TabletopDraggableComponent, ComponentGetState>(GetCompState);
        }

        private void OnTabletopActivate(EntityUid uid, TabletopGameComponent component, ActivateInWorldEvent args)
        {
            if(_actionBlockerSystem.CanInteract(args.User))
                OpenTable(args.User, args.Target);
        }

        private void GetCompState(EntityUid uid, TabletopDraggableComponent component, ref ComponentGetState args)
        {
            args.State = new TabletopDraggableComponentState(component.DraggingPlayer);
        }

        /// <summary>
        /// For a specific user, create a table if it does not exist yet and let the user open a UI window to play it.
        /// </summary>
        /// <param name="user">The user entity for which to open the window.</param>
        /// <param name="table">The entity with which the tabletop game session will be associated.</param>
        public void OpenTable(IEntity user, IEntity table)
        {
            if (user.PlayerSession() is not { } playerSession
                || !table.TryGetComponent(out TabletopGameComponent? tabletop)) return;

            // Make sure we have a session, and add the player to it
            var session = EnsureSession(table.Uid, tabletop);
            session.StartPlaying(playerSession);

            // Create a camera for the user to use
            // TODO: set correct coordinates, depending on the piece the game was started from
            IEntity camera = CreateCamera(tabletop, user, new MapCoordinates(0, 0, session.MapId));

            // Tell the client to open a viewport for the tabletop game
            RaiseNetworkEvent(new TabletopPlayEvent(table.Uid, camera.Uid, Loc.GetString(tabletop.BoardName), tabletop.Size), playerSession.ConnectedClient);
        }

        /// <summary>
        /// Create a session associated to this entity UID, if it does not already exist, and return it.
        /// </summary>
        /// <param name="uid">The entity UID to ensure a session for.</param>
        /// <returns>The created/stored tabletop game session.</returns>
        private TabletopSession EnsureSession(EntityUid uid, TabletopGameComponent tabletop)
        {
            // We already have a session, return it
            // TODO: if tables are connected, treat them as a single entity
            if (_gameSessions.ContainsKey(uid))
            {
                return _gameSessions[uid];
            }

            // Session does not exist for this entity yet, create a map and create a session
            var mapId = _mapManager.CreateMap();

            // Tabletop maps do not need lighting, turn it off
            var mapComponent = _mapManager.GetMapEntity(mapId).GetComponent<IMapComponent>();
            mapComponent.LightingEnabled = false;
            mapComponent.Dirty();

            _gameSessions.Add(uid, new TabletopSession(mapId));
            var session = _gameSessions[uid];

            // Since this is the first time opening this session, set up the game
            tabletop.Setup.SetupTabletop(session.MapId, EntityManager);

            return session;
        }

        #region Event handlers

        // Move an entity which is dragged by the user, but check if they are allowed to do so and to these coordinates
        private void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession as IPlayerSession is not { AttachedEntity: { } playerEntity } playerSession) return;

            // Check if player is actually playing at this table
            if (!_gameSessions.TryGetValue(msg.TableUid, out var tableUid) ||
                !tableUid.IsPlaying(playerSession)) return;

            // Return if can not see table or stunned/no hands
            if (!EntityManager.TryGetEntity(msg.TableUid, out var table)) return;
            if (!CanSeeTable(playerEntity, table) || StunnedOrNoHands(playerEntity)) return;

            // Check if moved entity exists and has tabletop draggable component
            if (!EntityManager.TryGetEntity(msg.MovedEntityUid, out var movedEntity)) return;
            if (!ComponentManager.HasComponent<TabletopDraggableComponent>(movedEntity.Uid)) return;

            // TODO: some permission system, disallow movement if you're not permitted to move the item

            // Move the entity and dirty it (we use the map ID from the entity so noone can try to be funny and move the item to another map)
            var transform = ComponentManager.GetComponent<ITransformComponent>(movedEntity.Uid);
            var entityCoordinates = new EntityCoordinates(_mapManager.GetMapEntityId(transform.MapID), msg.Coordinates.Position);
            transform.Coordinates = entityCoordinates;
            movedEntity.Dirty();
        }

        private void OnDraggingPlayerChanged(TabletopDraggingPlayerChangedEvent msg)
        {
            var draggedEntity = EntityManager.GetEntity(msg.DraggedEntityUid);

            if (!draggedEntity.TryGetComponent<TabletopDraggableComponent>(out var draggableComponent)) return;

            draggableComponent.DraggingPlayer = msg.DraggingPlayer;

            if (!draggedEntity.TryGetComponent<AppearanceComponent>(out var appearance)) return;

            if (draggableComponent.DraggingPlayer != null)
            {
                appearance.SetData(TabletopItemVisuals.Scale, new Vector2(1.25f, 1.25f));
                appearance.SetData(TabletopItemVisuals.DrawDepth, (int) DrawDepth.Items + 1);
            }
            else
            {
                appearance.SetData(TabletopItemVisuals.Scale, Vector2.One);
                appearance.SetData(TabletopItemVisuals.DrawDepth, (int) DrawDepth.Items);
            }
        }

        private void OnStopPlaying(TabletopStopPlayingEvent msg, EntitySessionEventArgs args)
        {
            if (_gameSessions.ContainsKey(msg.TableUid) && args.SenderSession as IPlayerSession is { } playerSession)
            {
                _gameSessions[msg.TableUid].StopPlaying(playerSession);
            }
        }

        // TODO: needs to be refactored such that the corresponding entity on the table gets removed, instead of the whole map
        private void OnGameShutdown(EntityUid uid, TabletopGameComponent component, ComponentShutdown args)
        {
            if (!_gameSessions.ContainsKey(uid)) return;

            // Delete the map and remove it from the list of sessions
            _mapManager.DeleteMap(_gameSessions[uid].MapId);
            _gameSessions.Remove(uid);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Create a camera entity for a user to control, and add the user to the view subscribers.
        /// </summary>
        /// <param name="tabletop">The tabletop to create the camera for.</param>
        /// <param name="user">The user entity to create this camera for and add to the view subscribers.</param>
        /// <param name="coordinates">The map coordinates to spawn this camera at.</param>
        // TODO: this can probably be generalized into a "CctvSystem" or whatever
        private IEntity CreateCamera(TabletopGameComponent tabletop, IEntity user, MapCoordinates coordinates)
        {
            // Spawn an empty entity at the coordinates
            var camera = EntityManager.SpawnEntity(null, coordinates);

            // Add an eye component and disable FOV
            var eyeComponent = camera.EnsureComponent<EyeComponent>();
            eyeComponent.DrawFov = false;
            eyeComponent.Zoom = tabletop.CameraZoom;

            // Add the user to the view subscribers. If there is no player session, just skip this step
            if (user.PlayerSession() is { } playerSession)
            {
                _viewSubscriberSystem.AddViewSubscriber(camera.Uid, playerSession);
            }

            return camera;
        }

        #endregion
    }
}
