#nullable enable
using Content.Shared.GameObjects.Components;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction.ConstructionConditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class NoWindowsInTile : IConstructionCondition
    {
        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            foreach (var entity in location.GetEntitiesInTile(true))
            {
                if (entity.HasComponent<SharedWindowComponent>())
                    return false;
            }

            return true;
        }
    }
}
