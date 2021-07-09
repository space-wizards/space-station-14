using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop
{
    [Serializable, NetSerializable]
    public class TabletopMoveEvent : EntityEventArgs
    {
        public EntityUid MovedEntity { get; }
        public GridId TargetGrid { get; }
        public Vector2 TargetPosition { get; }

        public TabletopMoveEvent(EntityUid movedEntity, GridId targetGrid, Vector2 targetPosition)
        {
            TargetGrid = targetGrid;
            TargetPosition = targetPosition;
            MovedEntity = movedEntity;
        }
    }
}
