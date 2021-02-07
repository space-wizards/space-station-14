#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Fluids
{
    public static class SpillExtensions
    {
        /// <summary>
        ///     Spills the specified solution at the entity's location if possible.
        /// </summary>
        /// <param name="entity">
        ///     The entity to use as a location to spill the solution at.
        /// </param>
        /// <param name="solution">Initial solution for the prototype.</param>
        /// <param name="prototype">The prototype to use.</param>
        /// <param name="sound">Play the spill sound.</param>
        /// <returns>The puddle if one was created, null otherwise.</returns>
        public static PuddleComponent? SpillAt(this Solution solution, IEntity entity, string prototype, bool sound = true)
        {
            var coordinates = entity.Transform.Coordinates;
            return solution.SpillAt(coordinates, prototype, sound);
        }

        /// <summary>
        ///     Spills the specified solution at the entity's location if possible.
        /// </summary>
        /// <param name="entity">
        ///     The entity to use as a location to spill the solution at.
        /// </param>
        /// <param name="solution">Initial solution for the prototype.</param>
        /// <param name="prototype">The prototype to use.</param>
        /// <param name="puddle">The puddle if one was created, null otherwise.</param>
        /// <param name="sound">Play the spill sound.</param>
        /// <returns>True if a puddle was created, false otherwise.</returns>
        public static bool TrySpillAt(this Solution solution, IEntity entity, string prototype, [NotNullWhen(true)] out PuddleComponent? puddle, bool sound = true)
        {
            puddle = solution.SpillAt(entity, prototype, sound);
            return puddle != null;
        }

        /// <summary>
        ///     Spills solution at the specified grid coordinates.
        /// </summary>
        /// <param name="solution">Initial solution for the prototype.</param>
        /// <param name="coordinates">The coordinates to spill the solution at.</param>
        /// <param name="prototype">The prototype to use.</param>
        /// <param name="sound">Whether or not to play the spill sound.</param>
        /// <returns>The puddle if one was created, null otherwise.</returns>
        public static PuddleComponent? SpillAt(this Solution solution, EntityCoordinates coordinates, string prototype, bool sound = true)
        {
            if (solution.TotalVolume == 0)
            {
                return null;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();

            if (!mapManager.TryGetGrid(coordinates.GetGridId(entityManager), out var mapGrid))
                return null; // Let's not spill to space.

            // If space return early, let that spill go out into the void
            var tileRef = mapGrid.GetTileRef(coordinates);
            if (tileRef.Tile.IsEmpty)
            {
                return null;
            }

            // Get normalized co-ordinate for spill location and spill it in the centre
            // TODO: Does SnapGrid or something else already do this?
            var spillGridCoords = mapGrid.GridTileToLocal(tileRef.GridIndices);

            var spilt = false;

            foreach (var spillEntity in entityManager.GetEntitiesAt(mapGrid.ParentMapId, spillGridCoords.Position))
            {
                if (!spillEntity.TryGetComponent(out PuddleComponent? puddleComponent))
                {
                    continue;
                }

                if (!puddleComponent.TryAddSolution(solution, sound))
                {
                    continue;
                }

                spilt = true;
                break;
            }

            // Did we add to an existing puddle
            if (spilt)
            {
                return null;
            }

            var puddle = serverEntityManager.SpawnEntity(prototype, spillGridCoords);
            var newPuddleComponent = puddle.GetComponent<PuddleComponent>();

            newPuddleComponent.TryAddSolution(solution, sound);

            return newPuddleComponent;
        }

        /// <summary>
        ///     Spills the specified solution at the entity's location if possible.
        /// </summary>
        /// <param name="coordinates">The coordinates to spill the solution at.</param>
        /// <param name="solution">Initial solution for the prototype.</param>
        /// <param name="prototype">The prototype to use.</param>
        /// <param name="puddle">The puddle if one was created, null otherwise.</param>
        /// <param name="sound">Play the spill sound.</param>
        /// <returns>True if a puddle was created, false otherwise.</returns>
        public static bool TrySpillAt(this Solution solution, EntityCoordinates coordinates, string prototype, [NotNullWhen(true)] out PuddleComponent? puddle, bool sound = true)
        {
            puddle = solution.SpillAt(coordinates, prototype, sound);
            return puddle != null;
        }

        public static bool TryGetPuddle(this TileRef tileRef, GridTileLookupSystem? gridTileLookupSystem, [NotNullWhen(true)] out PuddleComponent? puddle)
        {
            foreach (var entity in tileRef.GetEntitiesInTileFast(gridTileLookupSystem))
            {
                if (entity.TryGetComponent(out PuddleComponent? p))
                {
                    puddle = p;
                    return true;
                }
            }

            puddle = null;
            return false;
        }

        public static PuddleComponent? SpillAt(this TileRef tileRef, Solution solution, string prototype, bool overflow = true, bool sound = true)
        {
            if (solution.TotalVolume <= 0)
            {
                return null;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();

            var gridId = tileRef.GridIndex;

            // If space return early, let that spill go out into the void
            if (tileRef.Tile.IsEmpty)
            {
                return null;
            }

            PuddleComponent? puddle = null;

            // Get normalized co-ordinate for spill location and spill it in the centre
            // TODO: Does SnapGrid or something else already do this?
            if (!mapManager.TryGetGrid(gridId, out var spillTileMapGrid))
                return null; // Let's not spill to invalid grids.

            var spillGridCoords = spillTileMapGrid.GridTileToLocal(tileRef.GridIndices);

            var spilt = false;

            foreach (var spillEntity in entityManager.GetEntitiesAt(spillTileMapGrid.ParentMapId, spillGridCoords.Position))
            {
                if (!spillEntity.TryGetComponent(out PuddleComponent? puddleComponent))
                    continue;

                if (!overflow && puddleComponent.WouldOverflow(solution))
                    return null;

                if (!puddleComponent.TryAddSolution(solution, sound))
                    continue;

                puddle = puddleComponent;
                spilt = true;
                break;
            }

            // Did we add to an existing puddle
            if (spilt)
            {
                return puddle;
            }

            var puddleEnt = serverEntityManager.SpawnEntity(prototype, spillGridCoords);
            puddle = puddleEnt.GetComponent<PuddleComponent>();

            puddle.TryAddSolution(solution, sound);

            return puddle;
        }
    }
}
