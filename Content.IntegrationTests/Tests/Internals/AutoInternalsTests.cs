#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Internals;

[TestOf(typeof(InternalsSystem))]
public sealed class AutoInternalsTests : GameTest
{
    private const string TestInternalsDummy = "TestInternalsDummy";
    private const string TestInternalsDummyEntity = "TestInternalsDummyEntity";

    [SidedDependency(Side.Server)] private StationSpawningSystem _sStationSpawning = null!;
    [SidedDependency(Side.Server)] private AtmosphereSystem _sAtmos = null!;
    [SidedDependency(Side.Server)] private InternalsSystem _sInternals = null!;

    [Test]
    [Description($"Checks that a player mob spawned in space using {nameof(StationSpawningSystem)} automatically turns on internals.")]
    public async Task TestInternalsAutoActivateInSpaceForStationSpawn()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var profile = new HumanoidCharacterProfile();
            var dummy = _sStationSpawning.SpawnPlayerMob(TestMap!.GridCoords, TestInternalsDummy, profile, station: null);

            Assert.That(_sAtmos.HasAtmosphere(TestMap.Grid), Is.False, "Test map has atmosphere - test needs adjustment!");
            Assert.That(_sInternals.AreInternalsWorking(dummy), "Internals did not automatically connect!");

            SEntMan.DeleteEntity(dummy);
        });
    }

    [Test]
    [Description($"Checks that a player mob spawned in space using {nameof(EntityManager.SpawnAtPosition)} automatically turns on internals.")]
    public async Task TestInternalsAutoActivateInSpaceForEntitySpawn()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var dummy = SSpawnAtPosition(TestInternalsDummyEntity, TestMap!.GridCoords);

            Assert.That(_sAtmos.HasAtmosphere(TestMap.Grid), Is.False, "Test map has atmosphere - test needs adjustment!");
            Assert.That(_sInternals.AreInternalsWorking(dummy), "Internals did not automatically connect!");
        });
    }

    [TestPrototypes]
    private const string Prototypes = $@"
- type: playTimeTracker
  id: PlayTimeInternalsDummy

- type: startingGear
  id: InternalsDummyGear
  equipment:
    mask: ClothingMaskBreath
    suitstorage: OxygenTankFilled

- type: job
  id: {TestInternalsDummy}
  playTimeTracker: PlayTimeInternalsDummy
  startingGear: InternalsDummyGear

- type: entity
  id: {TestInternalsDummyEntity}
  parent: MobHuman
  components:
    - type: Loadout
      prototypes: [InternalsDummyGear]
";
}
