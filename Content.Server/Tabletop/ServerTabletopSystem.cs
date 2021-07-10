using Content.Shared.Tabletop.Events;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public class ServerTabletopSystem : EntitySystem
    {
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

            // TODO: sanity checks; only allow moving the entity to within the tabletop space

            // Move the entity and dirty it
            movedEntity.Transform.Coordinates = msg.Coordinates;
            movedEntity.Dirty();
        }
    }
}
