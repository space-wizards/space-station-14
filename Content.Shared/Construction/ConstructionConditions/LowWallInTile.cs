using System;
using Content.Shared.GameObjects.Components;
using Content.Shared.Maps;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction.ConstructionConditions
{
    [Serializable, NetSerializable]
    public class LowWallInTile : IConstructionCondition
    {
        public void ExposeData(ObjectSerializer serializer) { }

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            foreach (var entity in location.GetEntitiesInTile())
            {
                if (entity.HasComponent<SharedCanBuildWindowOnTopComponent>())
                    return true;
            }

            return false;
        }
    }
}
