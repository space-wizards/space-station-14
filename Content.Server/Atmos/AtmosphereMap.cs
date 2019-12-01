using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Interfaces.Atmos;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.Atmos
{
    internal class AtmosphereMap : IAtmosphereMap
    {
#pragma warning disable 649
        // ReSharper disable once InconsistentNaming
        [Dependency] private readonly IMapManager MapManager;
#pragma warning restore 649

        private readonly Dictionary<GridId, GridAtmosphereManager> _gridAtmosphereManagers =
            new Dictionary<GridId, GridAtmosphereManager>();

        public IGridAtmosphereManager GetGridAtmosphereManager(GridId grid)
        {
            if (_gridAtmosphereManagers.TryGetValue(grid, out var manager))
                return manager;

            if (!MapManager.GridExists(grid))
                throw new ArgumentException("Cannot get atmosphere of missing grid", nameof(grid));

            manager = new GridAtmosphereManager(MapManager.GetGrid(grid));
            _gridAtmosphereManagers[grid] = manager;
            return manager;
        }

        public IAtmosphere GetAtmosphere(ITransformComponent position) =>
            GetGridAtmosphereManager(position.GridID).GetAtmosphere(position.GridPosition);

        public void Update(float frameTime)
        {
            foreach (var atmos in _gridAtmosphereManagers.Values)
            {
                atmos.Update(frameTime);
            }
        }
    }

    internal class GridAtmosphereManager : IGridAtmosphereManager
    {
        // Arbitrarily define rooms as being 2.5m high
        private const float ROOM_HEIGHT = 2.5f;

        private readonly IMapGrid _grid;
        private Dictionary<MapIndices, Atmosphere> _atmospheres = new Dictionary<MapIndices, Atmosphere>();

        private HashSet<MapIndices> _invalidatedPositions = new HashSet<MapIndices>();

        public GridAtmosphereManager(IMapGrid grid)
        {
            _grid = grid;
        }

        public IAtmosphere GetAtmosphere(GridCoordinates coordinates) => GetAtmosphere(GridToMapCoord(coordinates));

        private Atmosphere GetAtmosphere(MapIndices pos)
        {
            if (_atmospheres.TryGetValue(pos, out var atmosphere))
                return atmosphere;

            // If this is clearly space - such as someone moving round outside the station - just return immediately
            // and don't add anything to the grid map
            if (IsSpace(pos))
                return null;

            // If the block is obstructed:
            // If the air blocker is marked as 'use adjacent atmosphere' then the cell behaves as if
            // you got one of it's adjacent atmospheres. If not, it returns null.
            var obstruction = GetObstructingComponent(pos);
            if (obstruction != null)
                return obstruction.UseAdjacentAtmosphere ? GetAdjacentAtmospheres(pos).FirstOrDefault().Value : null;

            var connected = FindConnectedCells(pos);

            if (connected == null)
            {
                // Since this is on a floor tile, it's fairly likely this is a room with
                // a breach. As such, cache this position for sake of static objects like
                // air vents which call this constantly.
                _atmospheres[pos] = null;
                return null;
            }

            // TODO create default atmosphere
            atmosphere = new Atmosphere(GetVolumeForCells(connected.Count));

            foreach (var c in connected)
                _atmospheres[c] = atmosphere;

            return atmosphere;
        }

        public void Invalidate(GridCoordinates coordinates)
        {
            _invalidatedPositions.Add(GridToMapCoord(coordinates));
        }

        private MapIndices GridToMapCoord(GridCoordinates pos) => _grid.SnapGridCellFor(pos, SnapGridOffset.Center);

        private List<MapIndices> FindConnectedCells(MapIndices start)
        {
            var inner = new HashSet<MapIndices>();
            var edge = new HashSet<MapIndices> {start};

            // Basic inner/edge finder
            // The inner list is all the positions we know are empty, and take no further actions.
            // The edge list is all the positions for whom we have not searched their neighbours.
            // Move stuff from edge to inner, while adding adjacent empty locations to edge
            // When edge is empty, we have filled the entire room.

            while (edge.Count > 0)
            {
                var temp = new List<MapIndices>(edge);
                edge.Clear();
                foreach (var pos in temp)
                {
                    inner.Add(pos);

                    if (IsSpace(pos))
                        return null;

                    Check(inner, edge, pos, Direction.North);
                    Check(inner, edge, pos, Direction.South);
                    Check(inner, edge, pos, Direction.East);
                    Check(inner, edge, pos, Direction.West);
                }
            }

            return new List<MapIndices>(inner);
        }

        private void Check(ICollection<MapIndices> inner, ISet<MapIndices> edges, MapIndices pos, Direction dir)
        {
            pos += (MapIndices) dir.CardinalToIntVec();
            if (IsObstructed(pos))
                return;

            if (inner.Contains(pos))
                return;

            edges.Add(pos);
        }

        private bool IsObstructed(MapIndices pos) => GetObstructingComponent(pos) != null;

        private AirtightComponent GetObstructingComponent(MapIndices pos)
        {
            foreach (var v in _grid.GetSnapGridCell(pos, SnapGridOffset.Center))
            {
                if (v.Owner.TryGetComponent<AirtightComponent>(out var ac))
                    return ac;
            }

            return null;
        }

        private bool IsSpace(MapIndices pos)
        {
            // TODO use ContentTileDefinition to define in YAML whether or not a tile is considered space
            return _grid.GetTileRef(pos).Tile.IsEmpty;
        }

        public void Update(float frameTime)
        {
            if (_invalidatedPositions.Count != 0)
                Revalidate();
        }

        private void Revalidate()
        {
            foreach (var pos in _invalidatedPositions)
            {
                if (IsObstructed(pos))
                    SplitAtmospheres(pos);
                else
                    MergeAtmospheres(pos);
            }

            _invalidatedPositions.Clear();
        }

        private void SplitAtmospheres(MapIndices pos)
        {
            // Remove the now-covered atmosphere
            _atmospheres.Remove(pos);

            var adjacent = GetAdjacentAtmospheres(pos);
            var adjacentPositions = adjacent.Select(p => pos.Offset(p.Key)).ToList();
            var adjacentAtmospheres = new HashSet<Atmosphere>(adjacent.Values);

            foreach (var atmos in adjacentAtmospheres)
            {
                var i = 0;
                int? spaceIdx = null;
                var sides = new Dictionary<MapIndices, int>();

                foreach (var pair in adjacent.Where(p => p.Value == atmos))
                {
                    var edgePos = pos.Offset(pair.Key);
                    if (sides.ContainsKey(edgePos))
                        continue;

                    var connected = FindConnectedCells(edgePos);
                    if (connected == null)
                    {
                        if (spaceIdx == null)
                            spaceIdx = i++;
                        sides[edgePos] = (int) spaceIdx;
                        continue;
                    }

                    foreach (var connectedPos in connected.Intersect(adjacentPositions))
                        sides[connectedPos] = i;

                    i++;
                }

                // If the sides were all connected, this atmosphere is intact
                // TODO in some way adjust the atmosphere volume
                if (i == 1)
                    continue;

                // If any of the sides that used to have this atmosphere are now exposed to space, remove
                // all the old atmosphere's cells.
                if (spaceIdx != null)
                {
                    foreach (var rpos in FindRoomContents(atmos))
                        _atmospheres.Remove(rpos);
                }

                // Split up each of the sides
                for (var j = 0; j < i; j++)
                {
                    // Find a position to represent this
                    var basePos = sides.First(p => p.Value == j).Key;

                    var connected = FindConnectedCells(basePos);

                    if (connected == null)
                        continue;

                    var newAtmos = new Atmosphere(GetVolumeForCells(connected.Count));

                    // Copy over contents of atmos, scaling it down to maintain the partial pressure
                    if (atmos != null)
                    {
                        newAtmos.Temperature = atmos.Temperature;
                        foreach (var gas in atmos.Gasses)
                            newAtmos.SetQuantity(gas.Gas, gas.Volume * newAtmos.Volume / atmos.Volume);
                    }

                    foreach (var cpos in connected)
                        _atmospheres[cpos] = newAtmos;
                }
            }
        }

        private void MergeAtmospheres(MapIndices pos)
        {
            var adjacent = new HashSet<Atmosphere>(GetAdjacentAtmospheres(pos).Values);

            // If this block doesn't separate two atmospheres, there's nothing to do
            if (adjacent.Count <= 1)
                return;

            var allCells = new List<MapIndices> {pos};
            foreach (var atmos in adjacent)
                allCells.AddRange(FindRoomContents(atmos));

            // Fuse all adjacent atmospheres
            Atmosphere replacement;
            if (adjacent.Contains(null))
            {
                replacement = null;
            }
            else
            {
                replacement = new Atmosphere(GetVolumeForCells(allCells.Count));
                foreach (var atmos in adjacent)
                {
                    if (atmos == null)
                        continue;

                    // Copy all the gasses across
                    foreach (var gas in atmos.Gasses)
                        replacement.Add(gas.Gas, gas.Volume, atmos.Temperature);
                }
            }

            foreach (var cellPos in allCells)
                _atmospheres[cellPos] = replacement;
        }

        private Dictionary<Direction, Atmosphere> GetAdjacentAtmospheres(MapIndices pos)
        {
            var sides = new Dictionary<Direction, Atmosphere>();
            foreach (var dir in Cardinal())
            {
                var side = pos.Offset(dir);
                if (IsObstructed(side))
                    continue;

                sides[dir] = GetAtmosphere(side);
            }

            return sides;
        }

        private IEnumerable<MapIndices> FindRoomContents(Atmosphere atmos)
        {
            // TODO this is O(n) with respect to the map size, so solve this some other way
            var poses = new List<MapIndices>();
            foreach (var (pos, cellAtmos) in _atmospheres)
            {
                if (cellAtmos == atmos)
                    poses.Add(pos);
            }

            return poses;
        }

        private static IEnumerable<Direction> Cardinal() =>
            new[]
            {
                Direction.North, Direction.East, Direction.South, Direction.West
            };

        private float GetVolumeForCells(int cellCount)
        {
            int scale = _grid.TileSize;
            return scale * scale * cellCount * ROOM_HEIGHT;
        }
    }

    internal class Atmosphere : IAtmosphere
    {
        // The universal gas constant, in cubic meters/pascals/kelvin/mols
        // Note this is in pascals, NOT kilopascals - divide by 1000 to convert it
        private const float R = 8.314462618f;

        private readonly Dictionary<Gas, float> _quantities = new Dictionary<Gas, float>();
        private float _temperature;

        public Atmosphere(float volume)
        {
            Volume = volume;
            UpdateCached();
        }

        public IEnumerable<GasProperty> Gasses => _quantities.Select(p => new GasProperty
        {
            Gas = p.Key,
            Volume = p.Value,
            PartialPressure = p.Value * PressureRatio
        });

        public float Volume { get; }

        public float Pressure => Quantity * PressureRatio;

        public float Quantity { get; private set; }

        public float Mass => throw new NotImplementedException();

        public float Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                UpdateCached();
            }
        }

        public float PressureRatio { get; private set; }

        public float QuantityOf(Gas gas)
        {
            return _quantities.ContainsKey(gas) ? _quantities[gas] : 0;
        }

        public float PartialPressureOf(Gas gas)
        {
            return QuantityOf(gas) * PressureRatio;
        }

        public float Add(Gas gas, float quantity, float temperature)
        {
            if (quantity < 0)
                throw new NotImplementedException();

            if (_quantities.ContainsKey(gas))
            {
                _quantities[gas] += quantity;

                // Convert things to doubles while averaging the temperatures, otherwise
                // small amounts of inaccuracy creep in
                var newTotal = 1d * Quantity + quantity;
                Temperature = (float) ((1d * Temperature * Quantity + 1d * temperature * quantity) / newTotal);
            }
            else
            {
                _quantities[gas] = quantity;
                Temperature = temperature;
            }

            // Setting temperature does the update for us, no need to call it manually

            return quantity;
        }

        public float SetQuantity(Gas gas, float quantity)
        {
            if (quantity < 0)
                throw new ArgumentException("Cannot set a negative quantity of gas", nameof(quantity));

            // Discard tiny amounts of gasses
            if (Math.Abs(quantity) < 0.001)
            {
                _quantities.Remove(gas);
                UpdateCached();
                return 0;
            }

            _quantities[gas] = quantity;
            UpdateCached();
            return quantity;
        }

        private void UpdateCached()
        {
            float q = 0;
            foreach (var value in _quantities.Values) q += value;
            Quantity = q;

            PressureRatio = R * Temperature / Volume;
        }
    }
}
