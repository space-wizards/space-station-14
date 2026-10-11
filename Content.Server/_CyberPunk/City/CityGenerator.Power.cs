using System.Linq;
using Content.Server._CyberPunk.Procgen;

namespace Content.Server._CyberPunk.City;

public static partial class CityGenerator
{
    /// <summary>Zones wealthy enough that each block keeps an SMES of its own between the grid and its substation.</summary>
    private static readonly HashSet<CityZone> Affluent = new() { CityZone.Corporate, CityZone.HighClass, CityZone.Public };

    /// <summary>An area wired as one, from one substation: a block, a township, a checkpoint or the solar plant.</summary>
    private readonly record struct Region(int X, int Y, int W, int H, bool Affluent);

    /// <summary>The solar plant's SMES units: the terminals charging them, and the cable their output leaves by.</summary>
    private sealed record SolarBank(List<(int X, int Y)> Terminals, List<(int X, int Y)> Outputs);

    /// <summary>How far past its edge a region's substation and cables may go: onto the avenues' sidewalks.</summary>
    private const int RegionMargin = 2;

    /// <summary>How big a walled-in space can be and still be a room rather than a courtyard.</summary>
    private const int MaxRoomTiles = DistrictSize * DistrictSize;

    private static bool IsWallLike(CityStructure structure)
    {
        return structure is >= CityStructure.Wall and <= CityStructure.Door;
    }

    private static bool IsPlainWall(CityStructure structure)
    {
        return structure is >= CityStructure.Wall and <= CityStructure.WallWood;
    }

    /// <summary>
    /// Wires the city. The solar panels charge the SMES bank at the plant, whose output runs under the sidewalks
    /// as high-voltage cable to a substation by every block. Wealthy blocks put an SMES of their own in between.
    /// Each substation feeds medium-voltage cable to an APC on the wall of every building in its block, and each
    /// APC feeds low-voltage cable round the inside of every room of its building, from room to room through the
    /// doors.
    /// </summary>
    private static void Wire(CityPlan plan, SolarBank bank, List<Region> regions)
    {
        var size = plan.Size;
        var (roomOf, rooms) = FindRooms(plan);
        var (buildings, doors) = BuildingsAndDoors(plan, roomOf, rooms);

        var regionBuildings = regions.Select(_ => new List<List<int>>()).ToArray();
        foreach (var building in buildings)
        {
            var first = rooms[building[0]][0];
            var (x, y) = (first % size, first / size);
            var r = regions.FindIndex(g => x >= g.X && y >= g.Y && x < g.X + g.W && y < g.Y + g.H);
            if (r >= 0)
                regionBuildings[r].Add(building);
        }

        var nearInput = WirePanels(plan, bank);

        var stations = new (int Entry, int Substation)?[regions.Count];
        for (var r = 0; r < regions.Count; r++)
        {
            if (regionBuildings[r].Count > 0)
                stations[r] = PlaceSubstation(plan, regions[r], roomOf, nearInput);
        }

        WireGrid(plan, bank, regions, stations, roomOf, nearInput);

        var apcs = new HashSet<int>();
        for (var r = 0; r < regions.Count; r++)
        {
            if (stations[r] is not { } station)
                continue;

            var feed = new Spread(plan, Bounds(plan, regions[r], RegionMargin), new[] { station.Substation },
                i => MediumCost(plan, roomOf, i), _ => true);
            foreach (var building in regionBuildings[r])
            {
                WireBuilding(plan, building, rooms, roomOf, doors, feed, apcs);
            }
        }
    }

