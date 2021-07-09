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
        public EntityCoordinates Coordinates { get; }

        public TabletopMoveEvent(EntityUid movedEntity, EntityCoordinates coordinates)
        {
            MovedEntity = movedEntity;
            Coordinates = coordinates;
        }
    }
}
