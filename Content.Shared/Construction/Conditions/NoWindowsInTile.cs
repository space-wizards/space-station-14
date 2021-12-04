using Content.Shared.Maps;
using Content.Shared.Window;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class NoWindowsInTile : IConstructionCondition
    {
        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            foreach (var entity in location.GetEntitiesInTile(LookupFlags.Approximate | LookupFlags.IncludeAnchored))
            {
                if (IoCManager.Resolve<IEntityManager>().HasComponent<SharedWindowComponent>(entity))
                    return false;
            }

            return true;
        }

        public ConstructionGuideEntry? GenerateGuideEntry()
        {
            return new ConstructionGuideEntry()
            {
                Localization = "construction-step-condition-no-windows-in-tile"
            };
        }
    }
}
