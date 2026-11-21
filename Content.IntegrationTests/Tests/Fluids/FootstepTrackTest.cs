using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.Chemistry.TileReactions;
using Content.Server.Decals;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Decals;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Fluids;

[TestFixture]
[TestOf(typeof(FootstepTrackSystem))]
public sealed class FootstepTrackTest : GameTest
{
    private static readonly ProtoId<ReagentPrototype> Blood = "Blood";
    private const string TrackerPrototype = "FootstepTrackTestTracker";
    private const string WearerPrototype = "FootstepTrackTestWearer";
    private const string ShoePrototype = "FootstepTrackTestShoe";
    private const string BloodPuddlePrototype = "FootstepTrackTestBloodPuddle";
    private static readonly FixedPoint2 BloodVolume = 30;

    [TestPrototypes]
    private static readonly string Prototypes = @$"
- type: entity
  id: {TrackerPrototype}
  components:
  - type: GravityAffected
  - type: FootstepTrack
    maxSteps: 8

- type: entity
  parent: InventoryBase
  id: {WearerPrototype}
  components:
  - type: Sprite
  - type: ContainerContainer

- type: entity
  id: {ShoePrototype}
  components:
  - type: Clothing
    slots:
    - FEET
  - type: FootstepTrack
    maxSteps: 8
    footprints:
    - BloodPawprint1
    - BloodPawprint2

- type: entity
  parent: Puddle
  id: {BloodPuddlePrototype}
  components:
  - type: Solution
    id: puddle
    solution:
      maxVol: 1000
      reagents:
      - ReagentId: {Blood}
        Quantity: {BloodVolume}
";

    [Test]
    public async Task FootprintsFadeOutAfterConfiguredSteps()
    {
        var testMap = await Pair.CreateTestMap();
        EntityUid tracker = default;

        await Server.WaitAssertion(() =>
        {
            var footprints = SEntMan.System<FootstepTrackSystem>();
            var map = SEntMan.System<SharedMapSystem>();
            tracker = SSpawnAtPosition(TrackerPrototype, testMap.GridCoords);
            var puddle = SSpawnAtPosition(BloodPuddlePrototype, testMap.GridCoords);

            for (var i = 1; i <= 9; i++)
            {
                map.SetTile(testMap.Grid, new Vector2i(i, 0), testMap.Tile.Tile);
            }

            Assert.That(footprints.TryPickupBloodFromPuddle(
                SEntity<PuddleComponent>(puddle),
                SEntity<FootstepTrackComponent>(tracker)), Is.True);
        });

        for (var i = 1; i <= 9; i++)
        {
            await Server.WaitPost(() =>
            {
                var transform = SEntMan.System<SharedTransformSystem>();
                transform.SetCoordinates(tracker, new EntityCoordinates(testMap.Grid.Owner, new Vector2(i + 0.1f, 0.1f)));
            });

            await Pair.RunTicksSync(1);
        }

        await Server.WaitAssertion(() =>
        {
            var decals = SEntMan.System<DecalSystem>();
            var footprints = decals.GetDecalsIntersecting(testMap.Grid, new Box2(0, -1, 10, 1))
                .Select(x => x.Decal)
                .Where(x => x.Id is "BloodFootprint1" or "BloodFootprint2")
                .OrderBy(x => x.Coordinates.X)
                .ToArray();

            Assert.That(footprints, Has.Length.EqualTo(8));

            for (var i = 0; i < footprints.Length; i++)
            {
                Assert.That(footprints[i].Coordinates, Is.EqualTo(new Vector2(i + 1, 0)));
                Assert.That(footprints[i].Id, Is.EqualTo(i % 2 == 0 ? "BloodFootprint1" : "BloodFootprint2"));
                Assert.That(footprints[i].Cleanable, Is.True);
                Assert.That(footprints[i].Angle.Degrees, Is.EqualTo(90).Within(0.001f));

                var expectedAlpha = (8 - i) / 8f;
                Assert.That(footprints[i].Color!.Value.A, Is.EqualTo(expectedAlpha).Within(0.001f));
            }

            Assert.That(SComp<FootstepTrackComponent>(tracker).StepsRemaining, Is.Zero);
        });
    }

