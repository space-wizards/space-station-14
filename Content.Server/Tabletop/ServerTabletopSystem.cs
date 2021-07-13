using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public class ServerTabletopSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private IEntity _tabletopMap => _mapManager.GetMapEntity(_mapManager.CreateMap());

        public override void Initialize()
        {
            SubscribeNetworkEvent<TabletopMoveEvent>(TabletopMoveHandler);
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
    }
}
