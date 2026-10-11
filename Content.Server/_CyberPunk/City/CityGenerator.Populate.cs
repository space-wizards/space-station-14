using System.Linq;
using Content.Server._CyberPunk.Procgen;

namespace Content.Server._CyberPunk.City;

/// <summary>
/// Populates residences: each building whose rooms make a home gets furniture, one router, a programmable or
/// security computer, and data cable joining them. Where a room's furniture stands is a Wave Function Collapse
/// pass over its tiles: every tile is empty, free-standing furniture, furniture backed against a wall, or a
/// chair or table asking for the other across an edge. Doors keep a tile of floor clear, nothing blocks a
/// doorway, and every residence keeps at least one bed whatever the collapse leaves behind. The bedroom dresses
/// around its bed — an end table beside it, a dresser in the room, carpet under it and curtains on the windows
/// — all in the home's colour, shared with the carpet inside the entry door.
/// </summary>
/// <remarks>
/// The pass reuses the rooms the power wiring found, groups them into buildings by their doors, and keeps to the
/// residential zones: the middle and upper classes' districts and the shanty town, plus the townships out in the
/// badlands. Every other building, landmarks included, is left empty for now.
/// </remarks>
public static partial class CityGenerator
{
    /// <summary>The zones whose buildings people live in.</summary>
    private static readonly HashSet<CityZone> Residential = new()
    {
        CityZone.HighClass, CityZone.MediumClass, CityZone.LowClass, CityZone.Shanty,
    };

    /// <summary>How a tile's edge looks to its neighbour, at each side.</summary>
    private enum InteriorSocket : byte
    {
        /// <summary>Open floor; anything may sit across this edge.</summary>
        Front,
        /// <summary>The furniture stands against a wall on this side.</summary>
        Backing,
        /// <summary>A chair facing across this edge, wanting a table.</summary>
        SeatWant,
        /// <summary>A table's side a chair may sit across.</summary>
        SeatGive,
    }

    private readonly record struct InteriorTile(CityObject Object, InteriorSocket[] Sockets, int Weight, int Facing);

    private enum ResidenceTier : byte
    {
        Rich,
        Medium,
        Poor,
    }

    /// <summary>
    /// How much each piece of furniture weighs, rich, medium and poor. Bare floor weighs a thousand, so a
    /// room's whole furniture budget stays a fraction of its floor: a home keeps its rooms mostly room.
    /// </summary>
    private static readonly Dictionary<CityObject, int[]> ObjectWeights = new()
    {
        [CityObject.Bed] = new[] { 57, 44, 9 },
        [CityObject.Mattress] = new[] { 0, 7, 31 },
        [CityObject.Sofa] = new[] { 40, 31, 0 },
        [CityObject.Table] = new[] { 40, 30, 20 },
        [CityObject.Chair] = new[] { 30, 22, 15 },
        [CityObject.Stool] = new[] { 25, 20, 15 },
        [CityObject.Dresser] = new[] { 33, 25, 0 },
        [CityObject.Bookshelf] = new[] { 33, 25, 0 },
        [CityObject.Sink] = new[] { 40, 31, 22 },
        [CityObject.Toilet] = new[] { 33, 25, 18 },
        [CityObject.Plant] = new[] { 20, 15, 6 },
    };

    /// <summary>How heavy bare floor is; the same for every tier, so the tier lives in the furniture.</summary>
    private const int EmptyWeight = 1000;

    /// <summary>How many of each piece a home may hold, however the collapse leans: the big pieces are one
    /// to a home, the small ones two. A kind left out has no cap.</summary>
    private static readonly Dictionary<CityObject, int> ObjectCaps = new()
    {
        [CityObject.Bed] = 1,
        [CityObject.Mattress] = 1,
        [CityObject.Sofa] = 1,
        [CityObject.Dresser] = 1,
        [CityObject.Bookshelf] = 1,
        [CityObject.Toilet] = 1,
        [CityObject.Plant] = 1,
        [CityObject.Sink] = 2,
        [CityObject.Table] = 1,
        [CityObject.Chair] = 2,
        [CityObject.Stool] = 2,
    };

    /// <summary>The colours the cloth-dyers of the city stock: every colour the carpets and curtains come in.</summary>
    private static readonly CityCarpet[] CarpetColours =
    {
        CityCarpet.Red, CityCarpet.Black, CityCarpet.Pink, CityCarpet.Blue, CityCarpet.Green,
        CityCarpet.Orange, CityCarpet.SkyBlue, CityCarpet.Purple, CityCarpet.Cyan, CityCarpet.White,
    };

