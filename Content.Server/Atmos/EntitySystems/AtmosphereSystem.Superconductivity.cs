using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        private void Superconduct(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
        {
            var directions = ConductivityDirections(gridAtmosphere, tile);

            for(var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!directions.IsFlagSet(direction))
                    continue;

                var adjacent = tile.AdjacentTiles[i];

                // TODO ATMOS handle adjacent being null.
                if (adjacent == null || adjacent.ThermalConductivity == 0f)
                    continue;

                if(adjacent.ArchivedCycle < gridAtmosphere.UpdateCounter)
                    Archive(adjacent, gridAtmosphere.UpdateCounter);

                NeighborConductWithSource(gridAtmosphere, adjacent, tile);

                ConsiderSuperconductivity(gridAtmosphere, adjacent);
            }

            RadiateToSpace(tile);
            FinishSuperconduction(gridAtmosphere, tile);
        }

        private AtmosDirection ConductivityDirections(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
        {
            if(tile.Air == null)
            {
                if(tile.ArchivedCycle < gridAtmosphere.UpdateCounter)
                    Archive(tile, gridAtmosphere.UpdateCounter);
                return AtmosDirection.All;
            }

            // TODO ATMOS check if this is correct
            return AtmosDirection.All;
        }

        public bool ConsiderSuperconductivity(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
        {
            if (tile.ThermalConductivity == 0f || !Superconduction)
                return false;

            gridAtmosphere.SuperconductivityTiles.Add(tile);
            return true;
        }

        public bool ConsiderSuperconductivity(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, bool starting)
        {
            if (!Superconduction)
                return false;

            if (tile.Air == null || tile.Air.Temperature < (starting
                    ? Atmospherics.MinimumTemperatureStartSuperConduction
                    : Atmospherics.MinimumTemperatureForSuperconduction))
                return false;

            return !(GetHeatCapacity(tile.Air) < Atmospherics.MCellWithRatio)
                   && ConsiderSuperconductivity(gridAtmosphere, tile);
        }

        public void FinishSuperconduction(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
        {
            // Conduct with air on my tile if I have it
            if (tile.Air != null)
            {
                tile.Temperature = TemperatureShare(tile, tile.ThermalConductivity, tile.Temperature, tile.HeatCapacity);
            }

            FinishSuperconduction(gridAtmosphere, tile, tile.Air?.Temperature ?? tile.Temperature);
        }

        public void FinishSuperconduction(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, float temperature)
        {
            // Make sure it's still hot enough to continue conducting.
            if (temperature < Atmospherics.MinimumTemperatureForSuperconduction)
            {
                gridAtmosphere.SuperconductivityTiles.Remove(tile);
            }
        }

        public void NeighborConductWithSource(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, TileAtmosphere other)
        {
            if (tile.Air == null)
            {
                // TODO ATMOS: why does this need to check if a tile exists if it doesn't use the tile?
                if (TryComp<MapGridComponent>(other.GridIndex, out var grid)
                    && _mapSystem.TryGetTileRef(other.GridIndex, grid, other.GridIndices, out var _))
                {
                    TemperatureShareOpenToSolid(other, tile);
                }
                else
                {
                    TemperatureShareMutualSolid(other, tile, tile.ThermalConductivity);
                }

                // TODO ATMOS: tile.TemperatureExpose(null, tile.Temperature, gridAtmosphere.GetVolumeForCells(1));
                return;
            }

            if (other.Air != null)
            {
                TemperatureShare(other, tile, Atmospherics.WindowHeatTransferCoefficient);
            }
            else
            {
                TemperatureShareOpenToSolid(tile, other);
            }

            AddActiveTile(gridAtmosphere, tile);
        }

        private void TemperatureShareOpenToSolid(TileAtmosphere tile, TileAtmosphere other)
        {
            if (tile.Air == null)
                return;

            other.Temperature = TemperatureShare(tile, other.ThermalConductivity, other.Temperature, other.HeatCapacity);
        }

        private void TemperatureShareMutualSolid(TileAtmosphere tile, TileAtmosphere other, float conductionCoefficient)
        {
            if (tile.AirArchived == null || other.AirArchived == null)
                return;

            var deltaTemperature = (tile.AirArchived.Temperature - other.AirArchived.Temperature);
            if (MathF.Abs(deltaTemperature) > Atmospherics.MinimumTemperatureDeltaToConsider
                && tile.HeatCapacity != 0f && other.HeatCapacity != 0f)
            {
                var heat = conductionCoefficient * deltaTemperature *
                           (tile.HeatCapacity * other.HeatCapacity / (tile.HeatCapacity + other.HeatCapacity));

                tile.Temperature -= heat / tile.HeatCapacity;
                other.Temperature += heat / other.HeatCapacity;
            }
        }

        public void RadiateToSpace(TileAtmosphere tile)
        {
            if (tile.AirArchived == null)
                return;

            // Considering 0ºC as the break even point for radiation in and out.
            if (tile.Temperature > Atmospherics.T0C)
            {
                // Hardcoded space temperature.
                var deltaTemperature = (tile.AirArchived.Temperature - Atmospherics.TCMB);
                if ((tile.HeatCapacity > 0) && (MathF.Abs(deltaTemperature) > Atmospherics.MinimumTemperatureDeltaToConsider))
                {
                    var heat = tile.ThermalConductivity * deltaTemperature * (tile.HeatCapacity *
                        Atmospherics.HeatCapacityVacuum / (tile.HeatCapacity + Atmospherics.HeatCapacityVacuum));

                    tile.Temperature -= heat;
                }
            }
        }
    }
}
