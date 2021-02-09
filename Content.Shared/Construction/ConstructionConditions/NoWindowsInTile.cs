using System;
using Content.Shared.GameObjects.Components;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction.ConstructionConditions
{
    [UsedImplicitly]
    public class NoWindowsInTile : IConstructionCondition
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) { }

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