    private static readonly HashSet<CityObject> LivingFurniture = new()
    {
        CityObject.Sofa, CityObject.Table, CityObject.Chair, CityObject.Stool, CityObject.Plant, CityObject.Bookshelf,
    };

    private static readonly HashSet<CityObject> BedroomFurniture = new()
    {
        CityObject.Bed, CityObject.Mattress, CityObject.Dresser, CityObject.Bookshelf, CityObject.Plant,
    };

    private static readonly HashSet<CityObject> KitchenFurniture = new()
    {
        CityObject.Sink, CityObject.Table, CityObject.Stool,
    };

    private static readonly HashSet<CityObject> BathFurniture = new()
    {
        CityObject.Toilet, CityObject.Sink,
    };

    /// <summary>What a bachelor pad's one room holds: everything at once.</summary>
    private static readonly HashSet<CityObject> BachelorFurniture = new()
    {
        CityObject.Bed, CityObject.Mattress, CityObject.Sofa, CityObject.Table, CityObject.Chair, CityObject.Stool,
        CityObject.Dresser, CityObject.Bookshelf, CityObject.Sink, CityObject.Toilet, CityObject.Plant,
    };

    /// <summary>The interior pass's tiles and rules, one set per tier, made on first use.</summary>
    private static readonly Lazy<(InteriorTile[] Tiles, WfcRules Rules)>[] InteriorRulesByTier =
    {
        new(() => BuildInteriorRules(ResidenceTier.Rich)),
        new(() => BuildInteriorRules(ResidenceTier.Medium)),
        new(() => BuildInteriorRules(ResidenceTier.Poor)),
    };

    /// <summary>How many tries the furniture pass gets before falling back to beds only.</summary>
    private const int FurnishAttempts = 20;

    private static bool IsWindow(CityStructure structure)
    {
        return structure is CityStructure.Window or CityStructure.WindowReinforced or CityStructure.WindowTinted;
    }

    /// <summary>A wall or a window: something furniture may stand against, but not a door or a girder.</summary>
    private static bool IsBackWall(CityStructure structure)
    {
        return structure is >= CityStructure.Wall and <= CityStructure.WallWood
            || structure is CityStructure.WindowReinforced or CityStructure.WindowTinted or CityStructure.Window;
    }

    private static bool InteriorFits(InteriorSocket a, InteriorSocket b)
    {
        return a == b
            || (a, b) is (InteriorSocket.SeatWant, InteriorSocket.SeatGive)
                or (InteriorSocket.SeatGive, InteriorSocket.SeatWant)
                or (InteriorSocket.SeatGive, InteriorSocket.Front)
                or (InteriorSocket.Front, InteriorSocket.SeatGive);
    }

    /// <summary>
    /// The interior pass's tiles: bare floor, free-standing furniture, a table with one side a chair may take,
    /// chairs facing one way, and wall-backed furniture facing one way, all with their sockets and weights.
    /// </summary>
    private static (InteriorTile[] Tiles, WfcRules Rules) BuildInteriorRules(ResidenceTier tier)
    {
        var t = (int) tier;
        var tiles = new List<InteriorTile>();

        // Bare floor, and the free-standing furniture.
        tiles.Add(new InteriorTile(CityObject.None, Sides(InteriorSocket.Front), EmptyWeight, 0));
        foreach (var o in new[] { CityObject.Stool, CityObject.Plant })
        {
            tiles.Add(new InteriorTile(o, Sides(InteriorSocket.Front), ObjectWeights[o][t], 0));
        }

        // A table with one side free for a chair, and a chair facing one way, wanting a table across its
        // front. Four facings a piece, so each carries a quarter of the piece's weight.
        for (var d = 0; d < 4; d++)
        {
            tiles.Add(new InteriorTile(CityObject.Table, Sides(InteriorSocket.Front, InteriorSocket.SeatGive, d),
                ObjectWeights[CityObject.Table][t] / 4, d));
            tiles.Add(new InteriorTile(CityObject.Chair, Sides(InteriorSocket.SeatWant, InteriorSocket.Front, d),
                ObjectWeights[CityObject.Chair][t] / 4, d));
        }

        // Furniture against a wall, facing into the room: four facings a piece, a quarter of the weight each.
        foreach (var o in new[]
                 {
                     CityObject.Bed, CityObject.Mattress, CityObject.Sofa, CityObject.Dresser, CityObject.Bookshelf,
                     CityObject.Sink, CityObject.Toilet,
                 })
        {
            for (var d = 0; d < 4; d++)
            {
                tiles.Add(new InteriorTile(o, Sides(InteriorSocket.Backing, InteriorSocket.Front, d),
                    ObjectWeights[o][t] / 4, WfcWave.Opposite(d)));
            }
        }

        var array = tiles.ToArray();
        var rules = new WfcRules(array.Select(tile => tile.Weight).ToArray(),
            (a, direction, b) => InteriorFits(array[a].Sockets[direction], array[b].Sockets[WfcWave.Opposite(direction)]));
        return (array, rules);
    }

