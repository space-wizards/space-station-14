using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Tests for AtmosphereSystem.DeltaPressure and surrounding systems
/// handling the DeltaPressureComponent.
/// </summary>
[TestFixture]
[TestOf(typeof(DeltaPressureSystem))]
public sealed class DeltaPressureTest
{
    #region Prototypes

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: BaseStructure
  id: DeltaPressureSolidTest
  placement:
    mode: SnapgridCenter
    snap:
    - Wall
  components:
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
          bounds: ""-0.5,-0.5,0.5,0.5""
        mask:
        - FullTileMask
        layer:
        - WallLayer
        density: 1000
  - type: Airtight
  - type: DeltaPressure
    minPressure: 15000
    minPressureDelta: 10000
    scalingType: Threshold

- type: entity
  parent: DeltaPressureSolidTest
  id: DeltaPressureSolidTestNoAutoJoin
  components:
  - type: DeltaPressure
    autoJoinProcessingList: false
";

    #endregion

    private readonly ResPath _testMap = new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    /// <summary>
    /// Asserts that an entity with a DeltaPressureComponent with autoJoinProcessingList
    /// set to true is automatically added to the DeltaPressure processing list
    /// on the grid's GridAtmosphereComponent.
    ///
    /// Also asserts that an entity with a DeltaPressureComponent with autoJoinProcessingList
    /// set to false is not automatically added to the DeltaPressure processing list.
    /// </summary>
    [Test]
    public async Task ProcessingListAutoJoinTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var mapLoader = entMan.System<MapLoaderSystem>();
        var mapSys = entMan.System<SharedMapSystem>();
        var atmosphereSystem = entMan.System<AtmosphereSystem>();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = true };

        Entity<MapGridComponent> grid = default;
        Entity<DeltaPressureComponent> dpEnt = default;

        // Load our test map in and assert that it exists.
        await server.WaitPost(() =>
        {
#pragma warning disable NUnit2045
            Assert.That(mapLoader.TryLoadMap(_testMap, out _, out var gridSet, deserializationOptions),
                $"Failed to load map {_testMap}.");
            Assert.That(gridSet, Is.Not.EqualTo(null), nameof(gridSet) + " != null");
#pragma warning restore NUnit2045

            // We already assert this but its high, so whatever
            // ReSharper disable once AssignNullToNotNullAttribute
            grid = gridSet.First();
        });

        await server.WaitAssertion(() =>
        {
            var uid = entMan.SpawnAtPosition("DeltaPressureSolidTest", new EntityCoordinates(grid.Owner, Vector2.Zero));
            dpEnt = new Entity<DeltaPressureComponent>(uid, entMan.GetComponent<DeltaPressureComponent>(uid));

            Assert.That(atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt));
            entMan.DeleteEntity(uid);
            Assert.That(!atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ProcessingListJoinLeaveTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var mapLoader = entMan.System<MapLoaderSystem>();
        var mapSys = entMan.System<SharedMapSystem>();
        var atmosphereSystem = entMan.System<AtmosphereSystem>();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = true };

        Entity<MapGridComponent> grid = default;
        Entity<DeltaPressureComponent> dpEnt = default;

        // Load our test map in and assert that it exists.
        await server.WaitPost(() =>
        {
#pragma warning disable NUnit2045
            Assert.That(mapLoader.TryLoadMap(_testMap, out _, out var gridSet, deserializationOptions),
                $"Failed to load map {_testMap}.");
            Assert.That(gridSet, Is.Not.EqualTo(null), nameof(gridSet) + " != null");
#pragma warning restore NUnit2045

            // We already assert this but its high, so whatever
            // ReSharper disable once AssignNullToNotNullAttribute
            grid = gridSet.First();
        });

        await server.WaitAssertion(() =>
        {
            var uid = entMan.SpawnAtPosition("DeltaPressureSolidTestNoAutoJoin", new EntityCoordinates(grid.Owner, Vector2.Zero));
            dpEnt = new Entity<DeltaPressureComponent>(uid, entMan.GetComponent<DeltaPressureComponent>(uid));

            // YUP
            Assert.That(!atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt));
            atmosphereSystem.TryAddDeltaPressureEntity(grid.Owner, dpEnt);
            Assert.That(atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt));
            atmosphereSystem.TryRemoveDeltaPressureEntity(grid.Owner, dpEnt);
            Assert.That(!atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt));
        });

        await pair.CleanReturnAsync();
    }
}

