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

            if (AnyUnstackableTiles(location, tagSystem))
                return false;

            return true;
        }

        public static bool AnyUnstackableTiles(EntityCoordinates location, TagSystem tagSystem)
        {
            var lookup = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();

            foreach (var entity in lookup.GetEntitiesIntersecting(location, LookupFlags.Approximate | LookupFlags.Static |
                                                                            LookupFlags.Sundries))
            {
                if (tagSystem.HasTag(entity, "Unstackable"))
                    return true;
            }

            return false;
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
