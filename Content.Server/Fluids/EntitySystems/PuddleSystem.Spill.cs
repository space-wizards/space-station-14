using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Coordinates.Helpers;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Fluids.EntitySystems
{
    public partial class PuddleSystem
    {
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;
        [Dependency] private readonly IEntityLookup _entityLookup = default!;

        /// <summary>
        ///     Whether adding this solution to this puddle would overflow.
        /// </summary>
        /// <param name="puddle">Puddle to which we are adding solution</param>
        /// <param name="solution">Solution we intend to add</param>
        /// <returns></returns>
        private bool WouldOverflow(PuddleComponent puddle, Solution solution)
        {
            return puddle.CurrentVolume + solution.TotalVolume > puddle.OverflowVolume;
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
        /// <returns>The puddle if one was created, null otherwise.</returns>
        public PuddleComponent? SpillAt(Solution solution, IEntity entity, string prototype,
            bool sound = true)
        {
            return SpillAt(solution, entity.Transform.Coordinates, prototype, sound);
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
        public bool TrySpillAt(Solution solution, IEntity entity, string prototype,
            [NotNullWhen(true)] out PuddleComponent? puddle, bool sound = true)
        {
            puddle = SpillAt(solution, entity, prototype, sound);
            return puddle != null;
        }

        /// <summary>
        ///     Spills solution at the specified grid coordinates.
        /// </summary>
        /// <param name="solution">Initial solution for the prototype.</param>
        /// <param name="coordinates">The coordinates to spill the solution at.</param>
        /// <param name="prototype">The prototype to use.</param>
        /// <param name="overflow"></param>
        /// <param name="sound">Whether or not to play the spill sound.</param>
        /// <returns>The puddle if one was created, null otherwise.</returns>
        public PuddleComponent? SpillAt(Solution solution, EntityCoordinates coordinates, string prototype,
            bool overflow = true, bool sound = true)
        {
            if (solution.TotalVolume == 0) return null;



            if (!_mapManager.TryGetGrid(coordinates.GetGridId(EntityManager), out var mapGrid))
                return null; // Let's not spill to space.

            return SpillAt(mapGrid.GetTileRef(coordinates), solution, prototype, overflow, sound);
        }

        public bool TryGetPuddle(TileRef tileRef, GridTileLookupSystem? gridTileLookupSystem,
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

        public PuddleComponent? SpillAt(TileRef tileRef, Solution solution, string prototype,
            bool overflow = true, bool sound = true)
        {
            if (solution.TotalVolume <= 0)
                return null;

            // If space return early, let that spill go out into the void
            if (tileRef.Tile.IsEmpty)
                return null;

            var gridId = tileRef.GridIndex;
            if (!_mapManager.TryGetGrid(gridId, out var mapGrid))
                return null; // Let's not spill to invalid grids.

            // Get normalized co-ordinate for spill location and spill it in the centre
            // TODO: Does SnapGrid or something else already do this?
            var spillGridCoords = mapGrid.GridTileToLocal(tileRef.GridIndices);

            PuddleComponent? puddle = null;
            var spilt = false;

            var spillEntities = _entityLookup
                .GetEntitiesIntersecting(mapGrid.ParentMapId, spillGridCoords.Position).ToArray();
            var solutionsSystem = Get<SolutionContainerSystem>();

            foreach (var spillEntity in spillEntities)
            {
                if (solutionsSystem.TryGetRefillableSolution(spillEntity.Uid, out var solutionContainerComponent))
                {
                    solutionsSystem.Refill(spillEntity.Uid, solutionContainerComponent,
                        solution.SplitSolution(ReagentUnit.Min(
                            solutionContainerComponent.AvailableVolume,
                            solutionContainerComponent.MaxSpillRefill))
                    );
                }
            }

            foreach (var spillEntity in spillEntities)
            {
                if (!spillEntity.TryGetComponent(out PuddleComponent? puddleComponent)) continue;

                if (!overflow && WouldOverflow(puddleComponent, solution)) return null;

                if (!TryAddSolution(puddleComponent, solution, sound)) continue;

                puddle = puddleComponent;
                spilt = true;
                break;
            }

            // Did we add to an existing puddle
            if (spilt) return puddle;

            var puddleEnt = _serverEntityManager.SpawnEntity(prototype, spillGridCoords);
            var newPuddleComponent = puddleEnt.GetComponent<PuddleComponent>();

            TryAddSolution(newPuddleComponent, solution, sound);

            return newPuddleComponent;
        }

        // Flow rate should probably be controlled globally so this is it for now
        private bool TryAddSolution(PuddleComponent puddleComponent, Solution solution,
            bool sound = true,
            bool checkForOverflow = true)
        {
            if (solution.TotalVolume == 0)
            {
                return false;
            }

            var puddleSolution = _solutionContainerSystem
                .GetSolution(puddleComponent.Owner.Uid, puddleComponent.SolutionName);
            var result = _solutionContainerSystem
                .TryAddSolution(puddleComponent.Owner.Uid, puddleSolution, solution);
            if (!result)
            {
                return false;
            }

            RaiseLocalEvent(puddleComponent.Owner.Uid, new SolutionChangedEvent());

            if (checkForOverflow)
            {
                CheckOverflow(puddleComponent);
            }

            if (!sound)
            {
                return true;
            }

            SoundSystem.Play(Filter.Pvs(puddleComponent.Owner), puddleComponent.SpillSound.GetSound(),
                puddleComponent.Owner);
            return true;
        }
    }
}
