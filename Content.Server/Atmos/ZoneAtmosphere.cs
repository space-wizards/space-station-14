using System.Collections.Generic;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    internal class ZoneAtmosphere : GasMixture
    {
        private readonly GridAtmosphereManager _parentGridManager;
        private readonly ISet<MapIndices> _cells;

        /// <summary>
        /// The collection of grid cells which are a part of this zone.
        /// </summary>
        /// <remarks>
        /// This should be kept in sync with the corresponding entries in <see cref="GridAtmosphereManager"/>.
        /// </remarks>
        public IEnumerable<MapIndices> Cells => _cells;

        /// <summary>
        /// The volume of this zone.
        /// </summary>
        /// <remarks>
        /// This is directly calculated from the number of cells in the zone.
        /// </remarks>
        public override float Volume => _parentGridManager.GetVolumeForCells(_cells.Count);

        public ZoneAtmosphere(GridAtmosphereManager parent, IEnumerable<MapIndices> cells)
        {
            _parentGridManager = parent;
            _cells = new HashSet<MapIndices>(cells);
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
            _cells.Add(cell);
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
            _cells.Remove(cell);
        }
    }
}