    [Test]
    public async Task EquippedShoesTrackWearerAndUseTheirConfiguredDecals()
    {
        var testMap = await Pair.CreateTestMap();
        EntityUid wearer = default;
        EntityUid shoes = default;

        await Server.WaitAssertion(() =>
        {
            var footprints = SEntMan.System<FootstepTrackSystem>();
            var inventory = SEntMan.System<InventorySystem>();
            var map = SEntMan.System<SharedMapSystem>();
            wearer = SSpawnAtPosition(WearerPrototype, testMap.GridCoords);
            shoes = SSpawnAtPosition(ShoePrototype, testMap.GridCoords);
            var puddle = SSpawnAtPosition(BloodPuddlePrototype, testMap.GridCoords);

            map.SetTile(testMap.Grid, new Vector2i(1, 0), testMap.Tile.Tile);
            Assert.That(inventory.TryEquip(wearer, shoes, "shoes"), Is.True);
            Assert.That(footprints.TryPickupBloodFromPuddle(
                SEntity<PuddleComponent>(puddle),
                SEntity<FootstepTrackComponent>(shoes),
                wearer), Is.True);
        });

        await Server.WaitPost(() =>
        {
            var transform = SEntMan.System<SharedTransformSystem>();
            transform.SetCoordinates(wearer, new EntityCoordinates(testMap.Grid.Owner, new Vector2(1.1f, 0.1f)));
        });

        await Pair.RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            var decals = SEntMan.System<DecalSystem>();
            Assert.That(SComp<FootstepTrackComponent>(shoes).StepsRemaining, Is.EqualTo(7));

            var footprint = decals.GetDecalsIntersecting(testMap.Grid, new Box2(1, -1, 2, 1))
                .Select(x => x.Decal)
                .Single(x => x.Id is "BloodPawprint1");

            Assert.That(footprint.Coordinates, Is.EqualTo(new Vector2(1, 0)));
            Assert.That(SComp<FootstepTrackComponent>(shoes).StepsRemaining, Is.EqualTo(7));
        });
    }

    [Test]
    public async Task PuddleTilesConsumeStepsWithoutSpawningFootprints()
    {
        var testMap = await Pair.CreateTestMap();
        EntityUid tracker = default;

        await Server.WaitAssertion(() =>
        {
            var footprints = SEntMan.System<FootstepTrackSystem>();
            var map = SEntMan.System<SharedMapSystem>();
            tracker = SSpawnAtPosition(TrackerPrototype, testMap.GridCoords);
            var pickupPuddle = SSpawnAtPosition(BloodPuddlePrototype, testMap.GridCoords);

            map.SetTile(testMap.Grid, new Vector2i(1, 0), testMap.Tile.Tile);
            map.SetTile(testMap.Grid, new Vector2i(2, 0), testMap.Tile.Tile);
            SSpawnAtPosition(BloodPuddlePrototype, new EntityCoordinates(testMap.Grid.Owner, new Vector2(1.1f, 0.1f)));

            Assert.That(footprints.TryPickupBloodFromPuddle(
                SEntity<PuddleComponent>(pickupPuddle),
                SEntity<FootstepTrackComponent>(tracker)), Is.True);
        });

        for (var i = 1; i <= 2; i++)
        {
            await Server.WaitPost(() =>
            {
                var transform = SEntMan.System<SharedTransformSystem>();
                transform.SetCoordinates(tracker, new EntityCoordinates(testMap.Grid.Owner, new Vector2(i + 0.1f, 0.1f)));
            });

            await Pair.RunTicksSync(1);
        }

        await Server.WaitAssertion(() =>
        {
            var decals = SEntMan.System<DecalSystem>();
            var footprints = decals.GetDecalsIntersecting(testMap.Grid, new Box2(0, -1, 3, 1))
                .Select(x => x.Decal)
                .Where(x => x.Id is "BloodFootprint1" or "BloodFootprint2")
                .OrderBy(x => x.Coordinates.X)
                .ToArray();

            Assert.That(footprints, Has.Length.EqualTo(1));
            Assert.That(footprints[0].Coordinates, Is.EqualTo(new Vector2(2, 0)));
            Assert.That(footprints[0].Color!.Value.A, Is.EqualTo(7f / 8f).Within(0.001f));
            Assert.That(SComp<FootstepTrackComponent>(tracker).StepsRemaining, Is.EqualTo(6));
        });
    }

    [Test]
    public async Task FootprintsReplaceSameWhenAlphaIsHigher()
    {
        var testMap = await Pair.CreateTestMap();
        EntityUid tracker = default;

        await Server.WaitAssertion(() =>
        {
            var footprints = SEntMan.System<FootstepTrackSystem>();
            var decals = SEntMan.System<DecalSystem>();
            var map = SEntMan.System<SharedMapSystem>();
            tracker = SSpawnAtPosition(TrackerPrototype, testMap.GridCoords);
            var puddle = SSpawnAtPosition(BloodPuddlePrototype, testMap.GridCoords);

            map.SetTile(testMap.Grid, new Vector2i(1, 0), testMap.Tile.Tile);
            map.SetTile(testMap.Grid, new Vector2i(2, 0), testMap.Tile.Tile);

            var tileOneCorner = new EntityCoordinates(testMap.Grid.Owner, new Vector2(1, 0));
            Assert.That(decals.TryAddDecal(
                "BloodFootprint1",
                tileOneCorner,
                out _,
                color: Color.Red.WithAlpha(0.25f),
                rotation: Angle.FromDegrees(90)), Is.True);
            Assert.That(decals.TryAddDecal(
                "BloodFootprint2",
                tileOneCorner,
                out _,
                color: Color.Red.WithAlpha(0.5f)), Is.True);

            Assert.That(footprints.TryPickupBloodFromPuddle(
                SEntity<PuddleComponent>(puddle),
                SEntity<FootstepTrackComponent>(tracker)), Is.True);
        });

        foreach (var x in new[] { 1, 2, 1 })
        {
            await Server.WaitPost(() =>
            {
                var transform = SEntMan.System<SharedTransformSystem>();
                transform.SetCoordinates(tracker, new EntityCoordinates(testMap.Grid.Owner, new Vector2(x + 0.1f, 0.1f)));
            });

            await Pair.RunTicksSync(1);
        }

        await Server.WaitAssertion(() =>
        {
            var decals = SEntMan.System<DecalSystem>();
            var tileOne = new Vector2(1, 0);
            var tileOneFootprints = decals.GetDecalsIntersecting(testMap.Grid, new Box2(1, -1, 2, 1))
                .Select(x => x.Decal)
                .Where(x => x.Id is "BloodFootprint1" or "BloodFootprint2")
                .Where(x => x.Coordinates == tileOne)
                .ToArray();
            var matchingIdFootprints = tileOneFootprints
                .Where(x => x.Id == "BloodFootprint1")
                .OrderByDescending(x => x.Color!.Value.A)
                .ToArray();
            var replacedFootprint = matchingIdFootprints[0];
            var differentAngleFootprint = matchingIdFootprints[1];
            var differentFootprint = tileOneFootprints.Single(x => x.Id == "BloodFootprint2");

            Assert.Multiple(() =>
            {
                Assert.That(tileOneFootprints, Has.Length.EqualTo(3));
                Assert.That(matchingIdFootprints, Has.Length.EqualTo(2));
                Assert.That(replacedFootprint.Color!.Value.A, Is.EqualTo(1f).Within(0.001f));
                Assert.That(replacedFootprint.Angle.Degrees, Is.EqualTo(90).Within(0.001f));
                Assert.That(replacedFootprint.Cleanable, Is.True);
                Assert.That(differentAngleFootprint.Color!.Value.A, Is.EqualTo(6f / 8f).Within(0.001f));
                Assert.That(Math.Abs(differentAngleFootprint.Angle.Degrees - 90), Is.GreaterThan(0.001f));
                Assert.That(differentFootprint.Color!.Value.A, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(SComp<FootstepTrackComponent>(tracker).StepsRemaining, Is.EqualTo(5));
            });
        });
    }
}