    /// <summary>Four sockets all of one kind, or <paramref name="kind"/> on side <paramref name="at"/>.</summary>
    private static InteriorSocket[] Sides(InteriorSocket kind, InteriorSocket? other = null, int at = 0)
    {
        var sockets = new[] { kind, kind, kind, kind };
        if (other is { } o)
            sockets[at] = o;
        return sockets;
    }

    /// <summary>The zone whose district a tile stands in, or null outside every district.</summary>
    private static CityZone? DistrictZone(CityPlan plan, int tile)
    {
        var (x, y) = (tile % plan.Size, tile / plan.Size);
        if (x < Avenue || y < Avenue)
            return null;

        var (dx, mx) = Math.DivRem(x - Avenue, Pitch);
        var (dy, my) = Math.DivRem(y - Avenue, Pitch);
        if (mx >= DistrictSize || my >= DistrictSize || dx >= plan.Districts || dy >= plan.Districts)
            return null;

        return plan.Zone(dx, dy);
    }

    /// <summary>Whether a tile stands inside a landmark's area.</summary>
    private static bool InsideLandmark(CityPlan plan, int tile, CityLandmark kind)
    {
        var (x, y) = (tile % plan.Size, tile / plan.Size);
        return plan.Landmarks.Any(l => l.Kind == kind && x >= l.X && y >= l.Y && x < l.X + l.W && y < l.Y + l.H);
    }

    /// <summary>Whether a building is somewhere people live: a residential zone's block, or a township shack.</summary>
    public static bool IsResidence(CityPlan plan, List<int> building, List<List<int>> rooms)
    {
        var tile = rooms[building[0]][0];
        if (InsideLandmark(plan, tile, CityLandmark.Township))
            return true;

        var zone = DistrictZone(plan, tile);
        return zone is { } z && Residential.Contains(z)
            && !plan.Landmarks.Any(l => l.Kind != CityLandmark.Township
                && tile % plan.Size >= l.X && tile / plan.Size >= l.Y
                && tile % plan.Size < l.X + l.W && tile / plan.Size < l.Y + l.H);
    }

    /// <summary>
    /// Every residence as the flat list of tiles in its rooms: the homes the interior pass fills, for tests and
    /// other passes to look at.
    /// </summary>
    public static List<List<int>> Residences(CityPlan plan)
    {
        return ResidenceRooms(plan).Select(r => r.SelectMany(t => t).ToList()).ToList();
    }

    /// <summary>
    /// Every residence as its rooms: the homes the interior pass fills, for tests and other passes to look at.
    /// </summary>
    public static List<List<List<int>>> ResidenceRooms(CityPlan plan)
    {
        var (roomOf, rooms) = FindRooms(plan);
        var (buildings, _) = BuildingsAndDoors(plan, roomOf, rooms);
        return buildings
            .Where(b => IsResidence(plan, b, rooms))
            .Select(b => b.Select(r => rooms[r]).ToList())
            .ToList();
    }

    /// <summary>
    /// Fills the residences with furniture, a router and a computer, and data cable between them.
    /// </summary>
    public static void Populate(CityPlan plan, CyberRng rng)
    {
        var (roomOf, rooms) = FindRooms(plan);
        var (buildings, _) = BuildingsAndDoors(plan, roomOf, rooms);

        var index = 0UL;
        foreach (var building in buildings)
        {
            if (!IsResidence(plan, building, rooms))
                continue;

            PopulateResidence(plan, roomOf, rooms, building, rng.Fork(index++));
        }
    }

