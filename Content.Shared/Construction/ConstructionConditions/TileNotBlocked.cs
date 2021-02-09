using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction.ConstructionConditions
{
    [UsedImplicitly]
    public class TileNotBlocked : IConstructionCondition
    {
        private bool _filterMobs = false;
        private bool _failIfSpace = true;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _filterMobs, "filterMobs", false);
            serializer.DataField(ref _failIfSpace, "failIfSpace", true);
        }

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            var tileRef = location.GetTileRef();

            if (tileRef == null || tileRef.Value.IsSpace())
                return !_failIfSpace;

            return !tileRef.Value.IsBlockedTurf(_filterMobs);
        }
    }
}