    /// <summary>
    /// Finds the rooms: walled-in patches of floor, small enough not to be a courtyard.
    /// </summary>
    /// <returns>Which room each tile is in, or -1, and each room's tiles.</returns>
    private static (int[] RoomOf, List<List<int>> Rooms) FindRooms(CityPlan plan)
    {
        var size = plan.Size;
        var roomOf = new int[size * size];
        var seen = new bool[size * size];
        Array.Fill(roomOf, -1);
        var rooms = new List<List<int>>();
        bool Inside(int i)
        {
            var s = plan.Structures[i];
            return plan.Floors[i] != CityFloor.Ocean && !IsWallLike(s)
                   && s is not (CityStructure.Fence or CityStructure.Mountain or CityStructure.BoundaryWall);
        }

        var stack = new Stack<int>();
        for (var start = 0; start < roomOf.Length; start++)
        {
            if (seen[start] || !Inside(start))
                continue;

            var tiles = new List<int>();
            var enclosed = true;
            seen[start] = true;
            stack.Push(start);
            while (stack.TryPop(out var i))
            {
                tiles.Add(i);
                var (x, y) = (i % size, i / size);
                foreach (var (dx, dy) in WfcWave.Directions)
                {
                    if (!plan.Contains(x + dx, y + dy))
                    {
                        enclosed = false;
                        continue;
                    }

                    var next = i + dy * size + dx;
                    if (!Inside(next))
                    {
                        enclosed &= IsWallLike(plan.Structures[next]);
                        continue;
                    }

                    if (seen[next])
                        continue;

                    seen[next] = true;
                    stack.Push(next);
                }
            }

            if (!enclosed || tiles.Count > MaxRoomTiles)
                continue;

            tiles.Sort();
            foreach (var i in tiles)
            {
                roomOf[i] = rooms.Count;
            }

            rooms.Add(tiles);
        }

        return (roomOf, rooms);
    }

    /// <summary>
    /// Lays cable under every solar panel and joins each row to the terminals charging the SMES bank, keeping it
    /// clear of the bank's output so the two only meet through the SMES units.
    /// </summary>
    /// <returns>The panels' cable and every tile beside it, where no other high-voltage cable may go.</returns>
    private static HashSet<int> WirePanels(CityPlan plan, SolarBank bank)
    {
        var size = plan.Size;
        var outputs = Around(plan, bank.Outputs.Select(t => t.Y * size + t.X));
        var (_, px, py, pw, ph) = plan.Landmarks.First(l => l.Kind == CityLandmark.SolarPlant);
        var bounds = (px, py, px + pw - 1, py + ph - 1);
        var terminals = bank.Terminals.Select(t => t.Y * size + t.X).ToList();
        var spread = new Spread(plan, bounds, terminals, i =>
        {
            var s = plan.Structures[i];
            if (outputs.Contains(i) || plan.Floors[i] == CityFloor.Ocean
                || s is CityStructure.Smes or CityStructure.Mountain or CityStructure.BoundaryWall)
            {
                return -1;
            }

            return s == CityStructure.SolarPanel ? 1 : 2;
        }, _ => true);

        var input = new HashSet<int>(terminals);
        var seen = new HashSet<int>();
        for (var y = py; y < py + ph; y++)
        {
            for (var x = px; x < px + pw; x++)
            {
                var start = y * size + x;
                if (plan.Structures[start] != CityStructure.SolarPanel || !seen.Add(start))
                    continue;

                // One row of panels, joined to the bank from its nearest panel.
                var row = new List<int> { start };
                for (var i = 0; i < row.Count; i++)
                {
                    foreach (var next in Neighbours(plan, row[i]))
                    {
                        if (plan.Structures[next] == CityStructure.SolarPanel && seen.Add(next))
                            row.Add(next);
                    }
                }

                input.UnionWith(row);
                var nearest = row.Where(spread.Reached).OrderBy(spread.Distance).FirstOrDefault(-1);
                if (nearest >= 0)
                    input.UnionWith(spread.PathTo(nearest));
            }
        }

        foreach (var i in input)
        {
            plan.Cables[i] |= CityCable.High;
        }

        return Around(plan, input);
    }

