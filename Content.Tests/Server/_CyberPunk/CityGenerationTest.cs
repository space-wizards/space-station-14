using System.Collections.Generic;
using System.Linq;
using Content.Server._CyberPunk.City;
using NUnit.Framework;

namespace Content.Tests.Server._CyberPunk;

[TestFixture]
[TestOf(typeof(CityGenerator))]
public sealed class CityGenerationTest
{
    private static readonly ulong[] Seeds = { 0, 1, 2, 3, 4, 5, 6, 7 };

    [Test]
    public void SameSeedSameCity()
    {
        var a = CityGenerator.Generate(42);
        var b = CityGenerator.Generate(42);
        Assert.That(a.Zones, Is.EqualTo(b.Zones));
        Assert.That(a.Floors, Is.EqualTo(b.Floors));
        Assert.That(a.Structures, Is.EqualTo(b.Structures));
        Assert.That(a.Cables, Is.EqualTo(b.Cables));
        Assert.That(a.Fixtures, Is.EqualTo(b.Fixtures));
        Assert.That(a.Objects, Is.EqualTo(b.Objects));
        Assert.That(a.Carpets, Is.EqualTo(b.Carpets));
        Assert.That(a.Curtains, Is.EqualTo(b.Curtains));
        Assert.That(a.Spawn, Is.EqualTo(b.Spawn));
    }

    [Test]
    public void CoastalCityInTheBadlands([ValueSource(nameof(Seeds))] ulong seed)
    {
        var plan = CityGenerator.Generate(seed);
        Assert.That(plan.Zones, Does.Contain(CityZone.Ocean));
        Assert.That(plan.Zones, Does.Contain(CityZone.Corporate));
        Assert.That(plan.Zones.Any(z => z is CityZone.Badlands or CityZone.Scrub));
        Assert.That(plan.Floor(plan.Spawn.X, plan.Spawn.Y), Is.EqualTo(CityFloor.Asphalt));
        Assert.That(plan.Structure(plan.Spawn.X, plan.Spawn.Y), Is.EqualTo(CityStructure.None));
    }

    /// <summary>
    /// Every city has each zone within its share of districts, one each of its landmarks, and a few townships out
    /// in the badlands, inside the fence.
    /// </summary>
    [Test]
    public void EveryZoneAndLandmark([ValueSource(nameof(Seeds))] ulong seed)
    {
        var plan = CityGenerator.Generate(seed);
        var quotas = new Dictionary<CityZone, (int Min, int Max)>
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
        foreach (var (zone, (min, max)) in quotas)
        {
            Assert.That(plan.Zones.Count(z => z == zone), Is.InRange(min, max), zone.ToString());
        }

        foreach (var landmark in new[]
                 {
                     CityLandmark.Headquarters, CityLandmark.CityHall, CityLandmark.PoliceStation, CityLandmark.Hospital,
                     CityLandmark.TransitStation, CityLandmark.Megabuilding, CityLandmark.SolarPlant,
                 })
        {
            Assert.That(plan.Landmarks.Count(l => l.Kind == landmark), Is.EqualTo(1), landmark.ToString());
        }

        var townships = plan.Landmarks.Where(l => l.Kind == CityLandmark.Township).ToList();
        Assert.That(townships, Has.Count.InRange(2, 4));
        var (near, far) = (CityGenerator.FenceInset, plan.Size - 1 - CityGenerator.FenceInset);
        Assert.That(townships.All(t => t.X > near && t.Y > near && t.X + t.W - 1 < far && t.Y + t.H - 1 < far));
    }

    /// <summary>
    /// Mountains rise in the badlands, a fence runs round the city, and nothing gets past the wall round the edge.
    /// </summary>
    [Test]
    public void FencedAndWalledIn([ValueSource(nameof(Seeds))] ulong seed)
    {
        var plan = CityGenerator.Generate(seed);
        Assert.That(plan.Structures, Does.Contain(CityStructure.Mountain));
        Assert.That(plan.Structures, Does.Contain(CityStructure.Fence));
        var last = plan.Size - 1;
        var edge = Enumerable.Range(0, plan.Size)
            .SelectMany(t => new[] { (t, 0), (t, last), (0, t), (last, t) })
            .Where(tile => plan.Structure(tile.Item1, tile.Item2) != CityStructure.BoundaryWall);
        Assert.That(edge, Is.Empty);
    }

