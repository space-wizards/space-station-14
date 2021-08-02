using System.Collections.Generic;
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
            SubscribeNetworkEvent<TabletopMoveEvent>(TabletopMoveHandler);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        }

        private void TabletopMoveHandler(TabletopMoveEvent msg)
        {
            if (!EntityManager.TryGetEntity(msg.MovedEntity, out var movedEntity))
            {
                return;
            }

            // TODO: sanity checks; only allow moving the entity to within the tabletop space, is the user actually allowed to move this entity?

            // Move the entity and dirty it
            movedEntity.Transform.Coordinates = msg.Coordinates;
            movedEntity.Dirty();
        }

        private IEntity CreateCamera(IEntity user, MapCoordinates coordinates)
        {
            var camera = _entityManager.SpawnEntity(null, coordinates);
            camera.Name = "camera";

            var eyeComponent = camera.EnsureComponent<EyeComponent>();
            eyeComponent.DrawFov = false;

            var playerSession = user.PlayerSession();
            if (playerSession != null)
            {
                Get<ViewSubscriberSystem>().AddViewSubscriber(camera.Uid, playerSession);
            }

            return camera;
        }

        private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
        {
            Reset();
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

        public void OpenTable(IEntity user, IEntity table)
        {
            EnsureTable(table.Uid);
            var mapId = GetMapId(table.Uid);

            // Create a camera for the user
            IEntity camera = CreateCamera(user, new MapCoordinates(0, 0, mapId));

            // Tell the client that it has to open a viewport for the tabletop game
            // TODO: use actual title/size from prototype, for now we assume its chess
            var playerSession = user.PlayerSession();
            if (playerSession != null)
            {
                _entityManager.EntityNetManager?.SendSystemNetworkMessage(
                    new TabletopPlayEvent(camera.Uid, "Chess", (8, 8)), playerSession.ConnectedClient
                );
            }
        }

        private void EnsureTable(EntityUid uid)
        {
            if (_gameSessions.ContainsKey(uid)) return;

            // Map does not exist for this entity yet, create it, store it and return it
            var mapId = _mapManager.CreateMap();
            _gameSessions.Add(uid, mapId);

            // TODO: don't assume chess
            SetupChessBoard(GetMapId(uid));
        }
    }
}
