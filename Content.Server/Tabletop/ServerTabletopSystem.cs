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

            movedEntity.Transform.Coordinates = msg.Coordinates;
            movedEntity.Dirty();
        }
    }
}