    /// <summary>
    /// Finds room by a region for its substation, on open ground where it blocks nobody's way, as near the
    /// region's edge as can be. A wealthy region's substation has an SMES beside it and the SMES a terminal
    /// beside that, all in a line.
    /// </summary>
    /// <returns>Where the grid's cable comes in, at the substation or the terminal, and the substation.</returns>
    private static (int Entry, int Substation)? PlaceSubstation(CityPlan plan, Region region, int[] roomOf, HashSet<int> nearInput)
    {
        var size = plan.Size;
        var (x0, y0, x1, y1) = Bounds(plan, region, RegionMargin);
        var length = region.Affluent ? 3 : 1;
        (int X, int Y, int Direction)? best = null;
        var bestScore = int.MaxValue;
        for (var y = y0; y <= y1; y++)
        {
            for (var x = x0; x <= x1; x++)
            {
                for (var d = 0; d < (region.Affluent ? 4 : 1); d++)
                {
                    var (dx, dy) = WfcWave.Directions[d];
                    var (ax, ay) = (dy, dx);
                    var clear = true;
                    // The line and a tile all round it must be open ground, and a tile further out free of other
                    // substations, so nothing is walled off and no two lines touch.
                    for (var along = -2; along <= length + 1 && clear; along++)
                    {
                        for (var across = -2; across <= 2 && clear; across++)
                        {
                            var (tx, ty) = (x + dx * along + ax * across, y + dy * along + ay * across);
                            if (!plan.Contains(tx, ty))
                            {
                                clear = false;
                                break;
                            }

                            var i = ty * size + tx;
                            var s = plan.Structures[i];
                            var outer = along < -1 || along > length || Math.Abs(across) > 1;
                            if (outer)
                                clear = s is not (CityStructure.Smes or CityStructure.Substation);
                            else
                            {
                                var onLine = across == 0 && along >= 0 && along < length;
                                clear = s == CityStructure.None && plan.Floors[i] != CityFloor.Ocean && roomOf[i] < 0
                                        && !nearInput.Contains(i) && plan.Cables[i] == CityCable.None
                                        && !(onLine && plan.Floors[i] == CityFloor.Asphalt);
                            }
                        }
                    }

                    if (!clear)
                        continue;

                    var (ex, ey) = (x + dx * (length - 1), y + dy * (length - 1));
                    var inside = Math.Min(Math.Min(x, ex) - region.X, Math.Min(y, ey) - region.Y);
                    inside = Math.Min(inside, region.X + region.W - 1 - Math.Max(x, ex));
                    inside = Math.Min(inside, region.Y + region.H - 1 - Math.Max(y, ey));
                    var score = inside < 0 ? 1000 - inside : inside;
                    if (score >= bestScore)
                        continue;

                    best = (x, y, d);
                    bestScore = score;
                }
            }
        }

        if (best is not { } spot)
            return null;

        var (bx, by, direction) = spot;

        var (sx, sy) = WfcWave.Directions[direction];
        var entry = by * size + bx;
        if (!region.Affluent)
        {
            plan.Structures[entry] = CityStructure.Substation;
            return (entry, entry);
        }

        var smes = entry + sy * size + sx;
        var substation = smes + sy * size + sx;
        plan.Fixtures.Add((CityFixture.Terminal, bx, by, direction));
        plan.Structures[smes] = CityStructure.Smes;
        plan.Structures[substation] = CityStructure.Substation;
        plan.Cables[smes] |= CityCable.High;
        plan.Cables[substation] |= CityCable.High;
        return (entry, substation);
    }