    private static void PopulateResidence(CityPlan plan, int[] roomOf, List<List<int>> rooms,
        List<int> building, CyberRng rng)
    {
        var size = plan.Size;
        var zone = DistrictZone(plan, rooms[building[0]][0]);
        var tier = zone switch
        {
            CityZone.HighClass => ResidenceTier.Rich,
            CityZone.MediumClass => ResidenceTier.Medium,
            _ => ResidenceTier.Poor,
        };

        // The living room is the biggest room that looks outside; the bedroom the biggest left; then the kitchen
        // and the bathroom. A one-room home does everything at once.
        var ordered = building.OrderByDescending(r => rooms[r].Count).ToList();
        bool HasWindow(int room)
        {
            return rooms[room].Any(tile => Neighbours(plan, tile).Any(n => IsWindow(plan.Structures[n])));
        }

        var living = ordered.FindIndex(HasWindow) is { } windowed && windowed >= 0
            ? ordered[windowed]
            : ordered[0];
        var roles = new Dictionary<int, HashSet<CityObject>>();
        HashSet<CityObject> Merge(params HashSet<CityObject>[] sets)
        {
            var merged = new HashSet<CityObject>();
            foreach (var set in sets)
            {
                merged.UnionWith(set);
            }

            return merged;
        }

        if (building.Count == 1)
        {
            roles[living] = BachelorFurniture;
        }
        else
        {
            roles[living] = LivingFurniture;
            ordered.Remove(living);
            roles[ordered[0]] = building.Count == 2
                ? Merge(BedroomFurniture, BathFurniture)
                : BedroomFurniture;
            if (building.Count >= 3)
                roles[ordered[1]] = KitchenFurniture;
            if (building.Count >= 4)
                roles[ordered[2]] = BathFurniture;
        }

        // The router and the computer stand against the living room's walls, or failing that somewhere else in
        // the building. The security computer's reader keeps ICE, so only the guarded few get one.
        var security = zone is { } z && Affluent.Contains(z) ? rng.Chance(1, 10) : rng.Chance(1, 20);
        var computerKind = security ? CityObject.SecurityComputer : CityObject.Computer;
        var routerRoom = rooms[living];
        var routerTile = PickDeviceSpot(plan, building, rooms, routerRoom, rng, -1);
        var computerTile = PickDeviceSpot(plan, building, rooms, routerRoom, rng, routerTile);
        if (routerTile < 0 || computerTile < 0)
            return;

        var devices = new Dictionary<int, (CityObject Kind, int Facing)>
        {
            [routerTile] = (CityObject.Router, DeviceFacing(plan, routerTile, size)),
            [computerTile] = (computerKind, DeviceFacing(plan, computerTile, size)),
        };

        foreach (var (tile, (kind, facing)) in devices)
        {
            plan.Objects.Add((kind, tile % size, tile / size, facing));
            plan.Cables[tile] |= CityCable.Low;
        }

        Cable(plan, routerTile, computerTile);

        // The furniture is a WFC pass over the building's tiles, with the devices' tiles held bare.
        var placed = Furnish(plan, roomOf, rooms, building, roles, ordered, devices, tier, rng);

        // The home's colour, shared by its carpets and its curtains. The bedroom — wherever the bed ended up —
        // gets an end table beside the bed, a dresser of its own, carpet under the bed and curtains on its
        // windows; the entry door gets a mat of the same cloth.
        var colour = rng.Pick(CarpetColours);
        var bedTile = placed.Keys.OrderBy(t => t)
            .FirstOrDefault(t => placed[t].Kind is CityObject.Bed or CityObject.Mattress, -1);
        if (bedTile >= 0)
        {
            PlaceEndTable(plan, rooms, building, placed, bedTile, rng);
            PlaceDresser(plan, roomOf, rooms, building, placed, bedTile, rng);
            CarpetBed(plan, rooms, building, bedTile, colour, rng);
            HangCurtains(plan, roomOf, rooms, bedTile, colour);
        }

        LayEntryCarpet(plan, rooms, building, colour);

        foreach (var (tile, (kind, facing)) in placed)
        {
            // The router and computer went out already; only the furniture is new here.
            if (devices.ContainsKey(tile))
                continue;

            plan.Objects.Add((kind, tile % size, tile / size, facing));
        }
    }

    /// <summary>Whether a floor tile stands next to a door, where furniture keeps clear.</summary>
    private static bool ByADoor(CityPlan plan, int tile)
    {
        return Neighbours(plan, tile).Any(n => plan.Structures[n] == CityStructure.Door);
    }

    /// <summary>Whether a piece of furniture may be moved for: the bed and the devices stand where they stand.</summary>
    private static bool Evictable(CityObject kind)
    {
        return kind is not (CityObject.Bed or CityObject.Mattress
            or CityObject.Router or CityObject.Computer or CityObject.SecurityComputer
            or CityObject.EndTable or CityObject.Dresser);
    }

