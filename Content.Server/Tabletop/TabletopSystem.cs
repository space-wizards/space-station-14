using System.Collections.Generic;
using Content.Server.Tabletop.Components;
using Content.Shared.GameTicking;
using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public partial class TabletopSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly Dictionary<EntityUid, MapId> _gameSessions = new();

        public override void Initialize()
        {
            SubscribeNetworkEvent<TabletopMoveEvent>(OnTabletopMove);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        }

        /**
         * <summary>
         * For a specific user, create a table if it does not exist yet and let the user open a UI window to play it.
         * </summary>
         * <param name="user">The user entity for which to open the window.</param>
         * <param name="table">The entity with which the tabletop game session will be associated.</param>
         */
        public void OpenTable(IEntity user, IEntity table)
        {
            // Make sure we have a table, and get its map ID
            EnsureTable(table.Uid);
            var mapId = GetMapId(table.Uid);

            // Create a camera for the user to use
            // TODO: set correct coordinates, depending on the piece the game was started from
            IEntity camera = CreateCamera(user, new MapCoordinates(0, 0, mapId));

            // Tell the client that it has to open a viewport for the tabletop game
            var playerSession = user.PlayerSession();
            if (playerSession != null)
            {
                // Send a message to the client to open a chess UI window
                // TODO: use actual title/size from prototype, for now we assume its chess
                _entityManager.EntityNetManager?.SendSystemNetworkMessage(
                    new TabletopPlayEvent(table.Uid, camera.Uid, "Chess", (274 + 64, 274)), playerSession.ConnectedClient
                );
            }
        }

        /**
         * <summary>Create a map related to this entity UID, if it does not already exist.</summary>
         * <param name="uid">The entity UID to ensure a table for.</param>
         */
        private void EnsureTable(EntityUid uid)
        {
            // We already have a table, return
            // TODO: if tables are connected, treat them as a single entity
            if (_gameSessions.ContainsKey(uid)) return;

            // Map does not exist for this entity yet, create it and store it
            var mapId = _mapManager.CreateMap();
            _gameSessions.Add(uid, mapId);

            // Tabletop maps do not need lighting, turn it off
            var mapComponent = _mapManager.GetMapEntity(mapId).GetComponent<IMapComponent>();
            mapComponent.LightingEnabled = false;
            mapComponent.Dirty();

            // TODO: don't assume chess
            SetupChessBoard(GetMapId(uid));
        }

        #region Event handlers

        // Move an entity which is dragged by the user, but check if they are allowed to do so and to these coordinates
        private void OnTabletopMove(TabletopMoveEvent msg)
        {
            if (!EntityManager.TryGetEntity(msg.MovedEntity, out var movedEntity))
            {
                return;
            }

            if (msg.FirstDrag && movedEntity.TryGetComponent<TabletopDraggableComponent>(out var draggableComponent))
            {
                draggableComponent.DraggingPlayer = msg.DraggingPlayer;
            }

            // TODO: some permission system, disallow movement if you're not permitted to move the item

            // Move the entity and dirty it (we use the map ID from the entity so noone can try to be funny and move the item to another map)
            var entityCoordinates = new EntityCoordinates(_mapManager.GetMapEntityId(movedEntity.Transform.MapID), msg.Coordinates.Position);
            movedEntity.Transform.Coordinates = entityCoordinates;
            movedEntity.Dirty();
        }

        // Remove all tabletop sessions and maps when the round restarts
        private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
        {
            Reset();
        }

        #endregion

        #region Utility

        /**
         * <summary>Create a camera entity for a user to control, and add the user to the view subscribers.</summary>
         * <param name="user">The user entity to create this camera for and add to the view subscribers.</param>
         * <param name="coordinates">The map coordinates to spawn this camera at.</param>
         */
        // TODO: this can probably be generalized into a "CctvSystem" or whatever
        private IEntity CreateCamera(IEntity user, MapCoordinates coordinates)
        {
            // Spawn an empty entity at the coordinates
            var camera = _entityManager.SpawnEntity(null, coordinates);

            // Add an eye component and disable FOV
            var eyeComponent = camera.EnsureComponent<EyeComponent>();
            eyeComponent.DrawFov = false;

            // Add the user to the view subscribers. If there is no player session, just skip this step
            if (user.PlayerSession() is { } playerSession)
            {
                Get<ViewSubscriberSystem>().AddViewSubscriber(camera.Uid, playerSession);
            }

            return camera;
        }

        /**
         * <summary>Get the <see cref="MapId"/> related to this entity UID.</summary>
         * <param name="uid">The identifier of the entity to get the map for.</param>
         * <returns>The <see cref="MapId"/> that has been reserved for this entity.</returns>
         */
        private MapId GetMapId(EntityUid uid)
        {
            if (_gameSessions.ContainsKey(uid))
            {
                return _gameSessions[uid];
            }

            throw new KeyNotFoundException("The table for the requested entity has not been initialized yet.");
        }

        /**
         * <summary>Remove all tabletop sessions and their maps.</summary>
         */
        private void Reset()
        {
            foreach (var (_, mapId) in _gameSessions)
            {
                _mapManager.DeleteMap(mapId);
            }

            _gameSessions.Clear();
        }

        #endregion
    }
}