    /// <summary>
    /// Runs high-voltage cable from the SMES bank to every substation, or to the terminal of a wealthy region's
    /// SMES, by the cheapest way along the avenues' sidewalks.
    /// </summary>
    private static void WireGrid(CityPlan plan, SolarBank bank, List<Region> regions, (int Entry, int Substation)?[] stations,
        int[] roomOf, HashSet<int> nearInput)
    {
        var size = plan.Size;
        var ends = new HashSet<int>();
        var blocked = new HashSet<int>(nearInput);
        for (var r = 0; r < regions.Count; r++)
        {
            if (stations[r] is not { } station)
                continue;

            ends.Add(station.Entry);
            if (station.Entry == station.Substation)
                continue;

            // A wealthy region's own SMES output stays apart from the grid.
            var local = Around(plan, new[] { station.Substation, (station.Entry + station.Substation) / 2 });
            local.Remove(station.Entry);
            blocked.UnionWith(local);
        }

        var spread = new Spread(plan, (0, 0, size - 1, size - 1), bank.Outputs.Select(t => t.Y * size + t.X), i =>
        {
            var s = plan.Structures[i];
            if (blocked.Contains(i) || roomOf[i] >= 0 || plan.Floors[i] == CityFloor.Ocean)
                return -1;

            if (s is not (CityStructure.None or CityStructure.Lamp or CityStructure.Tree or CityStructure.Rock
                or CityStructure.Fence or CityStructure.Substation))
            {
                return -1;
            }

            var (x, y) = (i % size, i / size);
            return plan.Floors[i] == CityFloor.Asphalt ? 4
                : x % Pitch == 0 || y % Pitch == 0 ? 1
                : plan.Floors[i] == CityFloor.Sidewalk ? 2
                : 3;
        }, i => !ends.Contains(i));

        foreach (var end in ends)
        {
            if (!spread.Reached(end))
                continue;

            foreach (var i in spread.PathTo(end))
            {
                plan.Cables[i] |= CityCable.High;
            }
        }
    }

    /// <summary>What a step of medium-voltage cable costs: cheapest under sidewalks, dearest through buildings.</summary>
    private static int MediumCost(CityPlan plan, int[] roomOf, int i)
    {
        var s = plan.Structures[i];
        if (plan.Floors[i] == CityFloor.Ocean || s is CityStructure.Smes or CityStructure.Substation
            or CityStructure.Mountain or CityStructure.BoundaryWall)
        {
            return -1;
        }

        if (s == CityStructure.Door || roomOf[i] >= 0)
            return 12;

        if (IsWallLike(s))
            return -1;

        return plan.Floors[i] switch
        {
            CityFloor.Sidewalk => 1,
            CityFloor.Asphalt => 3,
            _ => 2,
        };
    }

    /// <summary>
    /// Joins rooms through their doors into buildings.
    /// </summary>
    /// <returns>Every building's rooms, and the doors out of each room.</returns>
    private static (List<List<int>> Buildings, List<(int Door, int Room)>[] Doors) BuildingsAndDoors(
        CityPlan plan, int[] roomOf, List<List<int>> rooms)
    {
        var doors = rooms.Select(_ => new List<(int Door, int Room)>()).ToArray();
        var joined = new CityBlockGenerator.UnionFind(rooms.Count);
        var size = plan.Size;
        for (var i = 0; i < plan.Structures.Length; i++)
        {
            if (plan.Structures[i] != CityStructure.Door)
                continue;

            var (x, y) = (i % size, i / size);
            foreach (var (dx, dy) in new[] { (1, 0), (0, 1) })
            {
                if (!plan.Contains(x - dx, y - dy) || !plan.Contains(x + dx, y + dy))
                    continue;

                var (a, b) = (roomOf[i - dy * size - dx], roomOf[i + dy * size + dx]);
                if (a < 0 || b < 0 || a == b)
                    continue;

                doors[a].Add((i, b));
                doors[b].Add((i, a));
                joined.Join(a, b);
            }
        }

        var buildings = Enumerable.Range(0, rooms.Count).GroupBy(joined.Find).Select(g => g.ToList()).ToList();
        return (buildings, doors);
    }