    /// <summary>
    /// From where people arrive, every door in the city can be walked to, so every building and every room in it
    /// can be reached.
    /// </summary>
    [Test]
    public void EveryDoorCanBeReached([ValueSource(nameof(Seeds))] ulong seed)
    {
        var plan = CityGenerator.Generate(seed);
        bool Open(int x, int y)
        {
            return plan.Floor(x, y) != CityFloor.Ocean
                   && plan.Structure(x, y) is CityStructure.None or CityStructure.Door;
        }

        var reached = new bool[plan.Size * plan.Size];
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(plan.Spawn);
        reached[plan.Spawn.Y * plan.Size + plan.Spawn.X] = true;
        while (queue.TryDequeue(out var tile))
        {
            foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
            {
                var (x, y) = (tile.X + dx, tile.Y + dy);
                if (!plan.Contains(x, y) || reached[y * plan.Size + x] || !Open(x, y))
                    continue;

                reached[y * plan.Size + x] = true;
                queue.Enqueue((x, y));
            }
        }

        var doors = Enumerable.Range(0, plan.Size * plan.Size)
            .Where(i => plan.Structures[i] == CityStructure.Door)
            .ToList();
        Assert.That(doors, Is.Not.Empty);
        Assert.That(doors.Where(i => !reached[i]).Select(i => (i % plan.Size, i / plan.Size)), Is.Empty);
    }

    /// <summary>
    /// The solar panels charge the plant's SMES bank and nothing else. The bank feeds every substation, directly
    /// or through a block's own SMES, and a substation feeds every APC, with low-voltage cable on from it.
    /// </summary>
    [Test]
    public void WiredFromThePlant([ValueSource(nameof(Seeds))] ulong seed)
    {
        var plan = CityGenerator.Generate(seed);
        var size = plan.Size;
        var directions = new[] { (0, 1), (1, 0), (0, -1), (-1, 0) };
        var terminals = plan.Fixtures.Where(f => f.Kind == CityFixture.Terminal)
            .ToDictionary(f => f.Y * size + f.X, f => f.Direction);

        // The tiles joined by one kind of cable, never across a terminal and the SMES it faces.
        HashSet<int> Network(int start, CityCable cable)
        {
            var seen = new HashSet<int> { start };
            var stack = new Stack<int>();
            stack.Push(start);
            while (stack.TryPop(out var i))
            {
                for (var d = 0; d < 4; d++)
                {
                    var (x, y) = (i % size + directions[d].Item1, i / size + directions[d].Item2);
                    var next = y * size + x;
                    if (!plan.Contains(x, y) || (plan.Cables[next] & cable) == 0 || seen.Contains(next)
                        || terminals.TryGetValue(i, out var a) && a == d
                        || terminals.TryGetValue(next, out var b) && b == (d + 2) % 4)
                    {
                        continue;
                    }

                    seen.Add(next);
                    stack.Push(next);
                }
            }

            return seen;
        }

        int Faced(KeyValuePair<int, int> terminal)
        {
            var (dx, dy) = directions[terminal.Value];
            return terminal.Key + dy * size + dx;
        }

        var tiles = Enumerable.Range(0, size * size).ToList();
        var panels = tiles.Where(i => plan.Structures[i] == CityStructure.SolarPanel).ToList();
        Assert.That(panels, Is.Not.Empty);
        var input = Network(panels[0], CityCable.High);
        Assert.That(panels.All(input.Contains));
        Assert.That(input.Any(i => plan.Structures[i] is CityStructure.Smes or CityStructure.Substation), Is.False);

        var bank = terminals.Where(t => input.Contains(t.Key)).Select(Faced).ToList();
        Assert.That(bank, Is.Not.Empty);
        var grid = Network(bank[0], CityCable.High);
        Assert.That(bank.All(grid.Contains));

        var substations = tiles.Where(i => plan.Structures[i] == CityStructure.Substation).ToList();
        Assert.That(substations, Is.Not.Empty);
        foreach (var substation in substations)
        {
            var fed = grid.Contains(substation)
                      || terminals.Any(t => grid.Contains(t.Key) && Network(substation, CityCable.High).Contains(Faced(t)));
            Assert.That(fed, $"substation at {substation % size}, {substation / size}");
        }

        var apcs = plan.Fixtures.Where(f => f.Kind == CityFixture.Apc).ToList();
        Assert.That(apcs, Is.Not.Empty);
        foreach (var (_, x, y, _) in apcs)
        {
            var i = y * size + x;
            Assert.That(plan.Cable(x, y) & CityCable.Low, Is.EqualTo(CityCable.Low));
            Assert.That(Network(i, CityCable.Medium).Any(substations.Contains), $"APC at {x}, {y}");
        }
    }

