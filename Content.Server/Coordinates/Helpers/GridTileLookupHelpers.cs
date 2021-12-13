using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Coordinates.Helpers
{
    public static class GridTileLookupHelpers
    {
        /// <summary>
        ///     Helper that returns all entities in a turf very fast.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<EntityUid> GetEntitiesInTileFast(this TileRef turf, GridTileLookupSystem? gridTileLookup = null)
        {
            gridTileLookup ??= EntitySystem.Get<GridTileLookupSystem>();

            return gridTileLookup.GetEntitiesIntersecting(turf.GridIndex, turf.GridIndices);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<EntityUid> GetEntitiesInTileFast(this Vector2i indices, GridId gridId, GridTileLookupSystem? gridTileLookup = null)
        {
            gridTileLookup ??= EntitySystem.Get<GridTileLookupSystem>();
            return gridTileLookup.GetEntitiesIntersecting(gridId, indices);
        }
    }
}
