using Content.Shared.Maps;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class NoWindowsInTile : IConstructionCondition
    {
        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            var tagSystem = sysMan.GetEntitySystem<TagSystem>();
            var lookup = sysMan.GetEntitySystem<EntityLookupSystem>();

            foreach (var entity in lookup.GetEntitiesIntersecting(location, LookupFlags.Approximate | LookupFlags.Static))
            {
                if (tagSystem.HasTag(entity, "Window"))
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
