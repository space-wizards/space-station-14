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
    public class TabletopSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private MapId _tabletopMapId;

        public override void Initialize()
        {
            SubscribeNetworkEvent<TabletopMoveEvent>(TabletopMoveHandler);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Cleanup);
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

        public IEntity CreateCamera(TabletopGameComponent component, IPlayerSession playerSession)
        {
            var entityManager = component.Owner.EntityManager;
            var viewSubscriberSystem = Get<ViewSubscriberSystem>();

            var viewCoordinates = new MapCoordinates((0, 0), EnsureMapCreated());
            var camera = entityManager.SpawnEntity(null, viewCoordinates);

            var eyeComponent = camera.EnsureComponent<EyeComponent>();
            eyeComponent.DrawFov = false;
            viewSubscriberSystem.AddViewSubscriber(camera.Uid, playerSession);

            entityManager.SpawnEntity("Crowbar", viewCoordinates);

            return camera;
        }

        private void Cleanup(RoundRestartCleanupEvent args)
        {

        }

        private MapId EnsureMapCreated()
        {
            if (_tabletopMapId.Equals(MapId.Nullspace))
            {
                _tabletopMapId = _mapManager.CreateMap();
            }

            return _tabletopMapId;
        }
    }
}
