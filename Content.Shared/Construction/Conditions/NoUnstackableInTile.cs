using Content.Shared.Maps;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class NoUnstackableInTile : IConstructionCondition
    {
        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var tagSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<TagSystem>();

            foreach (var entity in location.GetEntitiesInTile(LookupFlags.Approximate | LookupFlags.Static | LookupFlags.Sundries))
            {
                if (tagSystem.HasTag(entity, "Unstackable"))
                    return false;
            }

            return true;
        }

        public ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-step-condition-no-unstackable-in-tile"
            };
        }
    }
}
