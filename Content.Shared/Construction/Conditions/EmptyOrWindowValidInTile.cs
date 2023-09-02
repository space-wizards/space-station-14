using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class EmptyOrWindowValidInTile : IConstructionCondition
    {
        [DataField("tileNotBlocked")]
        private TileNotBlocked _tileNotBlocked = new();

        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var result = false;

            foreach (var entity in location.GetEntitiesInTile(LookupFlags.Approximate | LookupFlags.Static))
            {
                if (IoCManager.Resolve<IEntityManager>().HasComponent<SharedCanBuildWindowOnTopComponent>(entity))
                    result = true;
            }

            if (!result)
                result = _tileNotBlocked.Condition(user, location, direction);

            return result;
        }

        public ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-guide-condition-empty-or-window-valid-in-tile"
            };
        }
    }
}
