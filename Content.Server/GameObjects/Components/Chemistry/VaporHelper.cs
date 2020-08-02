using Content.Shared.Chemistry;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components.Chemistry
{
    static class VaporHelper // Basically the SpillHelper but for vapor entities
    {
        /// <summary>
        /// Spawns the specified solution as a vapor at the entity's location if possible.
        /// </summary>
        /// <param name="entity">Entity to spawn the vapor at</param>
        /// <param name="solution">Initial solution for the prototype</param>
        internal static void SpillAt(IEntity entity, Solution solution)
        {
            var entityLocation = entity.Transform.GridPosition;
            SpillAt(entityLocation, solution);
        }

        /// <summary>
        /// Spawns solution as a vapor at the specified grid co-ordinates
        /// </summary>
        /// <param name="gridCoordinates">Coordinates to spawn the vapor at</param>
        /// <param name="solution">Initial solution for the prototype</param>
        internal static VaporComponent? SpillAt(GridCoordinates gridCoordinates, Solution solution)
        {
            if (solution.TotalVolume == 0)
            {
                return null;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var serverEntityManager = IoCManager.Resolve<IServerEntityManager>();

            var mapGrid = mapManager.GetGrid(gridCoordinates.GridID);

            // If space return early, let that spill go out into the void
            var tileRef = mapGrid.GetTileRef(gridCoordinates);
            if (tileRef.Tile.IsEmpty)
            {
                return null;
            }

            // Get normalized co-ordinate for spill location and spill it in the centre
            // TODO: Does SnapGrid or something else already do this?
            var vaporTileMapGrid = mapManager.GetGrid(gridCoordinates.GridID);
            var vaporTileRef = vaporTileMapGrid.GetTileRef(gridCoordinates).GridIndices;
            var vaporGridCoords = vaporTileMapGrid.GridTileToLocal(vaporTileRef);

            var spilt = false;

            foreach (var vaporEntity in entityManager.GetEntitiesAt(vaporTileMapGrid.ParentMapId, vaporGridCoords.Position))
            {
                if (!vaporEntity.TryGetComponent(out VaporComponent vaporComponent))
                {
                    continue;
                }

                if (!vaporComponent.TryAddSolution(solution))
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

            var vapor = serverEntityManager.SpawnEntity("Vapor", vaporGridCoords);
            var newVaporComponent = vapor.GetComponent<VaporComponent>();
            newVaporComponent.TryAddSolution(solution);
            //TODO: instantly update here?
            return newVaporComponent;
        }
    }
}
