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
        [DataField("failIfNotSturdy")] private bool _failIfNotSturdy = true;

        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var tileRef = location.GetTileRef();

            if (tileRef == null)
                return false;

            if (_failIfNotSturdy && !tileRef.Value.GetContentTileDefinition().Sturdy)
                return false;

            if (tileRef.Value.IsBlockedTurf(_filterMobs))
                return false;

            return true;
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
