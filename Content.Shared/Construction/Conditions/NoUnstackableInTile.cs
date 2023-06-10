using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class NoUnstackableInTile : IConstructionCondition
    {
        public const string GuidebookString = "construction-step-condition-no-unstackable-in-tile";
        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var tagSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<TagSystem>();

            if (AnyUnstackableTiles(location, tagSystem))
                return false;

            return true;
        }

        public static bool AnyUnstackableTiles(EntityCoordinates location, TagSystem tagSystem)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var gridUid = location.GetGridUid(entityManager);
            if (gridUid == null)
                return false;

            if (!entityManager.TryGetComponent<MapGridComponent>((EntityUid)gridUid, out var grid))
                return false;

            foreach (var entity in grid.GetAnchoredEntities(location))
            {
                if (tagSystem.HasTag(entity, "Unstackable"))
                {
                    return true;
                }
            }

            return false;
        }

        public ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = GuidebookString
            };
        }
    }
}