    /// <summary>
    /// Every residence sleeps somewhere, has exactly one router and at least one computer, and data cable joins
    /// them. The furniture keeps to bare floor, one piece to a tile, clear of the doors, and security computers
    /// stay rare across the city.
    /// </summary>
    [Test]
    public void ResidencesAreNetworked([ValueSource(nameof(Seeds))] ulong seed)
    {
        var plan = CityGenerator.Generate(seed);
        var size = plan.Size;
        var residences = CityGenerator.Residences(plan);
        Assert.That(residences, Is.Not.Empty);

        var devices = 0;
        foreach (var residence in residences)
        {
            var tiles = residence.ToHashSet();
            var objects = plan.Objects.Where(o => tiles.Contains(o.Y * size + o.X)).ToList();
            var routers = objects.Where(o => o.Kind == CityObject.Router).ToList();
            var computers = objects.Where(o => o.Kind is CityObject.Computer or CityObject.SecurityComputer).ToList();
            devices += computers.Count(o => o.Kind == CityObject.SecurityComputer);

            Assert.That(routers, Has.Count.EqualTo(1), "one router to a home");
            Assert.That(computers, Is.Not.Empty, "at least one computer to a home");
            Assert.That(objects.Any(o => o.Kind is CityObject.Bed or CityObject.Mattress), "somewhere to sleep");

            // One thing to a tile, on bare floor, and nothing on or beside a door but the devices.
            Assert.That(objects.Select(o => o.Y * size + o.X), Is.Unique);
            foreach (var o in objects)
            {
                var i = o.Y * size + o.X;
                Assert.That(plan.Structure(o.X, o.Y), Is.EqualTo(CityStructure.None));
                if (o.Kind is not (CityObject.Router or CityObject.Computer or CityObject.SecurityComputer
                    or CityObject.EndTable or CityObject.Dresser))
                {
                    foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
                    {
                        Assert.That(plan.Structure(o.X + dx, o.Y + dy),
                            Is.Not.EqualTo(CityStructure.Door), $"{o.Kind} at {o.X}, {o.Y}");
                    }
                }
            }

            // Data cable runs from the router to the computer through the home.
            var router = routers[0].Y * size + routers[0].X;
            var computer = computers[0].Y * size + computers[0].X;
            Assert.That((plan.Cables[router] & CityCable.Data) != 0, "data cable at the router");
            var reached = new HashSet<int> { router };
            var stack = new Stack<int>();
            stack.Push(router);
            while (stack.TryPop(out var i))
            {
                foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
                {
                    var next = i + dy * size + dx;
                    if ((plan.Cables[next] & CityCable.Data) == 0 || !reached.Add(next))
                        continue;

                    stack.Push(next);
                }
            }

            Assert.That(reached.Contains(computer), "the computer is on the router's network");
        }

        // A security computer shows up somewhere, but rarely.
        Assert.That(devices, Is.GreaterThan(0), "no security computer in any of the cities");
        Assert.That(devices, Is.LessThan(residences.Count * Seeds.Length / 4 + 1), "security computers too common");
    }

    /// <summary>
    /// Homes hold more than the bare bed, router and computer: the furniture pass actually sets rooms out.
    /// </summary>
    [Test]
    public void ResidencesAreFurnished([ValueSource(nameof(Seeds))] ulong seed)
    {
        var plan = CityGenerator.Generate(seed);
        var size = plan.Size;
        var extras = 0;
        foreach (var residence in CityGenerator.Residences(plan))
        {
            var tiles = residence.ToHashSet();
            extras += plan.Objects.Count(o => tiles.Contains(o.Y * size + o.X)
                && o.Kind is not (CityObject.Router or CityObject.Computer or CityObject.SecurityComputer
                    or CityObject.Bed or CityObject.Mattress));
        }

        Assert.That(extras, Is.GreaterThan(0), "no furniture beyond beds and devices anywhere");
    }