    /// <summary>
    /// Puts an end table beside the bed: beside it or in front of it, clear of the doors where there's room and
    /// anywhere free when there isn't, facing the way the bed does.
    /// </summary>
    private static void PlaceEndTable(CityPlan plan, List<List<int>> rooms, List<int> building,
        Dictionary<int, (CityObject Kind, int Facing)> placed, int bedTile, CyberRng rng)
    {
        var size = plan.Size;
        var inBuilding = new HashSet<int>(building.SelectMany(r => rooms[r]));
        var facing = placed[bedTile].Facing;

        bool Spot(int tile, bool clear)
        {
            if (!plan.Contains(tile % size, tile / size)
                || !inBuilding.Contains(tile)
                || plan.Structures[tile] != CityStructure.None
                || placed.TryGetValue(tile, out var there) && !Evictable(there.Kind))
                return false;

            return !clear || !ByADoor(plan, tile);
        }

        // Beside the bed: either side of the way it faces, then in front of it.
        int At(int d) => bedTile + WfcWave.Directions[d].Item2 * size + WfcWave.Directions[d].Item1;
        var spots = new[] { (facing + 1) % 4, (facing + 3) % 4, facing }.Select(At).ToList();
        var candidates = spots.Where(t => Spot(t, clear: true)).ToList();
        if (candidates.Count == 0)
            candidates = spots.Where(t => Spot(t, clear: false)).ToList();

        if (candidates.Count > 0)
        {
            rng.Shuffle(candidates);
            placed[candidates[0]] = (CityObject.EndTable, facing);
        }
    }

    /// <summary>
    /// Where there's a bed there's a dresser somewhere in the same room: against a wall, clear of the doors
    /// where there's room and anywhere free when there isn't; if the collapse left none, one stands by hand.
    /// </summary>
    private static void PlaceDresser(CityPlan plan, int[] roomOf, List<List<int>> rooms, List<int> building,
        Dictionary<int, (CityObject Kind, int Facing)> placed, int bedTile, CyberRng rng)
    {
        var size = plan.Size;
        var bedroom = roomOf[bedTile];
        if (placed.Any(p => roomOf[p.Key] == bedroom && p.Value.Kind == CityObject.Dresser))
            return;

        bool Spot(int tile, bool clear)
        {
            if (plan.Structures[tile] != CityStructure.None
                || placed.TryGetValue(tile, out var there) && !Evictable(there.Kind))
                return false;

            return !clear || !ByADoor(plan, tile);
        }

        // A dresser against the bedroom's walls; failing that, anywhere in the room the collapse left reachable.
        var backed = new List<(int Tile, int Facing)>();
        var free = new List<int>();
        foreach (var tile in rooms[bedroom])
        {
            if (placed.TryGetValue(tile, out var there) && !Evictable(there.Kind))
                continue;

            free.Add(tile);
            var (x, y) = (tile % size, tile / size);
            for (var d = 0; d < 4; d++)
            {
                var (dx, dy) = WfcWave.Directions[d];
                if (!plan.Contains(x + dx, y + dy) || !IsBackWall(plan.Structures[tile + dy * size + dx]))
                    continue;

                backed.Add((tile, WfcWave.Opposite(d)));
                break;
            }
        }

        var candidates = backed.Where(p => Spot(p.Tile, clear: true)).ToList();
        if (candidates.Count == 0)
            candidates = backed.Where(p => Spot(p.Tile, clear: false)).ToList();
        if (candidates.Count == 0)
            candidates = free.Where(t => Spot(t, clear: false))
                .Select(t => (t, DeviceFacing(plan, t, size))).ToList();

        if (candidates.Count > 0)
        {
            rng.Shuffle(candidates);
            var (tile, facing) = candidates[0];
            placed[tile] = (CityObject.Dresser, facing);
        }
    }

