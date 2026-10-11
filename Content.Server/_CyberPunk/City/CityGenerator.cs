using System.Linq;
using Content.Server._CyberPunk.Procgen;

namespace Content.Server._CyberPunk.City;

/// <summary>
/// Generates a coastal city with badlands round it. The same seed always gives the same city.
/// </summary>
/// <remarks>
/// <para>
/// The map is a square grid of districts with an avenue between every two of them and round the outside. First
/// a Wave Function Collapse pass picks each district's <see cref="CityZone"/>. Which zones may sit side by side is
/// fixed (no corporate towers next to the badlands), and each district is narrowed beforehand by where it lies:
/// a coastline wanders along one side of the map with the sea beyond it, and the city grows out in rings from a
/// corporate core near the shore, thinning to badlands at the edge. Along the coast, one side of the core is rich
/// and the other industrial. A quota pass then makes sure every zone has its share of districts, and the city's
/// one solar plant is placed out in the badlands.
/// </para>
/// <para>
/// Then the ground is painted. The sea and the beaches follow a smoothed, noisy blend of the districts, so the
/// shore doesn't follow district edges. Built-up districts are filled with streets and buildings by
/// <see cref="CityBlockGenerator"/>, styled per zone, except where a landmark stands: the corporate headquarters,
/// the civic centre, a megabuilding, the parks and the solar plant. Sparse townships line a few roads out in the
/// badlands. Avenues are streets wherever they touch the city, and two
/// highways carry on through the badlands.
/// </para>
/// <para>
/// Out in the badlands mountains rise towards the edge of the map, with the odd bunker dug into them. A fence
/// runs round the city a little way in from the edge, with a checkpoint wherever a highway crosses it, and an
/// indestructible wall closes off the edge itself.
/// </para>
/// <para>
/// Last, the city is wired from the solar plant: high-voltage cable under the sidewalks to a substation by every
/// block, medium-voltage cable on to an APC in every building, and low-voltage cable round its rooms. Then the
/// residences are furnished: a WFC pass sets out each home's furniture, and one router and a programmable or
/// security computer, with data cable joining them.
/// </para>
/// <para>
/// Last, the city is wired from the solar plant: high-voltage cable under the sidewalks to a substation by every
/// block, medium-voltage cable on to an APC in every building, and low-voltage cable round its rooms.
/// </para>
/// </remarks>
public static partial class CityGenerator
{
    public const int DistrictCells = 8;
    public const int DistrictSize = DistrictCells * CityBlockGenerator.Cell + 1;
    /// <summary>
    /// How wide an avenue is. A district and an avenue together are a whole number of cells, so neighbouring
    /// districts can be joined into one block over the avenue between them.
    /// </summary>
    public const int Avenue = 9;

    public const int Pitch = DistrictSize + Avenue;

    private const int ZoneAttempts = 100;

    /// <summary>How thick the wall round the edge of the map is.</summary>
    public const int BoundaryWidth = 2;

    /// <summary>
    /// How far in from the edge of the map the border fence runs: far enough that the wall at the edge can't be
    /// seen from the checkpoints.
    /// </summary>
    public const int FenceInset = 20;


    private const int Bunkers = 6;

    private static readonly int[] ZoneWeights =
    {
        10, // Ocean
        6, // Coast
        10, // Corporate
        8, // Commercial
        4, // Public
        6, // HighClass
        8, // MediumClass
        8, // LowClass
        6, // Industrial
        4, // Shanty
        10, // Badlands
        6, // Scrub
        1, // Township
        1, // Solar
    };

    /// <summary>The zones each zone may sit beside, besides itself.</summary>
    private static readonly Dictionary<CityZone, CityZone[]> Touches = new()
    {
        [CityZone.Ocean] = new[] { CityZone.Coast, CityZone.HighClass, CityZone.Industrial, CityZone.Shanty },
        [CityZone.Coast] = new[]
        {
            CityZone.Corporate, CityZone.Commercial, CityZone.Public, CityZone.HighClass, CityZone.MediumClass,
            CityZone.LowClass, CityZone.Industrial, CityZone.Shanty, CityZone.Badlands, CityZone.Scrub,
        },
        [CityZone.Corporate] = new[] { CityZone.Commercial, CityZone.Public, CityZone.HighClass, CityZone.MediumClass },
        [CityZone.Commercial] = new[]
        {
            CityZone.Public, CityZone.HighClass, CityZone.MediumClass, CityZone.LowClass, CityZone.Industrial, CityZone.Shanty,
        },
        [CityZone.Public] = new[] { CityZone.HighClass, CityZone.MediumClass },
        [CityZone.HighClass] = new[] { CityZone.MediumClass },
        [CityZone.MediumClass] = new[] { CityZone.LowClass, CityZone.Industrial, CityZone.Badlands, CityZone.Scrub },
        [CityZone.LowClass] = new[] { CityZone.Industrial, CityZone.Shanty, CityZone.Badlands, CityZone.Scrub },
        [CityZone.Industrial] = new[] { CityZone.Shanty, CityZone.Badlands, CityZone.Scrub },
        [CityZone.Shanty] = new[] { CityZone.Badlands, CityZone.Scrub },
        [CityZone.Badlands] = new[] { CityZone.Scrub },
        [CityZone.Scrub] = Array.Empty<CityZone>(),
        [CityZone.Township] = Array.Empty<CityZone>(),
        [CityZone.Solar] = Array.Empty<CityZone>(),
    };

    private static bool CanTouch(CityZone a, CityZone b)
    {
        return a == b || Array.IndexOf(Touches[a], b) >= 0 || Array.IndexOf(Touches[b], a) >= 0;
    }

    private static readonly WfcRules ZoneRules = new(ZoneWeights, (a, _, b) => CanTouch((CityZone) a, (CityZone) b));

    /// <summary>How many districts of each built-up zone a city has, at least and at most.</summary>
    private static readonly Dictionary<CityZone, (int Min, int Max)> Quotas = new()
    {
        [CityZone.Corporate] = (3, 4),
        [CityZone.Public] = (2, 3),
        [CityZone.Commercial] = (5, 7),
        [CityZone.HighClass] = (3, 4),
        [CityZone.MediumClass] = (5, 7),
        [CityZone.LowClass] = (5, 7),
        [CityZone.Industrial] = (5, 7),
        [CityZone.Shanty] = (2, 4),
    };

