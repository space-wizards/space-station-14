using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        public static IEnumerable<IEntity> GetEntitiesInTileFast(this TileRef turf, QuerySystem? query = null)
        {
            query ??= EntitySystem.Get<QuerySystem>();

            return query.GetEntitiesIntersecting(turf.GridIndex, turf.GridIndices);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<IEntity> GetEntitiesInTileFast(this Vector2i indices, GridId gridId, QuerySystem? query = null)
        {
            query ??= EntitySystem.Get<QuerySystem>();
            return query.GetEntitiesIntersecting(gridId, indices);
        }
    }
}
