using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public class ZoneAtmosphere
    {
        private readonly IGridAtmosphereManager _parentGridManager;
        private readonly HashSet<MapIndices> _tileIndices;

        /// <summary>
        /// The collection of grid tile indices which are a part of this zone.
        /// </summary>
        /// <remarks>
        /// This should be kept in sync with the corresponding entries in <see cref="GridAtmosphereManager"/>.
        /// </remarks>
        public IEnumerable<MapIndices> TileIndices => _tileIndices;

        public float Pressure { get; private set; }

        /// <summary>
        /// The volume of this zone.
        /// </summary>
        /// <remarks>
        /// This is directly calculated from the number of cells in the zone.
        /// </remarks>
        public float Volume { get; private set; }

        public ZoneAtmosphere(IGridAtmosphereManager parent, IEnumerable<MapIndices> cells)
        {
            _parentGridManager = parent;
            _tileIndices = new HashSet<MapIndices>(cells);
            UpdateCached();
        }

        /// <summary>
        /// Add a cell to the zone.
        /// </summary>
        /// <remarks>
        /// This does not update the parent <see cref="GridAtmosphereManager"/>.
        /// </remarks>
        /// <param name="cell">The indices of the cell to add.</param>
        public void AddCell(MapIndices cell)
        {
            _tileIndices.Add(cell);
            UpdateCached();
        }

        /// <summary>
        /// Remove a cell from the zone.
        /// </summary>
        /// <remarks>
        /// This does not update the parent <see cref="GridAtmosphereManager"/>.
        /// </remarks>
        /// <param name="cell">The indices of the cell to remove.</param>
        public void RemoveCell(MapIndices cell)
        {
            _tileIndices.Remove(cell);
            UpdateCached();
        }

        private void UpdateCached()
        {
            Volume = _parentGridManager.GetVolumeForCells(_tileIndices.Count);
            // TODO ATMOS Calculate pressure
        }
    }
}
