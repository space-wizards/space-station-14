using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;

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
            var entityManager = IoCManager.Resolve<IEntityManager>();

            foreach (var entity in lookup.GetEntitiesIntersecting(location, LookupFlags.Approximate | LookupFlags.Static |
                                                                            LookupFlags.Sundries))
            {
                if (tagSystem.HasTag(entity, "Unstackable"))
                {
                    // Only test against anchored unstackables.
                    if (entityManager.TryGetComponent<TransformComponent>(entity, out var transform) && !transform.Anchored)
                        continue;

                    return true;
                }
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