    private static readonly Dictionary<CityZone, BlockStyle> Styles = new()
    {
        [CityZone.Corporate] = new BlockStyle
        {
            Sprawl = 250,
            Terraces = 50,
            Floors = new[] { CityFloor.White, CityFloor.Dark, CityFloor.Marble },
            Wall = CityStructure.WallReinforced,
            Window = CityStructure.WindowReinforced,
            WindowPercent = 80,
        },
        [CityZone.Commercial] = new BlockStyle
        {
            Floors = new[] { CityFloor.Steel, CityFloor.White, CityFloor.Wood, CityFloor.Dark },
            Window = CityStructure.WindowTinted,
            WindowPercent = 60,
        },
        [CityZone.Public] = new BlockStyle
        {
            Roads = 40,
            Walks = 120,
            Sprawl = 300,
            Terraces = 20,
            Floors = new[] { CityFloor.White, CityFloor.Steel, CityFloor.Marble },
            WindowPercent = 70,
        },
        [CityZone.HighClass] = new BlockStyle
        {
            Roads = 30,
            Walks = 60,
            Lots = 250,
            Sprawl = 200,
            Terraces = 20,
            Floors = new[] { CityFloor.Wood, CityFloor.Marble, CityFloor.White },
            WindowPercent = 70,
        },
        [CityZone.MediumClass] = new BlockStyle
        {
            Sprawl = 50,
            Terraces = 250,
            Floors = new[] { CityFloor.Wood, CityFloor.Steel, CityFloor.Wood },
            Wall = CityStructure.WallBrick,
        },
        [CityZone.LowClass] = new BlockStyle
        {
            Walks = 120,
            Sprawl = 40,
            Terraces = 300,
            Sidewalk = CityFloor.OldConcrete,
            Floors = new[] { CityFloor.SteelDirty, CityFloor.OldConcrete, CityFloor.Steel },
            Wall = CityStructure.WallConcrete,
            WindowPercent = 20,
            DecayPercent = 2,
        },
        [CityZone.Industrial] = new BlockStyle
        {
            Roads = 70,
            Sprawl = 300,
            Sidewalk = CityFloor.Concrete,
            Floors = new[] { CityFloor.Steel, CityFloor.SteelDirty, CityFloor.Plating },
            Wall = CityStructure.WallConcrete,
            WindowPercent = 15,
        },
        [CityZone.Shanty] = new BlockStyle
        {
            Walks = 150,
            Sprawl = 30,
            Terraces = 300,
            Sidewalk = CityFloor.OldConcrete,
            Floors = new[] { CityFloor.SteelDirty, CityFloor.Plating, CityFloor.OldConcrete },
            Wall = CityStructure.WallRust,
            WindowPercent = 25,
            DecayPercent = 8,
        },
    };

    /// <summary>Industrial districts on the waterfront are the port.</summary>
    private static readonly BlockStyle Port = new()
    {
        Roads = 80,
        Sprawl = 400,
        Terraces = 30,
        Sidewalk = CityFloor.Concrete,
        Floors = new[] { CityFloor.Plating, CityFloor.SteelDirty },
        Wall = CityStructure.WallReinforced,
        WindowPercent = 10,
    };

    public static bool IsBuiltUp(CityZone zone)
    {
        return zone is >= CityZone.Corporate and <= CityZone.Shanty;
    }

    /// <summary>The first tile of district <paramref name="d"/> along either axis.</summary>
    public static int Origin(int d)
    {
        return Avenue + d * Pitch;
    }

    /// <summary>How many cells a block <paramref name="districts"/> districts across is, avenues between included.</summary>
    private static int BlockCells(int districts)
    {
        return (districts * Pitch - Avenue - 1) / CityBlockGenerator.Cell;
    }

