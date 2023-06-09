using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class TileNotBlocked : IConstructionCondition
    {
        [DataField("filterMobs")] private bool _filterMobs = false;
        [DataField("failIfSpace")] private bool _failIfSpace = true;

        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var tileRef = location.GetTileRef();

            if (tileRef == null || tileRef.Value.IsSpace())
                return !_failIfSpace;

            return !tileRef.Value.IsBlockedTurf(_filterMobs);
        }

        public ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-step-condition-tile-not-blocked",
            };
        }
    }
}
