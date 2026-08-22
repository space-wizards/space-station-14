using Content.Client.Fluids;
using Content.Client.IconSmoothing;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using ServerPuddleSystem = Content.Server.Fluids.EntitySystems.PuddleSystem;

namespace Content.IntegrationTests.Tests.Fluids;

[TestFixture]
[TestOf(typeof(PuddleColorBlendComponent))]
public sealed class PuddleColorBlendTest : GameTest
{
    [Test]
    public async Task InitializesCardinalDiagonalAndAlphaNeighbors()
    {
        var pair = Pair;
        var server = pair.Server;
        var client = pair.Client;
        var map = await pair.CreateTestMap();
        var mapSystem = server.System<SharedMapSystem>();
        var puddleSystem = server.System<ServerPuddleSystem>();

        var centerPosition = new Vector2i(0, 0);
        var northPosition = new Vector2i(0, 1);
        var northEastPosition = new Vector2i(1, 1);
        var eastPosition = new Vector2i(1, 0);
        var southPosition = new Vector2i(0, -1);

        // Create one connected puddle for each neighbor type plus a small, disconnected puddle.
        await server.WaitPost(() =>
        {
            foreach (var position in new[] { northPosition, northEastPosition, eastPosition, southPosition })
            {
                mapSystem.SetTile(map.Grid, position, new Tile(map.Tile.Tile.TypeId));
            }
        });

        EntityUid center = default;
        EntityUid north = default;
        EntityUid northEast = default;
        EntityUid east = default;
        EntityUid south = default;
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(Spill(centerPosition, "Water", out center), Is.True);
                Assert.That(Spill(northPosition, "Blood", out north), Is.True);
                Assert.That(Spill(northEastPosition, "JuiceWatermelon", out northEast), Is.True);
                Assert.That(Spill(eastPosition, "Vomit", out east), Is.True);
                Assert.That(Spill(southPosition, "Water", out south, 10), Is.True);
            });
        });

        await pair.RunTicksSync(5);

        await client.WaitAssertion(() =>
        {
            var centerBlend = client.EntMan.GetComponent<PuddleColorBlendComponent>(pair.ToClientUid(center));
            var northBlend = client.EntMan.GetComponent<PuddleColorBlendComponent>(pair.ToClientUid(north));
            var northEastBlend = client.EntMan.GetComponent<PuddleColorBlendComponent>(pair.ToClientUid(northEast));
            var eastBlend = client.EntMan.GetComponent<PuddleColorBlendComponent>(pair.ToClientUid(east));
            var southBlend = client.EntMan.GetComponent<PuddleColorBlendComponent>(pair.ToClientUid(south));
            var southSmooth = client.EntMan.GetComponent<IconSmoothComponent>(pair.ToClientUid(south));
            var sprite = client.EntMan.GetComponent<SpriteComponent>(pair.ToClientUid(center));

            Assert.Multiple(() =>
            {
                Assert.That(centerBlend.Shader, Is.Not.Null);
                Assert.That(centerBlend.Shader, Is.Not.SameAs(northBlend.Shader));
                Assert.That(sprite.Color, Is.EqualTo(Color.White));
                AssertNeighbor(centerBlend, PuddleNeighbor.North, northBlend.SelfColor);
                AssertNeighbor(centerBlend, PuddleNeighbor.NorthEast, northEastBlend.SelfColor);
                AssertNeighbor(centerBlend, PuddleNeighbor.East, eastBlend.SelfColor);
                Assert.That(centerBlend.NeighborPresent[(int) PuddleNeighbor.South], Is.Zero);
                Assert.That(centerBlend.NeighborPresent[(int) PuddleNeighbor.SouthWest], Is.Zero);
                Assert.That(centerBlend.NeighborPresent[(int) PuddleNeighbor.West], Is.Zero);
                Assert.That(centerBlend.NeighborPresent[(int) PuddleNeighbor.NorthWest], Is.Zero);
                Assert.That(southSmooth.Enabled, Is.False);
                Assert.That(southBlend.NeighborPresent, Is.All.Zero);

                // Blood has a distinct alpha; it must remain color data rather than a presence sentinel.
                Assert.That(centerBlend.NeighborColors[(int) PuddleNeighbor.North].A,
                    Is.EqualTo(northBlend.SelfColor.A));
            });
        });

        bool Spill(Vector2i position, string reagent, out EntityUid puddle, int amount = 30)
        {
            var tile = mapSystem.GetTileRef(map.Grid.Owner, map.Grid.Comp, position);
            return puddleSystem.TrySpillAt(tile, new Solution(reagent, FixedPoint2.New(amount)), out puddle,
                sound: false, tileReact: false);
        }
    }

    [Test]
    public async Task RefreshesNeighborsAfterThresholdColorAndRemovalChanges()
    {
        var pair = Pair;
        var server = pair.Server;
        var client = pair.Client;
        var map = await pair.CreateTestMap();
        var mapSystem = server.System<SharedMapSystem>();
        var puddleSystem = server.System<ServerPuddleSystem>();

        var centerPosition = new Vector2i(0, 0);
        var southPosition = new Vector2i(0, -1);
        var eastPosition = new Vector2i(1, 0);
        var northEastPosition = new Vector2i(1, 1);

        // Create one connected puddle for each neighbor type plus a small, disconnected puddle.
        await server.WaitPost(() =>
        {
            foreach (var position in new[] { southPosition, eastPosition, northEastPosition })
            {
                mapSystem.SetTile(map.Grid, position, new Tile(map.Tile.Tile.TypeId));
            }
        });

        EntityUid center = default;
        EntityUid south = default;
        EntityUid east = default;
        EntityUid northEast = default;
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(Spill(centerPosition, "Water", out center), Is.True);
                Assert.That(Spill(southPosition, "Water", out south, 10), Is.True);
                Assert.That(Spill(eastPosition, "Vomit", out east), Is.True);
                Assert.That(Spill(northEastPosition, "JuiceWatermelon", out northEast), Is.True);
            });
        });

        await pair.RunTicksSync(5);
        var clientCenter = pair.ToClientUid(center);
        var clientSouth = pair.ToClientUid(south);
        var clientEast = pair.ToClientUid(east);

        // Grow the south puddle past the smoothing threshold.
        await server.WaitAssertion(() => Assert.That(puddleSystem.TryAddSolution(
            south, new Solution("Water", FixedPoint2.New(20)), sound: false, checkForOverflow: false), Is.True));
        await pair.RunTicksSync(5);

        // Its newly enabled smoothing immediately updates the center's south neighbor.
        await client.WaitAssertion(() =>
        {
            var centerBlend = client.EntMan.GetComponent<PuddleColorBlendComponent>(clientCenter);
            var southBlend = client.EntMan.GetComponent<PuddleColorBlendComponent>(clientSouth);
            Assert.That(client.EntMan.GetComponent<IconSmoothComponent>(clientSouth).Enabled, Is.True);
            AssertNeighbor(centerBlend, PuddleNeighbor.South, southBlend.SelfColor);
        });

        Color oldEastColor = default;
        await client.WaitAssertion(() =>
            oldEastColor = client.EntMan.GetComponent<PuddleColorBlendComponent>(clientEast).SelfColor);

        // Mix blood into the east puddle and then remove the north-east puddle.
        await server.WaitAssertion(() => Assert.That(puddleSystem.TryAddSolution(
            east, new Solution("Blood", FixedPoint2.New(10)), sound: false, checkForOverflow: false), Is.True));
        await pair.RunTicksSync(5);

        await client.WaitAssertion(() =>
        {
            var centerBlend = client.EntMan.GetComponent<PuddleColorBlendComponent>(clientCenter);
            var eastBlend = client.EntMan.GetComponent<PuddleColorBlendComponent>(clientEast);
            Assert.That(eastBlend.SelfColor, Is.Not.EqualTo(oldEastColor));
            AssertNeighbor(centerBlend, PuddleNeighbor.East, eastBlend.SelfColor);
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(northEast));
        await pair.RunTicksSync(5);

        // Deleting a connected puddle clears the matching neighbor slot.
        await client.WaitAssertion(() => Assert.That(
            client.EntMan.GetComponent<PuddleColorBlendComponent>(clientCenter)
                .NeighborPresent[(int) PuddleNeighbor.NorthEast], Is.Zero));

        bool Spill(Vector2i position, string reagent, out EntityUid puddle, int amount = 30)
        {
            var tile = mapSystem.GetTileRef(map.Grid.Owner, map.Grid.Comp, position);
            return puddleSystem.TrySpillAt(tile, new Solution(reagent, FixedPoint2.New(amount)), out puddle,
                sound: false, tileReact: false);
        }
    }

    [Test]
    public async Task RemovingBlendComponentRestoresSpriteTint()
    {
        var pair = Pair;
        var server = pair.Server;
        var client = pair.Client;
        var map = await pair.CreateTestMap();
        var mapSystem = server.System<SharedMapSystem>();
        var puddleSystem = server.System<ServerPuddleSystem>();
        EntityUid puddle = default;

        // Create a normal puddle with a blend shader.
        await server.WaitAssertion(() =>
        {
            var tile = mapSystem.GetTileRef(map.Grid.Owner, map.Grid.Comp, Vector2i.Zero);
            Assert.That(puddleSystem.TrySpillAt(tile, new Solution("Water", FixedPoint2.New(30)), out puddle,
                sound: false, tileReact: false), Is.True);
        });

        await pair.RunTicksSync(5);
        var clientPuddle = pair.ToClientUid(puddle);
        Color puddleColor = default;
        await client.WaitAssertion(() =>
            puddleColor = client.EntMan.GetComponent<PuddleColorBlendComponent>(clientPuddle).SelfColor);

        // Remove the client-only component that owns the unique shader instance.
        await client.WaitPost(() => client.EntMan.RemoveComponent<PuddleColorBlendComponent>(clientPuddle));

        // The original sprite tint is restored when blending is removed.
        await client.WaitAssertion(() =>
        {
            Assert.That(client.EntMan.HasComponent<PuddleColorBlendComponent>(clientPuddle), Is.False);
            Assert.That(client.EntMan.GetComponent<SpriteComponent>(clientPuddle).Color, Is.EqualTo(puddleColor));
        });
    }

    private static void AssertNeighbor(PuddleColorBlendComponent blend, PuddleNeighbor neighbor, Color expected)
    {
        var index = (int) neighbor;
        Assert.Multiple(() =>
        {
            Assert.That(blend.NeighborPresent[index], Is.EqualTo(1f));
            Assert.That(blend.NeighborColors[index], Is.EqualTo(expected));
        });
    }
}
