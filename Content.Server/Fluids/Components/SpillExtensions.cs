using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Coordinates.Helpers;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Fluids.Components
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
        public static PuddleComponent? SpillAt(this Solution solution, IEntity entity, string prototype,
            bool sound = true)
        {
            return solution.SpillAt(entity.Transform.Coordinates, prototype, sound);
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
        public static bool TrySpillAt(this Solution solution, IEntity entity, string prototype,
            [NotNullWhen(true)] out PuddleComponent? puddle, bool sound = true)
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
        public static PuddleComponent? SpillAt(this Solution solution, EntityCoordinates coordinates, string prototype,
            bool overflow = true, bool sound = true)
        {
            if (solution.TotalVolume == 0) return null;

            var mapManager = IoCManager.Resolve<IMapManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!mapManager.TryGetGrid(coordinates.GetGridId(entityManager), out var mapGrid))
                return null; // Let's not spill to space.

            return SpillAt(mapGrid.GetTileRef(coordinates), solution, prototype, overflow, sound);
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
        public static bool TrySpillAt(this Solution solution, EntityCoordinates coordinates, string prototype,
            [NotNullWhen(true)] out PuddleComponent? puddle, bool sound = true)
        {
            puddle = solution.SpillAt(coordinates, prototype, sound);
            return puddle != null;
        }

        public static bool TryGetPuddle(this TileRef tileRef, GridTileLookupSystem? gridTileLookupSystem,
            [NotNullWhen(true)] out PuddleComponent? puddle)
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

        public static PuddleComponent? SpillAt(this TileRef tileRef, Solution solution, string prototype,
            bool overflow = true, bool sound = true)
        {
            if (solution.TotalVolume <= 0) return null;

            var mapManager = IoCManager.Resolve<IMapManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();

            // If space return early, let that spill go out into the void
            if (tileRef.Tile.IsEmpty) return null;

            var gridId = tileRef.GridIndex;
            if (!mapManager.TryGetGrid(gridId, out var mapGrid)) return null; // Let's not spill to invalid grids.

            // Get normalized co-ordinate for spill location and spill it in the centre
            // TODO: Does SnapGrid or something else already do this?
            var spillGridCoords = mapGrid.GridTileToLocal(tileRef.GridIndices);

            PuddleComponent? puddle = null;
            var spilt = false;

            var spillEntities = IoCManager.Resolve<IEntityLookup>()
                .GetEntitiesIntersecting(mapGrid.ParentMapId, spillGridCoords.Position).ToArray();
            foreach (var spillEntity in spillEntities)
            {
                if (EntitySystem.Get<SolutionContainerSystem>()
                    .TryGetRefillableSolution(spillEntity.Uid, out var solutionContainerComponent))
                {
                    EntitySystem.Get<SolutionContainerSystem>().Refill(spillEntity.Uid, solutionContainerComponent,
                        solution.SplitSolution(ReagentUnit.Min(
                            solutionContainerComponent.AvailableVolume,
                            solutionContainerComponent.MaxSpillRefill))
                    );
                }
            }

            var puddleSystem = EntitySystem.Get<PuddleSystem>();

            foreach (var spillEntity in spillEntities)
            {
                if (!spillEntity.TryGetComponent(out PuddleComponent? puddleComponent)) continue;

                if (!overflow && puddleSystem.WouldOverflow(puddleComponent.Owner.Uid, solution, puddleComponent)) return null;

                if (!puddleSystem.TryAddSolution(puddleComponent.Owner.Uid, solution, sound)) continue;

                puddle = puddleComponent;
                spilt = true;
                break;
            }

            // Did we add to an existing puddle
            if (spilt) return puddle;

            var puddleEnt = serverEntityManager.SpawnEntity(prototype, spillGridCoords);
            var newPuddleComponent = puddleEnt.GetComponent<PuddleComponent>();

            puddleSystem.TryAddSolution(newPuddleComponent.Owner.Uid, solution, sound);

            return newPuddleComponent;
        }
    }
}
