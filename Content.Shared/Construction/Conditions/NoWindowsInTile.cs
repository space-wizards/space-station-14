using Content.Shared.Maps;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class NoWindowsInTile : IConstructionCondition
    {
        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var gridUid = location.GetGridUid(entManager);

            if (!entManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
                return true;

            var tagQuery = entManager.GetEntityQuery<TagComponent>();
            var sysMan = entManager.EntitySysManager;
            var tagSystem = sysMan.GetEntitySystem<TagSystem>();
            var lookup = sysMan.GetEntitySystem<EntityLookupSystem>();

            foreach (var entity in lookup.GetEntitiesIntersecting(gridUid.Value, grid.LocalToTile(location)))
            {
                if (tagSystem.HasTag(entity, "Window", tagQuery))
                    return false;
            }

            return true;
        }

        public ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-step-condition-no-windows-in-tile"
            };
        }
    }
}