    /// <summary>
    /// Mounts an APC on the wall of a building wherever medium-voltage cable reaches it most cheaply, and runs
    /// low-voltage cable from it round the inside of every room, entering each room through a door.
    /// </summary>
    private static void WireBuilding(CityPlan plan, List<int> building, List<List<int>> rooms, int[] roomOf,
        List<(int Door, int Room)>[] doors, Spread feed, HashSet<int> apcs)
    {
        var size = plan.Size;
        (int Wall, int Room, int From, int Direction)? best = null;
        var bestCost = int.MaxValue;
        foreach (var room in building)
        {
            foreach (var tile in rooms[room])
            {
                for (var d = 0; d < 4; d++)
                {
                    var (dx, dy) = WfcWave.Directions[d];
                    var wall = tile + dy * size + dx;
                    if (!IsPlainWall(plan.Structures[wall]) || apcs.Contains(wall))
                        continue;

                    foreach (var from in Neighbours(plan, wall))
                    {
                        if (from == tile || !feed.Reached(from) || feed.Distance(from) >= bestCost)
                            continue;

                        best = (wall, tile, from, WfcWave.Opposite(d));
                        bestCost = feed.Distance(from);
                    }
                }
            }
        }

        if (best is not { } chosen)
            return;

        var (apc, entry, feedFrom, facing) = chosen;

        apcs.Add(apc);
        plan.Fixtures.Add((CityFixture.Apc, apc % size, apc / size, facing));
        plan.Cables[apc] |= CityCable.Medium | CityCable.Low;
        foreach (var i in feed.PathTo(feedFrom))
        {
            plan.Cables[i] |= CityCable.Medium;
        }

        // Room to room through the doors, outwards from the APC's room.
        var entries = new Dictionary<int, List<int>> { [roomOf[entry]] = new() { entry } };
        var queue = new Queue<int>();
        queue.Enqueue(roomOf[entry]);
        while (queue.TryDequeue(out var room))
        {
            foreach (var (door, next) in doors[room])
            {
                if (entries.ContainsKey(next))
                    continue;

                plan.Cables[door] |= CityCable.Low;
                var (dx, dy) = roomOf[door - 1] >= 0 && roomOf[door + 1] >= 0 ? (1, 0) : (0, 1);
                var step = dy * size + dx;
                entries[room].Add(roomOf[door - step] == room ? door - step : door + step);
                entries[next] = new List<int> { roomOf[door - step] == next ? door - step : door + step };
                queue.Enqueue(next);
            }
        }

        foreach (var (room, starts) in entries)
        {
            foreach (var i in Ring(plan, rooms[room], roomOf, room, starts))
            {
                plan.Cables[i] |= CityCable.Low;
            }
        }
    }

    /// <summary>
    /// The tiles of a room that run round it next to its walls, joined up with each other and with
    /// <paramref name="starts"/>, crossing the room where something stands in the way.
    /// </summary>
    private static HashSet<int> Ring(CityPlan plan, List<int> tiles, int[] roomOf, int room, List<int> starts)
    {
        var size = plan.Size;
        var edge = new HashSet<int>();
        foreach (var i in tiles)
        {
            var (x, y) = (i % size, i / size);
            var byWall = false;
            for (var dy = -1; dy <= 1 && !byWall; dy++)
            {
                for (var dx = -1; dx <= 1 && !byWall; dx++)
                {
                    byWall = roomOf[(y + dy) * size + x + dx] != room;
                }
            }

            if (byWall)
                edge.Add(i);
        }

        var cabled = new HashSet<int>();
        void Follow(int from)
        {
            var stack = new Stack<int>();
            stack.Push(from);
            cabled.Add(from);
            while (stack.TryPop(out var i))
            {
                foreach (var next in Neighbours(plan, i))
                {
                    if (edge.Contains(next) && cabled.Add(next))
                        stack.Push(next);
                }
            }
        }

        foreach (var start in starts)
        {
            edge.Add(start);
            if (!cabled.Contains(start))
                Follow(start);
        }

        // Any stretch of the ring not yet joined, such as round a pillar, is joined across the floor.
        while (edge.Any(i => !cabled.Contains(i)))
        {
            var from = new Dictionary<int, int>();
            var queue = new Queue<int>(cabled.OrderBy(i => i));
            foreach (var i in cabled)
            {
                from[i] = -1;
            }

            var found = -1;
            while (found < 0 && queue.TryDequeue(out var i))
            {
                foreach (var next in Neighbours(plan, i))
                {
                    if (roomOf[next] != room || from.ContainsKey(next))
                        continue;

                    from[next] = i;
                    if (edge.Contains(next))
                    {
                        found = next;
                        break;
                    }

                    queue.Enqueue(next);
                }
            }

            if (found < 0)
                break;

            for (var i = from[found]; i >= 0 && !cabled.Contains(i); i = from[i])
            {
                cabled.Add(i);
            }

            Follow(found);
        }

        return cabled;
    }

