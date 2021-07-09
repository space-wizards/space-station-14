using Content.Shared.Tabletop;
using Robust.Shared.GameObjects;

namespace Content.Server.Tabletop
{
    public class TabletopDragDropSystem : EntitySystem
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
