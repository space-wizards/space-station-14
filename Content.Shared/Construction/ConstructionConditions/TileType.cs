using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Shared.Construction.ConstructionConditions
{
    [UsedImplicitly]
    public class TileType : IConstructionCondition
    {

        public List<string> TargetTiles { get; private set; }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.TargetTiles, "targets", null);
        }

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            if (TargetTiles == null) return true;

            var tileFound = location.GetTileRef();

            if (tileFound == null)
                return false;

            var tile = tileFound.Value.Tile.GetContentTileDefinition();
            foreach (var targetTile in TargetTiles)
            {
                if (tile.Name == targetTile) {
                    return true;
                }
            }
            return false;
        }
    }
}
