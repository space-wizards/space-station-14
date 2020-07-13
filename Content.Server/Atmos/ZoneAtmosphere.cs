using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    internal class ZoneAtmosphere : IAtmosphere
    {
        private readonly GridAtmosphereManager _parentGridManager;
        private readonly Dictionary<MapIndices, GasMixture> _cells;
        private GasProperty[] _gasses;
        private float _moles;
        private float _pressure;
        private float _temperature;

        /// <summary>
        /// The collection of grid cells which are a part of this zone.
        /// </summary>
        /// <remarks>
        /// This should be kept in sync with the corresponding entries in <see cref="GridAtmosphereManager"/>.
        /// </remarks>
        public IEnumerable<MapIndices> Cells => _cells.Keys;

        public IReadOnlyDictionary<MapIndices, GasMixture> CellMixtures => _cells;

        public GasProperty[] Gasses => _gasses;

        public float Moles => _moles;

        public float Pressure => _pressure;

        public float Temperature
        {
            get => _temperature;
            set => _temperature = value;
        }

        /// <summary>
        /// The volume of this zone.
        /// </summary>
        /// <remarks>
        /// This is directly calculated from the number of cells in the zone.
        /// </remarks>
        public float Volume => _parentGridManager.GetVolumeForCells(_cells.Count);

        public ZoneAtmosphere(GridAtmosphereManager parent, IDictionary<MapIndices, GasMixture> cells)
        {
            _parentGridManager = parent;
            _cells = new Dictionary<MapIndices, GasMixture>(cells);
            UpdateCached();
        }

        public ZoneAtmosphere(GridAtmosphereManager parent, IEnumerable<MapIndices> cells)
        {
            _parentGridManager = parent;
            _cells = new Dictionary<MapIndices, GasMixture>(cells.ToDictionary(x => x, x => new GasMixture(_parentGridManager.GetVolumeForCells(1))));
            UpdateCached();
        }

        /// <summary>
        /// Add a cell to the zone.
        /// </summary>
        /// <remarks>
        /// This does not update the parent <see cref="GridAtmosphereManager"/>.
        /// </remarks>
        /// <param name="cell">The indices of the cell to add.</param>
        public void AddCell(MapIndices cell, GasMixture mixture)
        {
            _cells.Add(cell, mixture);
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
            _cells.Remove(cell);
            UpdateCached();
        }

        public GasMixture Remove(float amount)
        {
            return RemoveRatio(amount / Moles);
        }

        public GasMixture RemoveRatio(float ratio)
        {
            var mixture = new GasMixture(Volume);

            foreach (var (_, cellMixture) in _cells)
            {
                mixture.Merge(cellMixture.RemoveRatio(ratio));
            }

            UpdateCached();

            return mixture;
        }

        public void Merge(ZoneAtmosphere atmosphere)
        {
            Merge(atmosphere.AsGasMixture());
            UpdateCached();
        }

        public void Merge(GasMixture mixture)
        {
            var ratio = 1 / _cells.Count;

            foreach (var (_, cellMix) in _cells)
            {
                cellMix.Merge(mixture.RemoveRatio(ratio));
            }

            UpdateCached();
        }

        public void UpdateCached()
        {
            var mix = AsGasMixture();
            _gasses = mix.Gasses;
            _pressure = mix.Pressure;
            _temperature = mix.Temperature;
            _moles = mix.Moles;
        }

        public GasMixture AsGasMixture()
        {
            var mixture = new GasMixture(Volume);
            foreach (var (_, tileMix) in _cells)
            {
                mixture.Merge(tileMix);
            }

            return mixture;
        }
    }
}