    /// <summary>A region's tiles and some way round them, kept on the map.</summary>
    private static (int X0, int Y0, int X1, int Y1) Bounds(CityPlan plan, Region region, int margin)
    {
        return (Math.Max(0, region.X - margin), Math.Max(0, region.Y - margin),
            Math.Min(plan.Size - 1, region.X + region.W - 1 + margin), Math.Min(plan.Size - 1, region.Y + region.H - 1 + margin));
    }

    private static IEnumerable<int> Neighbours(CityPlan plan, int i)
    {
        var size = plan.Size;
        var (x, y) = (i % size, i / size);
        foreach (var (dx, dy) in WfcWave.Directions)
        {
            if (plan.Contains(x + dx, y + dy))
                yield return i + dy * size + dx;
        }
    }

    /// <summary>The tiles given and those beside them.</summary>
    private static HashSet<int> Around(CityPlan plan, IEnumerable<int> tiles)
    {
        var around = new HashSet<int>();
        foreach (var i in tiles)
        {
            around.Add(i);
            around.UnionWith(Neighbours(plan, i));
        }

        return around;
    }

    /// <summary>
    /// The cheapest ways out from some tiles to the rest of a rectangle. Stepping onto a tile costs what
    /// <c>cost</c> says, or can't be done where it says -1; a tile <c>through</c> refuses can be reached but not
    /// passed through.
    /// </summary>
    private sealed class Spread
    {
        private readonly int _size;
        private readonly (int X0, int Y0, int X1, int Y1) _bounds;
        private readonly int[] _distance;
        private readonly int[] _from;

        public Spread(CityPlan plan, (int X0, int Y0, int X1, int Y1) bounds, IEnumerable<int> sources,
            Func<int, int> cost, Func<int, bool> through)
        {
            _size = plan.Size;
            _bounds = bounds;
            var width = bounds.X1 - bounds.X0 + 1;
            _distance = new int[width * (bounds.Y1 - bounds.Y0 + 1)];
            _from = new int[_distance.Length];
            Array.Fill(_distance, int.MaxValue);
            var queue = new PriorityQueue<int, int>();
            foreach (var source in sources)
            {
                if (Local(source) is not { } local)
                    continue;

                _distance[local] = 0;
                _from[local] = -1;
                queue.Enqueue(source, 0);
            }

            while (queue.TryDequeue(out var i, out var distance))
            {
                var here = Local(i)!.Value;
                if (distance > _distance[here] || distance > 0 && !through(i))
                    continue;

                foreach (var next in Neighbours(plan, i))
                {
                    if (Local(next) is not { } there || cost(next) is var step && step < 0)
                        continue;

                    if (distance + step >= _distance[there])
                        continue;

                    _distance[there] = distance + step;
                    _from[there] = i;
                    queue.Enqueue(next, distance + step);
                }
            }
        }

        private int? Local(int i)
        {
            var (x, y) = (i % _size, i / _size);
            if (x < _bounds.X0 || y < _bounds.Y0 || x > _bounds.X1 || y > _bounds.Y1)
                return null;

            return (y - _bounds.Y0) * (_bounds.X1 - _bounds.X0 + 1) + x - _bounds.X0;
        }

        public bool Reached(int i)
        {
            return Local(i) is { } local && _distance[local] != int.MaxValue;
        }

        public int Distance(int i)
        {
            return _distance[Local(i)!.Value];
        }

        /// <summary>The tiles from one reached back to where the spread started.</summary>
        public IEnumerable<int> PathTo(int i)
        {
            for (; i >= 0; i = _from[Local(i)!.Value])
            {
                yield return i;
            }
        }
    }
}
