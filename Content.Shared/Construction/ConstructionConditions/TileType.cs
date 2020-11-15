using System;
using Content.Shared.GameObjects.Components;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction.ConstructionConditions
{
    [UsedImplicitly]
    public class TileType : IConstructionCondition
    {

        public string TargetTile;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.TargetTile, "target", "lattice");
        }

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            var tileFound = location.GetTileRef();
            if (tileFound == null)
                return false;
            var tile = TurfHelpers.GetContentTileDefinition(tileFound.Value.Tile);
            if (tile.Name == TargetTile)
                return true;
            return false;
        }
    }
}