    /// <summary>
    /// Every district, as blocks: (x, y) of its bottom-left district and its size in districts. Built-up districts
    /// are sometimes joined with neighbours of the same zone into one block of two or four, so the avenues don't
    /// make a regular grid. Landmark districts stay on their own.
    /// </summary>
    private static List<(int X, int Y, int W, int H)> Blocks(CityPlan plan, HashSet<(int, int)> lone, CyberRng rng)
    {
        var n = plan.Districts;
        var taken = new bool[n * n];
        var blocks = new List<(int, int, int, int)>();
        var sizes = new List<(int W, int H)> { (2, 2), (2, 1), (1, 2) };
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                if (taken[y * n + x])
                    continue;

                var zone = plan.Zone(x, y);
                bool Joins(int jx, int jy)
                {
                    return jx < n && jy < n && !taken[jy * n + jx] && plan.Zone(jx, jy) == zone && !lone.Contains((jx, jy));
                }

                var (w, h) = (1, 1);
                if (Styles.ContainsKey(zone) && !lone.Contains((x, y)) && rng.Chance(1, 2))
                {
                    rng.Shuffle(sizes);
                    foreach (var (sw, sh) in sizes)
                    {
                        var fits = true;
                        for (var by = y; by < y + sh; by++)
                        {
                            for (var bx = x; bx < x + sw; bx++)
                            {
                                fits &= Joins(bx, by);
                            }
                        }

                        if (fits)
                        {
                            (w, h) = (sw, sh);
                            break;
                        }
                    }
                }

                for (var by = y; by < y + h; by++)
                {
                    for (var bx = x; bx < x + w; bx++)
                    {
                        taken[by * n + bx] = true;
                    }
                }

                blocks.Add((x, y, w, h));
            }
        }

        return blocks;
    }

    public static CityPlan Generate(ulong seed, int districts = 12)
    {
        if (districts < 8)
            throw new ArgumentOutOfRangeException(nameof(districts), "a city needs at least 8 districts a side");

        var rng = new CyberRng(seed);
        var plan = new CityPlan(districts);
        var shape = new Shape(districts, rng.Fork(1));
        SolveZones(plan, shape, rng.Fork(2));
        var solar = PlaceSolarPlant(plan, shape);
        ClearStrandedDistricts(plan, shape);
        MeetQuotas(plan, shape);
        // Trimming the city to size can leave districts cut off; those go, and the shortfall is made up again.
        ClearStrandedDistricts(plan, shape);
        MeetQuotas(plan, shape);

        var terrain = new Terrain(plan, rng.Fork(3).NextU64());
        terrain.Paint();
        var roads = PaintAvenues(plan, shape);

        // The districts with a landmark of their own, nearest the centre first.
        var centre = shape.Centre;
        var civic = NearestOf(plan, centre, CityZone.Public, Array.Empty<(int, int)>());
        var park = civic is { } c ? NearestOf(plan, centre, CityZone.Public, new[] { c }) : null;
        var mega = NearestOf(plan, centre, CityZone.LowClass, Array.Empty<(int, int)>());
        var lone = new HashSet<(int, int)> { centre };
        foreach (var d in new[] { civic, park, mega })
        {
            if (d is { } district)
                lone.Add(district);
        }

        var districtRng = rng.Fork(4);
        var regions = new List<Region>();
        foreach (var (dx, dy, w, h) in Blocks(plan, lone, rng.Fork(5)))
        {
            var zoneRng = districtRng.Fork((ulong) (dy * districts + dx));
            var (ox, oy) = (Origin(dx), Origin(dy));
            var zone = plan.Zone(dx, dy);
            if (IsBuiltUp(zone))
                regions.Add(new Region(ox, oy, w * Pitch - Avenue, h * Pitch - Avenue, Affluent.Contains(zone)));
            if ((dx, dy) == centre)
                PaintHeadquarters(plan, ox, oy, zoneRng);
            else if ((dx, dy) == civic)
                PaintCivicCentre(plan, ox, oy, zoneRng);
            else if ((dx, dy) == park)
                PaintPark(plan, ox, oy, zoneRng);
            else if ((dx, dy) == mega)
                PaintMegabuilding(plan, ox, oy, zoneRng);
            else if (Styles.TryGetValue(zone, out var style))
            {
                if (zone == CityZone.Industrial && ByTheSea(plan, dx, dy, w, h))
                    style = Port;

                CityBlockGenerator.Generate(plan, ox, oy, BlockCells(w), BlockCells(h), style, zoneRng);
                if (zone == CityZone.HighClass)
                    PaintLawns(plan, ox, oy, BlockCells(w) * CityBlockGenerator.Cell + 1, BlockCells(h) * CityBlockGenerator.Cell + 1);
            }
            else if (zone == CityZone.Badlands && zoneRng.Chance(1, 2))
                PaintRuin(plan, ox, oy, zoneRng);
        }

        var bank = PaintSolarPlant(plan, solar);
        PaintTownships(plan, roads, rng.Fork(7));
        PaintBunkers(plan, rng.Fork(6));
        foreach (var (x, y, w, h) in PaintBorder(plan))
        {
            regions.Add(new Region(x, y, w, h, false));
        }

        foreach (var (kind, x, y, w, h) in plan.Landmarks)
        {
            if (kind is CityLandmark.Township or CityLandmark.SolarPlant)
                regions.Add(new Region(x, y, w, h, false));
        }

        var (cx, cy) = shape.Centre;
        plan.Spawn = (cx * Pitch + Avenue / 2, cy * Pitch + Avenue / 2);
        DigTunnels(plan);
        Wire(plan, bank, regions);
        Populate(plan, rng.Fork(8));
        return plan;
    }

    /// <summary>The district of a zone nearest <paramref name="from"/>, leaving out some.</summary>
    private static (int X, int Y)? NearestOf(CityPlan plan, (int X, int Y) from, CityZone zone, (int, int)[] except)
    {
        (int X, int Y)? best = null;
        var bestDistance = int.MaxValue;
        for (var y = 0; y < plan.Districts; y++)
        {
            for (var x = 0; x < plan.Districts; x++)
            {
                var distance = (x - from.X) * (x - from.X) + (y - from.Y) * (y - from.Y);
                if (plan.Zone(x, y) != zone || Array.IndexOf(except, (x, y)) >= 0 || distance >= bestDistance)
                    continue;

                best = (x, y);
                bestDistance = distance;
            }
        }

        return best;
    }

    private static bool ByTheSea(CityPlan plan, int x, int y, int w, int h)
    {
        for (var dy = y - 1; dy <= y + h; dy++)
        {
            for (var dx = x - 1; dx <= x + w; dx++)
            {
                if (dx >= 0 && dy >= 0 && dx < plan.Districts && dy < plan.Districts && plan.Zone(dx, dy) == CityZone.Ocean)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Where the sea and the city lie: the coast runs along one side of the map, and the city grows out from a
    /// centre one district in from the shore. Along the coast, one side of the centre is rich and the other poor.
    /// </summary>
    private sealed class Shape
    {
        public readonly (int X, int Y) Centre;
        private readonly int _districts;
        private readonly int _side;
        private readonly int[] _shore;
        private readonly float _radius;
        private readonly float[] _jitter;

        /// <summary>Which way along the coast the rich side lies, and how ragged its edge is.</summary>
        private readonly int _lean;
        private readonly float[] _leanJitter;

        public Shape(int districts, CyberRng rng)
        {
            _districts = districts;
            _side = rng.Range(0, 3);

            // How many districts of sea there are at each point along the coast.
            _shore = new int[districts];
            var (shallowest, deepest) = (Math.Max(1, districts / 4), districts / 4 + 1);
            _shore[0] = rng.Range(shallowest, deepest);
            for (var v = 1; v < districts; v++)
            {
                _shore[v] = Math.Clamp(_shore[v - 1] + rng.Range(-1, 1), shallowest, deepest);
            }

            var cv = rng.Range(districts / 2 - 1, districts / 2);
            Centre = FromCoast(_shore[cv] + 1, cv);
            _radius = districts * 0.28f + rng.Range(0, 6) / 10f;
            _jitter = new float[districts * districts];
            _leanJitter = new float[districts * districts];
            for (var i = 0; i < _jitter.Length; i++)
            {
                _jitter[i] = rng.Range(-5, 5) / 10f;
                _leanJitter[i] = rng.Range(-8, 8) / 10f;
            }

            _lean = rng.Chance(1, 2) ? 1 : -1;
        }

        /// <summary>
        /// A district's position as (u, v): u counts inland from the sea's edge of the map, v runs along the coast.
        /// </summary>
        private (int U, int V) ToCoast(int x, int y)
        {
            var last = _districts - 1;
            return _side switch
            {
                0 => (x, y),
                1 => (last - x, y),
                2 => (y, x),
                _ => (last - y, x),
            };
        }

        private (int X, int Y) FromCoast(int u, int v)
        {
            var last = _districts - 1;
            return _side switch
            {
                0 => (u, v),
                1 => (last - u, v),
                2 => (v, u),
                _ => (v, last - u),
            };
        }

        /// <summary>
        /// The zones a district may be before the zone pass. The districts round the edge are left open, for the
        /// border fence and the mountains.
        /// </summary>
        public CityZone[] Allowed(int x, int y)
        {
            var zones = Ring(x, y);
            if (x != 0 && y != 0 && x != _districts - 1 && y != _districts - 1)
                return zones;

            var open = zones.Where(z => !IsBuiltUp(z)).ToArray();
            return open.Length > 0 ? open : new[] { CityZone.Badlands, CityZone.Scrub };
        }

        /// <summary>How far a district is from the city centre, in districts, roughened a little.</summary>
        public float Distance(int x, int y)
        {
            var (u, v) = ToCoast(x, y);
            var (cu, cv) = ToCoast(Centre.X, Centre.Y);
            return MathF.Sqrt((u - cu) * (u - cu) + (v - cv) * (v - cv)) + _jitter[y * _districts + x];
        }

        /// <summary>
        /// The zones a district may be for how far it is from the sea and the city centre, and which side of the
        /// centre it's on. The rich side has the civic centre and the wealthy; the poor side has industry and the
        /// shanty town. Districts near the line between them may be either.
        /// </summary>
        private CityZone[] Ring(int x, int y)
        {
            if ((x, y) == Centre)
                return new[] { CityZone.Corporate };

            var (u, v) = ToCoast(x, y);
            var shore = _shore[v];
            if (u < shore - 1)
                return new[] { CityZone.Ocean };

            var (_, cv) = ToCoast(Centre.X, Centre.Y);
            var lean = _lean * (v - cv) + _leanJitter[y * _districts + x];
            var (rich, poor) = (lean > -0.6f, lean < 0.6f);
            var r = Distance(x, y);
            var zones = new List<CityZone>();
            void Add(bool side, params CityZone[] add)
            {
                if (side)
                    zones.AddRange(add.Where(z => !zones.Contains(z)));
            }

            if (u == shore - 1)
            {
                zones.AddRange(new[] { CityZone.Ocean, CityZone.Coast });
                Add(poor && r <= _radius + 1, CityZone.Industrial);
                return zones.ToArray();
            }

            if (r <= _radius * 0.3f)
            {
                Add(true, CityZone.Corporate, CityZone.Commercial);
            }
            else if (r <= _radius * 0.55f)
            {
                Add(rich, CityZone.Commercial, CityZone.Public, CityZone.Corporate);
                Add(poor, CityZone.Commercial, CityZone.MediumClass);
            }
            else if (r <= _radius * 0.8f)
            {
                Add(rich, CityZone.Public, CityZone.HighClass, CityZone.MediumClass, CityZone.Commercial);
                Add(poor, CityZone.MediumClass, CityZone.LowClass, CityZone.Industrial, CityZone.Commercial);
            }
            else if (r <= _radius)
            {
                Add(rich, CityZone.HighClass, CityZone.MediumClass);
                Add(poor, CityZone.LowClass, CityZone.Industrial, CityZone.Shanty, CityZone.MediumClass);
            }
            else if (r <= _radius + 1.2f)
            {
                Add(rich, CityZone.MediumClass, CityZone.Scrub, CityZone.Badlands);
                Add(poor, CityZone.Shanty, CityZone.LowClass, CityZone.Industrial, CityZone.Scrub, CityZone.Badlands);
            }
            else
            {
                Add(true, CityZone.Badlands, CityZone.Scrub);
            }

            if (u == shore)
            {
                Add(true, CityZone.Coast);
                Add(rich && r <= _radius, CityZone.HighClass);
                Add(poor && r <= _radius + 1.2f, CityZone.Industrial, CityZone.Shanty);
            }

            return zones.ToArray();
        }

        /// <summary>A district's zone if the pass can't be solved: just sea, shore and badlands.</summary>
        public CityZone Fallback(int x, int y)
        {
            var (u, v) = ToCoast(x, y);
            if ((x, y) == Centre)
                return CityZone.Corporate;

            return u < _shore[v] - 1 ? CityZone.Ocean : u == _shore[v] - 1 ? CityZone.Coast : CityZone.Badlands;
        }
    }

    private static void SolveZones(CityPlan plan, Shape shape, CyberRng rng)
    {
        var n = plan.Districts;
        for (var attempt = 0UL; attempt < ZoneAttempts; attempt++)
        {
            var wave = new WfcWave(n, n, ZoneRules.All());
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var options = new WfcSet();
                    foreach (var zone in shape.Allowed(x, y))
                    {
                        options.Insert((int) zone);
                    }

                    wave.Set(x, y, options);
                }
            }

            var attemptRng = rng.Fork(attempt);
            if (wave.Solve(ZoneRules, ref attemptRng) is not { } solution)
                continue;

            for (var i = 0; i < solution.Length; i++)
            {
                plan.Zones[i] = (CityZone) solution[i];
            }

            return;
        }

        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                plan.Zones[y * n + x] = shape.Fallback(x, y);
            }
        }
    }

    /// <summary>
    /// Built-up districts that the city's streets don't reach, such as one on its own out at sea, are left as
    /// open land instead.
    /// </summary>
    private static void ClearStrandedDistricts(CityPlan plan, Shape shape)
    {
        var n = plan.Districts;
        var reached = new bool[n * n];
        var stack = new Stack<(int X, int Y)>();
        stack.Push(shape.Centre);
        reached[shape.Centre.Y * n + shape.Centre.X] = true;
        while (stack.TryPop(out var d))
        {
            foreach (var (dx, dy) in WfcWave.Directions)
            {
                var (x, y) = (d.X + dx, d.Y + dy);
                if (x < 0 || y < 0 || x >= n || y >= n || reached[y * n + x] || !IsBuiltUp(plan.Zone(x, y)))
                    continue;

                reached[y * n + x] = true;
                stack.Push((x, y));
            }
        }

        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                if (reached[y * n + x] || !IsBuiltUp(plan.Zone(x, y)))
                    continue;

                var bySea = WfcWave.Directions.Any(d => x + d.X >= 0 && y + d.Y >= 0 && x + d.X < n && y + d.Y < n
                                                        && plan.Zone(x + d.X, y + d.Y) == CityZone.Ocean);
                plan.Zones[y * n + x] = bySea ? CityZone.Coast : CityZone.Scrub;
            }
        }
    }

    /// <summary>
    /// The ground everywhere before anything is built: sea, beaches and the land of each zone.
    /// </summary>
    private sealed class Terrain
    {
        /// <summary>How far in from the edge of the map the mountains start to rise.</summary>
        private const float MountainRise = 120f;

        private const float MountainHeight = 0.8f;

        private readonly CityPlan _plan;
        private readonly ulong _seed;

        public Terrain(CityPlan plan, ulong seed)
        {
            _plan = plan;
            _seed = seed;
        }

        private static float SeaLevel(CityZone zone)
        {
            return zone switch
            {
                CityZone.Ocean => 1f,
                CityZone.Coast => 0.5f,
                _ => 0f,
            };
        }

        /// <summary>The district a tile is nearest the middle of, along one axis.</summary>
        private int Nearest(float t)
        {
            return Math.Clamp((int) MathF.Round(DistrictPosition(t)), 0, _plan.Districts - 1);
        }

        /// <summary>Where a tile is in districts, with each district's middle on a whole number.</summary>
        private static float DistrictPosition(float t)
        {
            return (t - Avenue - DistrictSize / 2f) / Pitch;
        }

        /// <summary>A value per zone, blended smoothly between the middles of the districts round a tile.</summary>
        private float Blend(Func<CityZone, float> value, int x, int y)
        {
            var last = _plan.Districts - 1;
            var fx = Math.Clamp(DistrictPosition(x), 0, last);
            var fy = Math.Clamp(DistrictPosition(y), 0, last);
            var (x0, y0) = ((int) fx, (int) fy);
            var (x1, y1) = (Math.Min(x0 + 1, last), Math.Min(y0 + 1, last));
            var (tx, ty) = (fx - x0, fy - y0);
            float At(int dx, int dy) => value(_plan.Zone(dx, dy));
            var bottom = At(x0, y0) + (At(x1, y0) - At(x0, y0)) * tx;
            var top = At(x0, y1) + (At(x1, y1) - At(x0, y1)) * tx;
            return bottom + (top - bottom) * ty;
        }

        /// <summary>How much sea a tile is.</summary>
        private float Sea(int x, int y)
        {
            return Blend(SeaLevel, x, y)
                   + (Noise.At(_seed, x / 30f, y / 30f) - 0.5f) * 0.7f
                   + (Noise.At(_seed ^ 0x5EA, x / 8f, y / 8f) - 0.5f) * 0.3f;
        }

        /// <summary>
        /// Whether a tile is mountain: rough ground that rises the further it is from the city and the nearer the
        /// edge of the map.
        /// </summary>
        private bool Mountain(int x, int y)
        {
            var edge = Math.Min(Math.Min(x, y), Math.Min(_plan.Size - 1 - x, _plan.Size - 1 - y));
            var town = Blend(z => IsBuiltUp(z) || z == CityZone.Solar ? 1f : 0f, x, y);
            var height = Noise.At(_seed ^ 0x3077, x / 32f, y / 32f) * 0.6f
                         + Noise.At(_seed ^ 0x3078, x / 8f, y / 8f) * 0.25f
                         + Math.Max(0f, 1f - edge / MountainRise) * 0.6f
                         - town;
            return height > MountainHeight;
        }

        public void Paint()
        {
            for (var y = 0; y < _plan.Size; y++)
            {
                for (var x = 0; x < _plan.Size; x++)
                {
                    var zone = LandZone(x, y);
                    var sea = Sea(x, y);
                    if (sea > 0.55f)
                        _plan.Set(x, y, CityFloor.Ocean);
                    else if (sea > 0.4f)
                        _plan.Set(x, y, CityFloor.Sand);
                    else if (zone is CityZone.Badlands or CityZone.Scrub or CityZone.Township && sea < 0.25f && Mountain(x, y))
                        _plan.Set(x, y, Land(zone, x, y), CityStructure.Mountain);
                    else
                        _plan.Set(x, y, Land(zone, x, y), Scatter(zone, x, y));
                }
            }
        }

        /// <summary>
        /// The zone whose land a tile gets. Where it's looked up is pushed about by noise at a few scales, so the
        /// borders between the zones wander and fray into each other instead of following the district squares.
        /// </summary>
        private CityZone LandZone(int x, int y)
        {
            float Warp(ulong salt)
            {
                return (Noise.At(_seed ^ salt, x / 24f, y / 24f) - 0.5f) * 48f
                       + (Noise.At(_seed ^ salt << 8, x / 6f, y / 6f) - 0.5f) * 12f
                       + (Noise.Hash(_seed ^ salt << 16, x, y) % 9 - 4f);
            }

            return _plan.Zone(Nearest(x + Warp(0xA11)), Nearest(y + Warp(0xB22)));
        }

        private CityFloor Land(CityZone zone, int x, int y)
        {
            var n = Noise.At(_seed ^ 0x1A4D, x / 6f, y / 6f);
            return zone switch
            {
                CityZone.Ocean or CityZone.Coast => n < 0.6f ? CityFloor.Sand : CityFloor.Desert,
                CityZone.Scrub or CityZone.Township => n < 0.35f ? CityFloor.Dirt : n < 0.7f ? CityFloor.WildGrass : CityFloor.Desert,
                CityZone.Badlands or CityZone.Solar => n < 0.3f ? CityFloor.LowDesert : n < 0.75f ? CityFloor.Desert : CityFloor.Sand,
                _ => CityFloor.Dirt,
            };
        }

        /// <summary>Rocks and the odd tree, scattered over open land.</summary>
        private CityStructure Scatter(CityZone zone, int x, int y)
        {
            var roll = Noise.Hash(_seed ^ 0x5CA7, x, y) % 1000;
            return zone switch
            {
                CityZone.Badlands when roll < 10 => CityStructure.Rock,
                CityZone.Scrub when roll < 5 => CityStructure.Rock,
                CityZone.Scrub when roll < 8 => CityStructure.Tree,
                CityZone.Coast when roll < 3 => CityStructure.Rock,
                _ => CityStructure.None,
            };
        }
    }

    private enum Road : byte
    {
        None,
        Street,
        Highway,
    }

    /// <summary>
    /// Avenues are streets with sidewalks wherever they touch a built-up district. Out in the badlands, the two
    /// running along the city centre's south and west edges carry on as highways; the rest are left as land.
    /// </summary>
    /// <returns>
    /// What each avenue is: those running north to south by line and district, then those running east to west by
    /// district and line.
    /// </returns>
    private static (Road[,] Vertical, Road[,] Horizontal) PaintAvenues(CityPlan plan, Shape shape)
    {
        var n = plan.Districts;
        var (cx, cy) = shape.Centre;

        Road Segment(bool highway, (int X, int Y) a, (int X, int Y) b)
        {
            CityZone? Zone((int X, int Y) d)
            {
                return d.X >= 0 && d.Y >= 0 && d.X < n && d.Y < n ? plan.Zone(d.X, d.Y) : null;
            }

            var (za, zb) = (Zone(a), Zone(b));
            if (za is { } ra && IsBuiltUp(ra) || zb is { } rb && IsBuiltUp(rb))
                return Road.Street;

            return highway && za != CityZone.Ocean && zb != CityZone.Ocean ? Road.Highway : Road.None;
        }

        // Segments of the avenues running north to south, by line and district, then those running east to west.
        var vertical = new Road[n + 1, n];
        var horizontal = new Road[n, n + 1];
        for (var i = 0; i <= n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                vertical[i, j] = Segment(i == cx, (i - 1, j), (i, j));
                horizontal[j, i] = Segment(i == cy, (j, i - 1), (j, i));
            }
        }

        for (var i = 0; i <= n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                PaintSegment(plan, vertical[i, j], i * Pitch, Origin(j), true);
                PaintSegment(plan, horizontal[j, i], Origin(j), i * Pitch, false);
            }
        }

        for (var i = 0; i <= n; i++)
        {
            for (var j = 0; j <= n; j++)
            {
                var around = new[]
                {
                    j > 0 ? vertical[i, j - 1] : Road.None,
                    j < n ? vertical[i, j] : Road.None,
                    i > 0 ? horizontal[i - 1, j] : Road.None,
                    i < n ? horizontal[i, j] : Road.None,
                };
                PaintJunction(plan, i * Pitch, j * Pitch, around);
            }
        }

        return (vertical, horizontal);
    }

    /// <summary>
    /// One avenue between two junctions, with (<paramref name="x"/>, <paramref name="y"/>) its bottom-left tile.
    /// </summary>
    private static void PaintSegment(CityPlan plan, Road road, int x, int y, bool vertical)
    {
        if (road == Road.None)
            return;

        for (var along = 0; along < DistrictSize; along++)
        {
            for (var across = 0; across < Avenue; across++)
            {
                var (tx, ty) = vertical ? (x + across, y + along) : (x + along, y + across);
                var lanes = across is >= 2 and < Avenue - 2;
                // Highways stop at the shore.
                if (road == Road.Highway && plan.Floor(tx, ty) == CityFloor.Ocean)
                    continue;

                if (lanes)
                    plan.Set(tx, ty, CityFloor.Asphalt);
                else if (road == Road.Street)
                    plan.Set(tx, ty, CityFloor.Sidewalk);
            }

            // Lamps every 12 tiles, alternating sides of the street.
            if (road == Road.Street && along % 12 == 6)
            {
                var across = along % 24 == 6 ? 1 : Avenue - 2;
                var (lx, ly) = vertical ? (x + across, y + along) : (x + along, y + across);
                plan.SetStructure(lx, ly, CityStructure.Lamp);
            }
        }
    }

    /// <summary>Where avenues cross: open road, with sidewalk on the corners between streets.</summary>
    private static void PaintJunction(CityPlan plan, int x, int y, Road[] around)
    {
        if (Array.TrueForAll(around, r => r == Road.None))
            return;

        var street = Array.IndexOf(around, Road.Street) >= 0;
        for (var dy = 0; dy < Avenue; dy++)
        {
            for (var dx = 0; dx < Avenue; dx++)
            {
                var laneX = dx is >= 2 and < Avenue - 2;
                var laneY = dy is >= 2 and < Avenue - 2;
                if (street)
                    plan.Set(x + dx, y + dy, laneX || laneY ? CityFloor.Asphalt : CityFloor.Sidewalk);
                else if ((laneX || laneY) && plan.Floor(x + dx, y + dy) != CityFloor.Ocean)
                    plan.Set(x + dx, y + dy, CityFloor.Asphalt);
            }
        }
    }

    /// <summary>Lawns and trees inside a ring of paths, with a cross through the middle and sometimes a pond.</summary>
    private static void PaintPark(CityPlan plan, int ox, int oy, CyberRng rng)
    {
        const int mid = DistrictSize / 2;

        static bool Path(int t)
        {
            return t <= 1 || t >= DistrictSize - 2 || Math.Abs(t - mid) <= 1;
        }

        var pond = rng.Chance(2, 3);
        var (px, py) = (rng.Pick(new[] { mid / 2, mid + mid / 2 }), rng.Pick(new[] { mid / 2, mid + mid / 2 }));
        var radius = rng.Range(4, 6);
        for (var y = 0; y < DistrictSize; y++)
        {
            for (var x = 0; x < DistrictSize; x++)
            {
                var (dx, dy) = (x - px, y - py);
                if (Path(x) || Path(y))
                    plan.Set(ox + x, oy + y, CityFloor.Sidewalk);
                else if (pond && dx * dx + dy * dy <= radius * radius)
                    plan.Set(ox + x, oy + y, CityFloor.Ocean);
                else if (pond && dx * dx + dy * dy <= (radius + 1) * (radius + 1))
                    plan.Set(ox + x, oy + y, CityFloor.Sand);
                else
                {
                    var nearPath = Path(x - 1) || Path(x + 1) || Path(y - 1) || Path(y + 1);
                    plan.Set(ox + x, oy + y, CityFloor.Grass, !nearPath && rng.Chance(1, 9) ? CityStructure.Tree : CityStructure.None);
                }
            }
        }

        foreach (var t in new[] { mid / 2, mid + 2, mid + mid / 2 })
        {
            plan.SetStructure(ox + t, oy + mid + 2, CityStructure.Lamp);
            plan.SetStructure(ox + mid + 2, oy + t, CityStructure.Lamp);
        }
    }

    /// <summary>A roofless shack falling apart in the badlands.</summary>
    private static void PaintRuin(CityPlan plan, int ox, int oy, CyberRng rng)
    {
        var (w, h) = (rng.Range(5, 9), rng.Range(5, 9));
        var (x0, y0) = (ox + rng.Range(2, DistrictSize - w - 2), oy + rng.Range(2, DistrictSize - h - 2));
        for (var y = y0; y < y0 + h; y++)
        {
            for (var x = x0; x < x0 + w; x++)
            {
                var edge = x == x0 || y == y0 || x == x0 + w - 1 || y == y0 + h - 1;
                var structure = !edge ? CityStructure.None : rng.Range(0, 9) switch
                {
                    < 5 => CityStructure.WallRust,
                    < 8 => CityStructure.Girder,
                    _ => CityStructure.None,
                };
                plan.Set(x, y, rng.Chance(1, 3) ? CityFloor.Plating : CityFloor.OldConcrete, structure);
            }
        }
    }

    /// <summary>
    /// Bunkers dug into the mountains, inside the fence: a room or two walled in concrete, with a door out into
    /// the rock. <see cref="DigTunnels"/> leads a tunnel to each.
    /// </summary>
    private static void PaintBunkers(CityPlan plan, CyberRng rng)
    {
        var (low, placed) = (FenceInset + 4, 0);
        for (var attempt = 0; attempt < 300 && placed < Bunkers; attempt++)
        {
            var (w, h) = (rng.Range(7, 11), rng.Range(6, 8));
            var high = plan.Size - FenceInset - 4;
            var (x0, y0) = (rng.Range(low, high - w), rng.Range(low, high - h));
            var solid = true;
            for (var y = y0 - 2; y < y0 + h + 2 && solid; y++)
            {
                for (var x = x0 - 2; x < x0 + w + 2 && solid; x++)
                {
                    solid = plan.Structure(x, y) == CityStructure.Mountain;
                }
            }

            if (!solid)
                continue;

            var wall = rng.Chance(1, 2) ? CityStructure.WallConcrete : CityStructure.WallReinforced;
            var floor = rng.Pick(new[] { CityFloor.Concrete, CityFloor.SteelDirty, CityFloor.Plating });
            var split = w >= 9 ? x0 + w / 2 : -1;
            for (var y = y0; y < y0 + h; y++)
            {
                for (var x = x0; x < x0 + w; x++)
                {
                    var edge = x == x0 || y == y0 || x == x0 + w - 1 || y == y0 + h - 1;
                    var structure = edge || x == split ? wall : CityStructure.None;
                    if (x == split && y == y0 + h / 2)
                        structure = CityStructure.Door;

                    plan.Set(x, y, floor, structure);
                }
            }

            var (dx, dy) = rng.Pick(new[] { (x0 + 2, y0), (x0 + 2, y0 + h - 1), (x0, y0 + h / 2), (x0 + w - 1, y0 + h / 2) });
            plan.SetStructure(dx, dy, CityStructure.Door);
            placed++;
        }
    }

    /// <summary>
    /// The edge of the map: a fence round the city with a checkpoint wherever a highway crosses it, and beyond
    /// that the boundary wall. The fence runs over land only, so it ends at the sea and gaps where mountains stand.
    /// </summary>
    /// <returns>The checkpoints, as their bottom-left tile and size.</returns>
    private static List<(int X, int Y, int W, int H)> PaintBorder(CityPlan plan)
    {
        var (near, far) = (FenceInset, plan.Size - 1 - FenceInset);
        var ring = new List<(int X, int Y)>();
        for (var t = near; t <= far; t++)
        {
            ring.Add((t, near));
            ring.Add((t, far));
            if (t != near && t != far)
            {
                ring.Add((near, t));
                ring.Add((far, t));
            }
        }

        // Highways crossing the fence line: runs of exactly one road's lanes, as the solar farms' tracks are narrower.
        var checkpoints = new List<(int X, int Y, bool Vertical)>();
        const int lanes = Avenue - 4;
        foreach (var fixedY in new[] { near, far })
        {
            for (var x = near; x <= far - lanes; x++)
            {
                if (Lanes(plan, x, fixedY, 1, 0) == lanes && plan.Floor(x - 1, fixedY) != CityFloor.Asphalt)
                    checkpoints.Add((x + lanes / 2, fixedY, true));
            }
        }

        foreach (var fixedX in new[] { near, far })
        {
            for (var y = near; y <= far - lanes; y++)
            {
                if (Lanes(plan, fixedX, y, 0, 1) == lanes && plan.Floor(fixedX, y - 1) != CityFloor.Asphalt)
                    checkpoints.Add((fixedX, y + lanes / 2, false));
            }
        }

        foreach (var (x, y) in ring)
        {
            if (plan.Floor(x, y) != CityFloor.Ocean && plan.Structure(x, y) != CityStructure.Mountain)
                plan.Set(x, y, plan.Floor(x, y), CityStructure.Fence);
        }

        var areas = new List<(int X, int Y, int W, int H)>();
        foreach (var (x, y, vertical) in checkpoints)
        {
            // Towards the middle of the map, so the booths' doors face the city.
            var inward = (vertical ? y : x) < plan.Size / 2 ? 1 : -1;
            PaintCheckpoint(plan, x, y, vertical, inward);
            areas.Add(vertical ? (x - 9, y - 3, 19, 7) : (x - 3, y - 9, 7, 19));
        }

        for (var y = 0; y < plan.Size; y++)
        {
            for (var x = 0; x < plan.Size; x++)
            {
                if (Math.Min(Math.Min(x, y), Math.Min(plan.Size - 1 - x, plan.Size - 1 - y)) < BoundaryWidth)
                    plan.SetStructure(x, y, CityStructure.BoundaryWall);
            }
        }

        return areas;
    }

    /// <summary>How many tiles of asphalt run from a tile in a direction.</summary>
    private static int Lanes(CityPlan plan, int x, int y, int dx, int dy)
    {
        var count = 0;
        while (plan.Contains(x, y) && plan.Floor(x, y) == CityFloor.Asphalt)
        {
            count++;
            (x, y) = (x + dx, y + dy);
        }

        return count;
    }

    /// <summary>
    /// A border checkpoint where a road crosses the fence at (<paramref name="cx"/>, <paramref name="cy"/>), the
    /// middle of the road: a concrete apron with a guard booth either side, joined to the fence.
    /// </summary>
    private static void PaintCheckpoint(CityPlan plan, int cx, int cy, bool vertical, int inward)
    {
        // a runs along the fence, b along the road.
        void Put(int a, int b, CityFloor? floor, CityStructure structure)
        {
            var (x, y) = vertical ? (cx + a, cy + b * inward) : (cx + b * inward, cy + a);
            plan.Set(x, y, floor ?? plan.Floor(x, y), structure);
        }

        for (var b = -3; b <= 3; b++)
        {
            for (var a = -9; a <= 9; a++)
            {
                var side = Math.Abs(a);
                if (side <= 2)
                {
                    Put(a, b, null, CityStructure.None);
                    continue;
                }

                if (side <= 4 || Math.Abs(b) == 3)
                {
                    Put(a, b, CityFloor.Concrete, CityStructure.None);
                    continue;
                }

                var edge = side == 5 || side == 9 || Math.Abs(b) == 2;
                var structure = !edge ? CityStructure.None
                    : side == 5 && Math.Abs(b) <= 1 ? CityStructure.WindowReinforced
                    : side == 7 && b == 2 ? CityStructure.Door
                    : CityStructure.WallReinforced;
                Put(a, b, CityFloor.Steel, structure);
            }
        }

        foreach (var a in new[] { -3, 3 })
        {
            Put(a, -3, null, CityStructure.Lamp);
            Put(a, 3, null, CityStructure.Lamp);
        }
    }

    private static bool Open(CityPlan plan, int i)
    {
        return plan.Floors[i] != CityFloor.Ocean && plan.Structures[i] is CityStructure.None or CityStructure.Door;
    }

    /// <summary>
    /// Every door can be walked to from where people arrive: anywhere with a door that can't be reached, such as
    /// a bunker in the mountains, gets a tunnel dug to it through the rock from the nearest place that can.
    /// </summary>
    private static void DigTunnels(CityPlan plan)
    {
        var size = plan.Size;
        var area = new int[size * size];
        Array.Fill(area, -1);
        var areas = 0;
        var withDoors = new List<bool>();
        var stack = new Stack<int>();
        IEnumerable<int> Neighbours(int i)
        {
            var (x, y) = (i % size, i / size);
            if (x > 0)
                yield return i - 1;
            if (x < size - 1)
                yield return i + 1;
            if (y > 0)
                yield return i - size;
            if (y < size - 1)
                yield return i + size;
        }

        // Label each patch of open ground.
        for (var start = 0; start < area.Length; start++)
        {
            if (area[start] >= 0 || !Open(plan, start))
                continue;

            var door = false;
            area[start] = areas;
            stack.Push(start);
            while (stack.TryPop(out var i))
            {
                door |= plan.Structures[i] == CityStructure.Door;
                foreach (var next in Neighbours(i))
                {
                    if (area[next] >= 0 || !Open(plan, next))
                        continue;

                    area[next] = areas;
                    stack.Push(next);
                }
            }

            withDoors.Add(door);
            areas++;
        }

        var reached = new bool[areas];
        reached[area[plan.Spawn.Y * size + plan.Spawn.X]] = true;
        var parent = new int[size * size];
        var queue = new Queue<int>();
        while (true)
        {
            var wanted = Enumerable.Range(0, areas).Any(a => withDoors[a] && !reached[a]);
            if (!wanted)
                return;

            // Breadth first out from everywhere reached, through rock and open ground, to the nearest patch that
            // has a door and isn't reached yet.
            Array.Fill(parent, -2);
            queue.Clear();
            for (var i = 0; i < area.Length; i++)
            {
                if (area[i] >= 0 && reached[area[i]])
                {
                    parent[i] = -1;
                    queue.Enqueue(i);
                }
            }

            var found = -1;
            while (found < 0 && queue.TryDequeue(out var i))
            {
                foreach (var next in Neighbours(i))
                {
                    if (parent[next] != -2)
                        continue;

                    var open = area[next] >= 0;
                    if (!open && plan.Structures[next] != CityStructure.Mountain)
                        continue;

                    parent[next] = i;
                    if (open && withDoors[area[next]] && !reached[area[next]])
                    {
                        found = next;
                        break;
                    }

                    queue.Enqueue(next);
                }
            }

            if (found < 0)
                return;

            reached[area[found]] = true;
            for (var i = parent[found]; i >= 0; i = parent[i])
            {
                if (area[i] >= 0)
                {
                    reached[area[i]] = true;
                    continue;
                }

                plan.Structures[i] = CityStructure.None;
                area[i] = area[found];
            }
        }
    }
}

/// <summary>
/// Seeded value noise: smooth random values between 0 and 1 that change gently from tile to tile.
/// </summary>
public static class Noise
{
    public static ulong Hash(ulong seed, int x, int y)
    {
        return new CyberRng(seed ^ unchecked((ulong) x * 0x9E3779B97F4A7C15 ^ (ulong) y * 0xC2B2AE3D27D4EB4F)).NextU64();
    }

    public static float At(ulong seed, float x, float y)
    {
        var (x0, y0) = ((int) MathF.Floor(x), (int) MathF.Floor(y));
        var (tx, ty) = (Smooth(x - x0), Smooth(y - y0));
        float Corner(int cx, int cy) => (Hash(seed, cx, cy) >> 40) / (float) (1 << 24);
        var bottom = Corner(x0, y0) + (Corner(x0 + 1, y0) - Corner(x0, y0)) * tx;
        var top = Corner(x0, y0 + 1) + (Corner(x0 + 1, y0 + 1) - Corner(x0, y0 + 1)) * tx;
        return bottom + (top - bottom) * ty;
    }

    private static float Smooth(float t)
    {
        return t * t * (3 - 2 * t);
    }
}
