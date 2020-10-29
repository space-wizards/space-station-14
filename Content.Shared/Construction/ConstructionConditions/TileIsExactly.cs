using System;
using Content.Shared.GameObjects.Components;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Map;

namespace Content.Shared.Construction.ConstructionConditions
{
    [UsedImplicitly]
    public class TileIsExactly : IConstructionCondition
    {
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.TileName, "tile", string.Empty);
        }

        /// <summary>
        ///     The tile ID.
        /// </summary>
        public string TileName { get; private set; }

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            var tileRef = location.GetTileRef();
            var tileIDNum = (tileRef?.Tile ?? Tile.Empty).TypeId;
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();
            return tileIDNum == tileDefinitionManager[TileName].TileId;
        }
    }
}