    /// <summary>
    /// Rooms stay mostly room: no home crowds a plant or a table into most of its floor, however the collapse
    /// leans.
    /// </summary>
    [Test]
    public void ResidencesStayMostlyRoom([ValueSource(nameof(Seeds))] ulong seed)
    {
        var plan = CityGenerator.Generate(seed);
        var size = plan.Size;
        foreach (var residence in CityGenerator.Residences(plan))
        {
            var tiles = residence.ToHashSet();
            var objects = plan.Objects.Where(o => tiles.Contains(o.Y * size + o.X)).ToList();
            int Count(params CityObject[] kinds) => objects.Count(o => kinds.Contains(o.Kind));
            var furniture = objects.Count(o => o.Kind != CityObject.Router
                && o.Kind != CityObject.Computer && o.Kind != CityObject.SecurityComputer);

            Assert.That(furniture, Is.LessThanOrEqualTo(residence.Count / 3 + 2),
                $"{residence.Count} tiles hold {furniture} pieces ({string.Join(", ",
                    objects.GroupBy(o => o.Kind).Select(g => $"{g.Count()} {g.Key}"))})");
            Assert.That(Count(CityObject.Table, CityObject.Chair, CityObject.Stool), Is.LessThanOrEqualTo(residence.Count / 8 + 4),
                $"dining sets stay small: {string.Join(", ",
                    objects.GroupBy(o => o.Kind).Select(g => $"{g.Count()} {g.Key}"))} over "
                + $"{residence.Count} tiles");
            Assert.That(Count(CityObject.Table), Is.LessThanOrEqualTo(1), "one table to a home");
            Assert.That(Count(CityObject.Chair), Is.LessThanOrEqualTo(2), "two chairs to a home");
            Assert.That(Count(CityObject.Stool), Is.LessThanOrEqualTo(2), "two stools to a home");
            Assert.That(Count(CityObject.Plant), Is.LessThanOrEqualTo(1), "one plant to a home");
        }
    }

    /// <summary>
    /// Where a bed stands there's an end table beside it, a dresser in the same room, carpet under the bed and
    /// curtains over the bedroom's windows — all in one of the ten colours — and a mat of the same cloth lies
    /// inside the entry door.
    /// </summary>
    [Test]
    public void ResidencesAreDressed([ValueSource(nameof(Seeds))] ulong seed)
    {
        var plan = CityGenerator.Generate(seed);
        var size = plan.Size;
        bool Window(int x, int y)
        {
            return plan.Contains(x, y)
                   && plan.Structure(x, y) is CityStructure.Window or CityStructure.WindowReinforced
                       or CityStructure.WindowTinted;
        }

        var colours = new HashSet<CityCarpet>();
        foreach (var rooms in CityGenerator.ResidenceRooms(plan))
        {
            var tiles = rooms.SelectMany(r => r).ToHashSet();
            var objects = plan.Objects.Where(o => tiles.Contains(o.Y * size + o.X)).ToList();

            // A mat of carpet inside the entry door: some carpet touches a door of the home.
            var doors = tiles.SelectMany(t =>
                {
                    var (x, y) = (t % size, t / size);
                    return new[] { (x, y + 1), (x + 1, y), (x, y - 1), (x - 1, y) };
                })
                .Where(p => plan.Contains(p.Item1, p.Item2)
                    && plan.Structure(p.Item1, p.Item2) == CityStructure.Door)
                .Select(p => p.Item1 + p.Item2 * size)
                .ToHashSet();
            Assert.That(doors, Is.Not.Empty, "a home without a way in");
            var byDoor = plan.Carpets.Where(c => new[]
                {
                    (c.X, c.Y + 1), (c.X + 1, c.Y), (c.X, c.Y - 1), (c.X - 1, c.Y),
                }
                .Any(p => plan.Contains(p.Item1, p.Item2) && doors.Contains(p.Item2 * size + p.Item1))).ToList();
            Assert.That(byDoor, Is.Not.Empty, "no carpet inside the entry door");
            colours.UnionWith(byDoor.Select(c => c.Colour));

            var bed = objects.FirstOrDefault(o => o.Kind is CityObject.Bed or CityObject.Mattress);
            if (bed.Kind is not (CityObject.Bed or CityObject.Mattress))
                continue;

            // The end table stands beside the bed, unless every tile beside it is a wall, a door, or a device.
            bool Takable(int x, int y)
            {
                return plan.Contains(x, y) && tiles.Contains(y * size + x)
                    && plan.Structure(x, y) == CityStructure.None
                    && objects.All(o => o.X != x || o.Y != y
                        || o.Kind is not (CityObject.Router or CityObject.Computer
                            or CityObject.SecurityComputer or CityObject.Bed or CityObject.Mattress
                            or CityObject.EndTable or CityObject.Dresser));
            }

            var beside = new HashSet<(int, int)>();
            foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
            {
                beside.Add((bed.X + dx, bed.Y + dy));
            }

            Assert.That(objects.Any(o => o.Kind == CityObject.EndTable && beside.Contains((o.X, o.Y)))
                || !beside.Any(p => Takable(p.Item1, p.Item2)), "no end table beside the bed");

            // A dresser somewhere in the bedroom, and carpet under the bed.
            var bedroom = rooms.First(r => r.Contains(bed.Y * size + bed.X));
            Assert.That(objects.Any(o => o.Kind == CityObject.Dresser && bedroom.Contains(o.Y * size + o.X))
                || !bedroom.Any(t => Takable(t % size, t / size)), "no dresser in the bedroom");
            Assert.That(plan.Carpets.Any(c => c.X == bed.X && c.Y == bed.Y), "no carpet under the bed");

            // Curtains over every window of the bedroom, in the home's colour.
            foreach (var tile in bedroom)
            {
                var (x, y) = (tile % size, tile / size);
                foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
                {
                    if (!Window(x + dx, y + dy))
                        continue;

                    Assert.That(plan.Curtains.Any(c => c.X == x + dx && c.Y == y + dy),
                        $"no curtain over the window at {x + dx}, {y + dy}");
                }
            }
        }

        // The ten colours of the cloth-dyers all get used across the city.
        Assert.That(colours, Has.Count.EqualTo(10), string.Join(", ", colours));
    }
}