    /// <summary>
    /// Carpets the bedroom under the bed in the home's colour, and half the time around it too: a rug a bed can
    /// sit in the middle of.
    /// </summary>
    private static void CarpetBed(CityPlan plan, List<List<int>> rooms, List<int> building,
        int bedTile, CityCarpet colour, CyberRng rng)
    {
        var size = plan.Size;
        var inBuilding = new HashSet<int>(building.SelectMany(r => rooms[r]));

        void Lay(int x, int y)
        {
            if (!plan.Contains(x, y))
                return;

            var tile = y * size + x;
            if (!inBuilding.Contains(tile) || plan.Structures[tile] != CityStructure.None
                || plan.Carpets.Any(c => c.X == x && c.Y == y))
                return;

            plan.Carpets.Add((colour, x, y));
        }

        var (bx, by) = (bedTile % size, bedTile / size);
        Lay(bx, by);
        if (!rng.Chance(1, 2))
            return;

        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                Lay(bx + dx, by + dy);
            }
        }
    }

    /// <summary>Hangs curtains in the home's colour over every window of the bedroom.</summary>
    private static void HangCurtains(CityPlan plan, int[] roomOf, List<List<int>> rooms, int bedTile, CityCarpet colour)
    {
        var size = plan.Size;
        var bedroom = roomOf[bedTile];
        var hung = new HashSet<int>();
        foreach (var tile in rooms[bedroom])
        {
            var (x, y) = (tile % size, tile / size);
            foreach (var (dx, dy) in WfcWave.Directions)
            {
                if (!plan.Contains(x + dx, y + dy))
                    continue;

                var window = (y + dy) * size + x + dx;
                if (!IsWindow(plan.Structures[window]) || !hung.Add(window))
                    continue;

                plan.Curtains.Add((colour, x + dx, y + dy));
            }
        }
    }

    /// <summary>
    /// Lays a carpet inside the first door out of the building: three tiles wide along the wall the door sits
    /// on, two tiles deep where the room allows it, one where it doesn't, and a single tile where even that
    /// won't fit.
    /// </summary>
    private static void LayEntryCarpet(CityPlan plan, List<List<int>> rooms, List<int> building, CityCarpet colour)
    {
        var size = plan.Size;
        var inBuilding = new HashSet<int>(building.SelectMany(r => rooms[r]));

        bool Free(int x, int y)
        {
            return plan.Contains(x, y) && inBuilding.Contains(y * size + x)
                && plan.Structures[y * size + x] == CityStructure.None
                && !plan.Carpets.Any(c => c.X == x && c.Y == y);
        }

        foreach (var door in ExteriorDoors(plan, rooms, building))
        {
            var (dx0, dy0) = (door % size, door / size);
            foreach (var (ux, uy) in new[] { (1, 0), (0, 1) })
            {
                if (!plan.Contains(dx0 - ux, dy0 - uy) || !plan.Contains(dx0 + ux, dy0 + uy))
                    continue;

                var a = (dy0 - uy) * size + dx0 - ux;
                var b = (dy0 + uy) * size + dx0 + ux;
                var aIn = inBuilding.Contains(a);
                if (aIn == inBuilding.Contains(b))
                    continue; // the door joins two rooms of the building, or nothing at all

                // The tile inside the door, the way deeper into the room, and along the wall the door sits on.
                var (ix, iy) = aIn ? (dx0 - ux, dy0 - uy) : (dx0 + ux, dy0 + uy);
                var (dxIn, dyIn) = (ix - dx0, iy - dy0);
                var (wx, wy) = uy == 0 ? (0, 1) : (1, 0);
                var row = new[] { (ix, iy), (ix + wx, iy + wy), (ix - wx, iy - wy) };

                if (!row.All(t => Free(t.Item1, t.Item2)))
                {
                    if (Free(ix, iy))
                        plan.Carpets.Add((colour, ix, iy));

                    continue;
                }

                foreach (var (x, y) in row)
                {
                    plan.Carpets.Add((colour, x, y));
                }

                // A second row deep, when the entry is big enough for one.
                var deep = row.Select(t => (t.Item1 + dxIn, t.Item2 + dyIn)).ToArray();
                if (!deep.All(t => Free(t.Item1, t.Item2)))
                    return;

                foreach (var (x, y) in deep)
                {
                    plan.Carpets.Add((colour, x, y));
                }

                return;
            }
        }
    }

    /// <summary>Every door out of the building: one whose far side isn't part of it.</summary>
    private static List<int> ExteriorDoors(CityPlan plan, List<List<int>> rooms, List<int> building)
    {
        var size = plan.Size;
        var inBuilding = new HashSet<int>(building.SelectMany(r => rooms[r]));
        var doors = new HashSet<int>();
        foreach (var i in inBuilding)
        {
            var (x, y) = (i % size, i / size);
            foreach (var (dx, dy) in WfcWave.Directions)
            {
                if (!plan.Contains(x + dx, y + dy) || plan.Structures[(y + dy) * size + x + dx] != CityStructure.Door)
                    continue;

                // The door's passage runs the way we stepped; its far side is one tile beyond it.
                var door = (y + dy) * size + x + dx;
                var far = door + dy * size + dx;
                if (!plan.Contains(x + 2 * dx, y + 2 * dy) || !inBuilding.Contains(far))
                    doors.Add(door);
            }
        }

        return doors.OrderBy(d => d).ToList();
    }

    /// <summary>Which way a device placed at a tile faces: away from the wall or window behind it.</summary>
    private static int DeviceFacing(CityPlan plan, int tile, int size)
    {
        var (x, y) = (tile % size, tile / size);
        for (var d = 0; d < 4; d++)
        {
            var (dx, dy) = WfcWave.Directions[d];
            if (!plan.Contains(x + dx, y + dy) || IsBackWall(plan.Structures[tile + dy * size + dx]))
                return WfcWave.Opposite(d);
        }

        return 0;
    }

    /// <summary>
    /// A floor tile for a router or a computer: against a wall, keeping its floor clear of the doors. The
    /// living room's tiles first, then the rest of the building's; failing that, any floor tile at all.
    /// </summary>
    /// <returns>The tile, or -1 when nowhere in the building fits.</returns>
    private static int PickDeviceSpot(CityPlan plan, List<int> building, List<List<int>> rooms,
        List<int> firstRoom, CyberRng rng, int taken)
    {
        var size = plan.Size;
        var inBuilding = new HashSet<int>(building.SelectMany(r => rooms[r]));
        var candidates = new List<(int Tile, int Facing)>();
        var relaxed = new List<int>();
        foreach (var room in new[] { -1 }.Concat(building))
        {
            foreach (var tile in room < 0 ? firstRoom : rooms[room])
            {
                if (tile == taken || !inBuilding.Contains(tile) || relaxed.Contains(tile))
                    continue;

                var (x, y) = (tile % size, tile / size);
                var byDoor = Neighbours(plan, tile).Any(n => plan.Structures[n] == CityStructure.Door);
                if (byDoor)
                {
                    relaxed.Add(tile);
                    continue;
                }

                candidates.Add((tile, DeviceFacing(plan, tile, size)));
            }

            if (candidates.Count > 0)
                break;
        }

        // A home so small that every tile touches a door still gets its device.
        var spots = candidates.Count > 0
            ? candidates.Select(c => c.Tile).ToList()
            : relaxed;
        if (spots.Count == 0)
            return -1;

        rng.Shuffle(spots);
        return spots[0];
    }

    /// <summary>
    /// Runs data cable from the computer to the router through the building's floor, under doors where rooms
    /// meet, and marks the low-voltage cable both devices need under themselves.
    /// </summary>
    private static void Cable(CityPlan plan, int router, int computer)
    {
        var size = plan.Size;
        var from = new Dictionary<int, int> { [computer] = -1 };
        var queue = new Queue<int>();
        queue.Enqueue(computer);
        while (queue.TryDequeue(out var i) && !from.ContainsKey(router))
        {
            foreach (var next in Neighbours(plan, i))
            {
                if (from.ContainsKey(next))
                    continue;

                var s = plan.Structures[next];
                if (s != CityStructure.None && s != CityStructure.Door)
                    continue;

                from[next] = i;
                queue.Enqueue(next);
            }
        }

        if (!from.ContainsKey(router))
            return;

        for (var i = router; i >= 0; i = from[i])
        {
            plan.Cables[i] |= CityCable.Data;
        }
    }

    /// <summary>
    /// The furniture pass: a WFC wave over the building's tiles, each cell's options its room's role's furniture
    /// that fits its walls and doors. Bare floor holds the devices' tiles, and wherever the collapse leaves no
    /// bed, one is put back against the bedroom wall by hand. Returns where every piece stands, devices included.
    /// </summary>
    private static Dictionary<int, (CityObject Kind, int Facing)> Furnish(CityPlan plan, int[] roomOf, List<List<int>> rooms, List<int> building,
        Dictionary<int, HashSet<CityObject>> roles, List<int> ordered, Dictionary<int, (CityObject Kind, int Facing)> devices,
        ResidenceTier tier, CyberRng rng)
    {
        var size = plan.Size;
        var cells = building.SelectMany(r => rooms[r]).ToList();
        var minX = cells.Min(i => i % size);
        var minY = cells.Min(i => i / size);
        var maxX = cells.Max(i => i % size);
        var maxY = cells.Max(i => i / size);
        var (w, h) = (maxX - minX + 1, maxY - minY + 1);
        var inBuilding = new HashSet<int>(cells);
        var (tiles, rules) = InteriorRulesByTier[(int) tier].Value;
        var empty = Array.FindIndex(tiles, t => t.Object == CityObject.None);
        var placed = new Dictionary<int, (CityObject Kind, int Facing)>();
        foreach (var (tile, device) in devices)
        {
            placed[tile] = device;
        }

        for (var attempt = 0UL; attempt < FurnishAttempts; attempt++)
        {
            var wave = new WfcWave(w, h, WfcSet.Single(empty));
            foreach (var tile in cells)
            {
                var (x, y) = (tile % size, tile / size);
                if (devices.ContainsKey(tile))
                    continue;

                var options = new WfcSet();
                var role = roles.GetValueOrDefault(roomOf[tile], new HashSet<CityObject>());
                for (var t = 0; t < tiles.Length; t++)
                {
                    // Bare floor is always an option; the role picks the furniture.
                    if (rules.Weights[t] <= 0
                        || tiles[t].Object != CityObject.None && !role.Contains(tiles[t].Object))
                        continue;

                    var fits = true;
                    for (var d = 0; d < 4 && fits; d++)
                    {
                        var (dx, dy) = WfcWave.Directions[d];
                        var nx = x + dx;
                        var ny = y + dy;
                        if (!plan.Contains(nx, ny))
                        {
                            fits = tiles[t].Sockets[d] != InteriorSocket.SeatWant
                                && tiles[t].Sockets[d] != InteriorSocket.SeatGive;
                            continue;
                        }

                        var n = ny * size + nx;
                        if (inBuilding.Contains(n))
                        {
                            // Backing stands against walls, so it can't face another cell of the building.
                            fits = tiles[t].Sockets[d] != InteriorSocket.Backing;
                        }
                        else if (plan.Structures[n] == CityStructure.Door)
                        {
                            // Doors keep the tile in front of them clear.
                            fits = tiles[t].Object == CityObject.None;
                        }
                        else
                        {
                            // Anything else out there is wall, window or ground: no seat across it.
                            fits = tiles[t].Sockets[d] is InteriorSocket.Front or InteriorSocket.Backing;
                        }
                    }

                    if (fits)
                        options.Insert(t);
                }

                wave.Set(x - minX, y - minY, options.IsEmpty ? WfcSet.Single(empty) : options);
            }

            var attemptRng = rng.Fork(attempt);
            if (wave.Solve(rules, ref attemptRng) is not { } solution)
                continue;

            foreach (var tile in cells)
            {
                var (x, y) = (tile % size, tile / size);
                var t = solution[(y - minY) * w + x - minX];
                if (tiles[t].Object == CityObject.None)
                    continue;

                placed[tile] = (tiles[t].Object, tiles[t].Facing);
            }

            break;
        }

        // However the collapse leans, a home holds one of each big piece and two of the small ones. A mattress
        // and a bed are the same piece of the home: somewhere to sleep.
        var counts = new Dictionary<CityObject, int>();
        foreach (var tile in placed.Keys.OrderBy(t => t).ToArray())
        {
            var kind = placed[tile].Kind;
            var capKey = kind == CityObject.Mattress ? CityObject.Bed : kind;
            if (!ObjectCaps.TryGetValue(capKey, out var cap))
                continue;

            counts.TryGetValue(capKey, out var n);
            if (n >= cap)
                placed.Remove(tile);
            else
                counts[capKey] = n + 1;
        }

        // Every home sleeps somewhere, collapse or no collapse.
        if (!placed.Values.Any(d => d.Kind is CityObject.Bed or CityObject.Mattress))
        {
            var bedroom = ordered.Count > 0 ? ordered[0] : building[0];
            var candidates = new List<(int Tile, int Facing)>();
            foreach (var tile in rooms[bedroom])
            {
                if (devices.ContainsKey(tile))
                    continue;

                var (x, y) = (tile % size, tile / size);
                for (var d = 0; d < 4; d++)
                {
                    var (dx, dy) = WfcWave.Directions[d];
                    if (!plan.Contains(x + dx, y + dy) || !IsBackWall(plan.Structures[tile + dy * size + dx]))
                        continue;

                    candidates.Add((tile, WfcWave.Opposite(d)));
                    break;
                }
            }

            if (candidates.Count > 0)
            {
                rng.Shuffle(candidates);
                var (tile, facing) = candidates[0];
                var kind = tier == ResidenceTier.Poor ? CityObject.Mattress : CityObject.Bed;
                placed[tile] = (kind, facing);
            }
        }

        return placed;
    }
}