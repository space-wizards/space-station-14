using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Utility
{
    public static class GridTileLookupHelpers
    {
        /// <summary>
        ///     Helper that returns all entities in a turf very fast.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IEntity> GetEntitiesInTileFast(this TileRef turf)
        {
            var gridTileLookup = EntitySystem.Get<GridTileLookupSystem>();

            return gridTileLookup.GetEntitiesIntersecting(turf.GridIndex, turf.GridIndices);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IEntity> GetEntitiesInTileFast(this MapIndices indices, GridId gridId)
        {
            var turf = indices.GetTileRef(gridId);

            if (turf == null)
                return Enumerable.Empty<IEntity>();

            return GetEntitiesInTileFast(turf.Value);
        }
    }
}
