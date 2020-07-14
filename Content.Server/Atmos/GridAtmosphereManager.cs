using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Atmos
{
    /// <inheritdoc cref="IGridAtmosphereManager"/>
    internal class GridAtmosphereManager : IGridAtmosphereManager
    {
        // Arbitrarily define rooms as being 2.5m high
        private const float RoomHeight = 2.5f;

        private readonly IMapGrid _grid;
        private readonly Dictionary<MapIndices, ZoneAtmosphere> _atmospheres = new Dictionary<MapIndices, ZoneAtmosphere>();

        private HashSet<MapIndices> _invalidatedCoords = new HashSet<MapIndices>();

        public GridAtmosphereManager(IMapGrid grid)
        {
            _grid = grid;
        }

        public IAtmosphere GetAtmosphere(MapIndices indices) => GetZoneAtmosphere(indices);

        private ZoneAtmosphere GetZoneAtmosphere(MapIndices indices)
        {
            if (_atmospheres.TryGetValue(indices, out var atmosphere))
                return atmosphere;

            // If this is clearly space - such as someone moving round outside the station - just return immediately
            // and don't add anything to the grid map
            if (IsSpace(indices))
                return null;

            // If the block is obstructed:
            // If the air blocker is marked as 'use adjacent atmosphere' then the cell behaves as if
            // you got one of it's adjacent atmospheres. If not, it returns null.
            var obstruction = GetObstructingComponent(indices);
            if (obstruction != null)
                return obstruction.UseAdjacentAtmosphere ? GetAdjacentAtmospheres(indices).FirstOrDefault().Value : null;

            var connected = FindConnectedCells(indices);

            if (connected == null)
            {
                // Since this is on a floor tile, it's fairly likely this is a room with
                // a breach. As such, cache this position for sake of static objects like
                // air vents which call this constantly.
                _atmospheres[indices] = null;
                return null;
            }

            atmosphere = new ZoneAtmosphere(this, connected.ToDictionary(x => x, x => new GasMixture(GetVolumeForCells(1))));
            // TODO: Not hardcode this
            //var oneAtmosphere = 101325; // 1 atm in Pa
            //var roomTemp = 293.15f; // 20c in k
            // Calculating moles to add: n = (PV)/(RT)
            //var totalMoles = (oneAtmosphere * atmosphere.Volume) / (Atmospherics.R * roomTemp);
            //atmosphere.Add("oxygen", totalMoles * 0.2f);
            //atmosphere.Add("nitrogen", totalMoles * 0.8f);
            //atmosphere.Temperature = roomTemp;

            foreach (var c in connected)
                _atmospheres[c] = atmosphere;

            return atmosphere;
        }

        public void Invalidate(MapIndices indices)
        {
            _invalidatedCoords.Add(indices);
        }

        /// <summary>
        /// Get the collection of grid cells connected to a given cell.
        /// </summary>
        /// <param name="start">The cell to start building the collection from.</param>
        /// <returns>
        /// <c>null</c>, if the cell is somehow connected to space. Otherwise, the
        /// collection of all cells connected to the starting cell (inclusive).
        /// </returns>
        private ISet<MapIndices> FindConnectedCells(MapIndices start)
        {
            var inner = new HashSet<MapIndices>();
            var edge = new HashSet<MapIndices> {start};

            void Check(MapIndices pos, Direction dir)
            {
                pos = pos.Offset(dir);
                if (IsZoneBlocked(pos))
                    return;

                if (inner.Contains(pos))
                    return;

                edge.Add(pos);
            }

            // Basic inner/edge finder
            // The inner list is all the positions we know are empty, and take no further actions.
            // The edge list is all the positions for whom we have not searched their neighbours.
            // Move stuff from edge to inner, while adding adjacent empty locations to edge
            // When edge is empty, we have filled the entire room.

            while (edge.Count > 0)
            {
                var temp = new HashSet<MapIndices>(edge);
                edge.Clear();
                foreach (var pos in temp)
                {
                    inner.Add(pos);

                    if (IsSpace(pos))
                        return null;

                    foreach (var dir in Cardinal())
                    {
                        Check(pos, dir);
                    }
                }
            }

            return inner;
        }

        public bool IsZoneBlocked(MapIndices indices)
        {
            var ac = GetObstructingComponent(indices);
            return ac != null && ac.ZoneBlocked;
        }

        public bool IsAirBlocked(MapIndices indices)
        {
            var ac = GetObstructingComponent(indices);
            return ac != null && ac.AirBlocked;
        }

        private AirtightComponent GetObstructingComponent(MapIndices indices)
        {
            foreach (var v in _grid.GetSnapGridCell(indices, SnapGridOffset.Center))
            {
                if (v.Owner.TryGetComponent<AirtightComponent>(out var ac))
                    return ac;
            }

            return null;
        }

        public bool IsSpace(MapIndices indices)
        {
            // TODO ATMOS use ContentTileDefinition to define in YAML whether or not a tile is considered space
            return _grid.GetTileRef(indices).Tile.IsEmpty;
        }

        public void Update(float frameTime)
        {
            if (_invalidatedCoords.Count != 0)
                Revalidate();
        }

        private void Revalidate()
        {
            foreach (var indices in _invalidatedCoords)
            {
                if (IsSpace(indices))
                {
                    ClearAtmospheres(indices);
                }
                else if (IsZoneBlocked(indices))
                {
                    SplitAtmospheres(indices);
                } else if (IsAirBlocked(indices))
                {
                    // connect zones?
                }
                else
                {
                    MergeAtmospheres(indices);
                }
            }

            _invalidatedCoords.Clear();
        }

        private void SplitAtmospheres(MapIndices indices)
        {
            Debug.Assert(IsZoneBlocked(indices));

            // Remove the now-covered atmosphere (if there was one)
            if (_atmospheres.TryGetValue(indices, out var coveredAtmos))
            {
                _atmospheres.Remove(indices);

                if (coveredAtmos != null)
                    coveredAtmos.RemoveCell(indices);
            }

            // The collection of split atmosphere components - each one is a collection
            // of connected grid indices
            var sides = new HashSet<ISet<MapIndices>>();

            foreach (var edgeCoordinate in Cardinal().Select(dir => indices.Offset(dir)))
            {
                // If this is true, this edge is already contained in another edge's connected area
                if (sides.Any(collection => collection != null && collection.Contains(edgeCoordinate)))
                    continue;

                // If this is true, the edge has no atmosphere to split anyways
                if (IsZoneBlocked(edgeCoordinate))
                    continue;

                // Otherwise, this is a new connected area (now that zones are split)
                var connected = FindConnectedCells(edgeCoordinate);
                // So add it to the map, and increase the number of split atmospheres
                sides.Add(connected);
            }

            // If the sides were all connected (or have no air), this atmosphere is intact
            if (sides.Count <= 1)
            {
                return;
            }

            // Split up each of the sides
            foreach(var connected in sides)
            {
                if (connected == null)
                    continue;

                var newAtmos = new ZoneAtmosphere(this, connected);

                // Copy over contents of atmos, scaling it down to maintain the partial pressure
                if (coveredAtmos != null)
                {
                    newAtmos.Temperature = coveredAtmos.Temperature;
                    newAtmos.Merge(coveredAtmos.Remove(newAtmos.Volume));
                }

                foreach (var cpos in connected)
                    _atmospheres[cpos] = newAtmos;
            }
        }

        public GasMixture GetMixtureOnTile(MapIndices indices)
        {
            if (!_atmospheres.TryGetValue(indices, out var zone)) return null;
            return !zone.CellMixtures.TryGetValue(indices, out var mixture) ? null : mixture;
        }

        private void MergeAtmospheres(MapIndices indices)
        {
            Debug.Assert(!IsZoneBlocked(indices));

            var adjacent = new HashSet<ZoneAtmosphere>(GetAdjacentAtmospheres(indices).Values);

            // If this block doesn't separate two atmospheres, there's no merging to do
            if (adjacent.Count <= 1)
            {
                var joinedAtmos = adjacent.FirstOrDefault();

                if (joinedAtmos != null)
                {
                    // If the block is not in space, just add it to the existing zone
                    // TODO ATMOS Default gas maybe? Prob not.
                    joinedAtmos.AddCell(indices, new GasMixture(GetVolumeForCells(1)));
                    _atmospheres[indices] = joinedAtmos;
                    return;
                }
                else
                {
                    // Otherwise, try to create a new zone around the block
                    var connectedCells =
                        FindConnectedCells(indices)
                            ?.ToDictionary(x => x, x => GetMixtureOnTile(x)
                                                       ?? new GasMixture(GetVolumeForCells(1)));

                    if (connectedCells != null)
                    {
                        var newZone = new ZoneAtmosphere(this, connectedCells);
                        foreach (var (cell, mix) in connectedCells)
                            _atmospheres[cell] = newZone;
                    }
                }
                return;
            }

            var allCells = new List<MapIndices> {indices};
            foreach (var atmos in adjacent.Where(zone => zone != null))
                allCells.AddRange(atmos.Cells);

            // Fuse all adjacent atmospheres
            ZoneAtmosphere replacement;
            if (adjacent.Contains(null))
            {
                replacement = null;
            }
            else
            {
                replacement = new ZoneAtmosphere(this, allCells);
                foreach (var atmos in adjacent)
                {
                    // Copy all the gasses across
                    replacement.Merge(atmos);
                }
            }

            foreach (var cellPos in allCells)
                _atmospheres[cellPos] = replacement;
        }

        /// <summary>
        /// Empty the atmospheres of a tile known to be space.
        /// </summary>
        /// <param name="indices"></param>
        private void ClearAtmospheres(MapIndices indices)
        {
            Debug.Assert(IsSpace(indices));

            _atmospheres[indices] = null;

            foreach (var (_, atmosphere) in GetAdjacentAtmospheres(indices))
            {
                if (atmosphere == null)
                    continue;

                foreach (var cell in atmosphere.Cells)
                {
                    _atmospheres[cell] = null;
                }
            }
        }

        private Dictionary<Direction, ZoneAtmosphere> GetAdjacentAtmospheres(MapIndices coords)
        {
            var sides = new Dictionary<Direction, ZoneAtmosphere>();
            foreach (var dir in Cardinal())
            {
                var side = coords.Offset(dir);
                if (IsZoneBlocked(side))
                    continue;

                sides[dir] = GetZoneAtmosphere(side);
            }

            return sides;
        }

        private static IEnumerable<Direction> Cardinal() =>
            new[]
            {
                Direction.North, Direction.East, Direction.South, Direction.West
            };

        internal float GetVolumeForCells(int cellCount)
        {
            return _grid.TileSize * cellCount * Atmospherics.CellVolume;
        }
    }
}
