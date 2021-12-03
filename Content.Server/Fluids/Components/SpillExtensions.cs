using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Coordinates.Helpers;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Fluids.Components
{
    // TODO: Kill these with fire
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
        /// <param name="combine">Whether to attempt to merge with existing puddles</param>
        public static PuddleComponent? SpillAt(this Solution solution, IEntity entity, string prototype,
            bool sound = true, bool combine = true)
        {
            return solution.SpillAt(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity).Coordinates, prototype, sound, combine: combine);
        }

        /// <summary>
        ///     Spills the specified solution at the entity's location if possible.
        /// </summary>
        /// <param name="entity">
        ///     The entity to use as a location to spill the solution at.
        /// </param>
        /// <param name="solution">Initial solution for the prototype.</param>
        /// <param name="prototype">The prototype to use.</param>
        /// <param name="sound">Play the spill sound.</param>
        /// <param name="entityManager"></param>
        /// <param name="combine">Whether to attempt to merge with existing puddles</param>
        /// <returns>The puddle if one was created, null otherwise.</returns>
        public static PuddleComponent? SpillAt(this Solution solution, EntityUid entity, string prototype,
            bool sound = true, IEntityManager? entityManager = null, bool combine = true)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            return solution.SpillAt(entityManager.GetComponent<TransformComponent>(entity).Coordinates, prototype, sound, combine: combine);
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
        /// <param name="combine">Whether to attempt to merge with existing puddles</param>
        /// <returns>True if a puddle was created, false otherwise.</returns>
        public static bool TrySpillAt(this Solution solution, IEntity entity, string prototype,
            [NotNullWhen(true)] out PuddleComponent? puddle, bool sound = true, bool combine = true)
        {
            puddle = solution.SpillAt(entity, prototype, sound, combine: combine);
            return puddle != null;
        }

        /// <summary>
        ///     Spills solution at the specified grid coordinates.
        /// </summary>
        /// <param name="solution">Initial solution for the prototype.</param>
        /// <param name="coordinates">The coordinates to spill the solution at.</param>
        /// <param name="prototype">The prototype to use.</param>
        /// <param name="sound">Whether or not to play the spill sound.</param>
        /// <param name="combine">Whether to attempt to merge with existing puddles</param>
        /// <returns>The puddle if one was created, null otherwise.</returns>
        public static PuddleComponent? SpillAt(this Solution solution, EntityCoordinates coordinates, string prototype,
            bool overflow = true, bool sound = true, bool combine = true)
        {
            if (solution.TotalVolume == 0) return null;

            var mapManager = IoCManager.Resolve<IMapManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!mapManager.TryGetGrid(coordinates.GetGridId(entityManager), out var mapGrid))
                return null; // Let's not spill to space.

            return SpillAt(mapGrid.GetTileRef(coordinates), solution, prototype, overflow, sound, combine: combine);
        }

        /// <summary>
        ///     Spills the specified solution at the entity's location if possible.
        /// </summary>
        /// <param name="coordinates">The coordinates to spill the solution at.</param>
        /// <param name="solution">Initial solution for the prototype.</param>
        /// <param name="prototype">The prototype to use.</param>
        /// <param name="puddle">The puddle if one was created, null otherwise.</param>
        /// <param name="sound">Play the spill sound.</param>
        /// <param name="combine">Whether to attempt to merge with existing puddles</param>
        /// <returns>True if a puddle was created, false otherwise.</returns>
        public static bool TrySpillAt(this Solution solution, EntityCoordinates coordinates, string prototype,
            [NotNullWhen(true)] out PuddleComponent? puddle, bool sound = true, bool combine = true)
        {
            puddle = solution.SpillAt(coordinates, prototype, sound, combine: combine);
            return puddle != null;
        }

        public static bool TryGetPuddle(this TileRef tileRef, GridTileLookupSystem? gridTileLookupSystem,
            [NotNullWhen(true)] out PuddleComponent? puddle)
        {
            foreach (var entity in tileRef.GetEntitiesInTileFast(gridTileLookupSystem))
            {
                if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out PuddleComponent? p))
                {
                    puddle = p;
                    return true;
                }
            }

            puddle = null;
            return false;
        }

        public static PuddleComponent? SpillAt(this TileRef tileRef, Solution solution, string prototype,
            bool overflow = true, bool sound = true, bool noTileReact = false, bool combine = true)
        {
            if (solution.TotalVolume <= 0) return null;

            // If space return early, let that spill go out into the void
            if (tileRef.Tile.IsEmpty) return null;

            var mapManager = IoCManager.Resolve<IMapManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();

            var gridId = tileRef.GridIndex;
            if (!mapManager.TryGetGrid(gridId, out var mapGrid)) return null; // Let's not spill to invalid grids.

            if (!noTileReact)
            {
                // First, do all tile reactions
                foreach (var reagent in solution.Contents.ToArray())
                {
                    var proto = prototypeManager.Index<ReagentPrototype>(reagent.ReagentId);
                    proto.ReactionTile(tileRef, reagent.Quantity);
                }
            }

            // Tile reactions used up everything.
            if (solution.CurrentVolume == FixedPoint2.Zero)
                return null;

            // Get normalized co-ordinate for spill location and spill it in the centre
            // TODO: Does SnapGrid or something else already do this?
            var spillGridCoords = mapGrid.GridTileToWorld(tileRef.GridIndices);

            var spillEntities = IoCManager.Resolve<IEntityLookup>()
                .GetEntitiesIntersecting(mapGrid.ParentMapId, spillGridCoords.Position).ToArray();
            foreach (var spillEntity in spillEntities)
            {
                if (EntitySystem.Get<SolutionContainerSystem>()
                    .TryGetRefillableSolution(spillEntity, out var solutionContainerComponent))
                {
                    EntitySystem.Get<SolutionContainerSystem>().Refill(spillEntity, solutionContainerComponent,
                        solution.SplitSolution(FixedPoint2.Min(
                            solutionContainerComponent.AvailableVolume,
                            solutionContainerComponent.MaxSpillRefill))
                    );
                }
            }

            var puddleSystem = EntitySystem.Get<PuddleSystem>();

            if (combine)
            {
                foreach (var spillEntity in spillEntities)
                {
                    if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(spillEntity, out PuddleComponent? puddleComponent)) continue;

                    if (!overflow && puddleSystem.WouldOverflow(puddleComponent.Owner, solution, puddleComponent)) return null;

                    if (!puddleSystem.TryAddSolution(puddleComponent.Owner, solution, sound)) continue;

                    return puddleComponent;
                }
            }

            var puddleEnt = serverEntityManager.SpawnEntity(prototype, spillGridCoords);
            var newPuddleComponent = IoCManager.Resolve<IEntityManager>().GetComponent<PuddleComponent>(puddleEnt);

            puddleSystem.TryAddSolution(newPuddleComponent.Owner, solution, sound);

            return newPuddleComponent;
        }
    }
}
