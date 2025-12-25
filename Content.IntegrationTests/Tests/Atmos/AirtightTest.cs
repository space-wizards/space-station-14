using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Mega-testclass for testing <see cref="AirtightSystem"/> and <see cref="AirtightComponent"/>.
/// </summary>
[TestOf(typeof(AirtightSystem))]
[TestOf(typeof(AtmosphereSystem))]
public sealed class AirtightTest : AtmosTest
{
    // Load the same DeltaPressure test because it's quite a useful testmap for testing airtightness.
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    private readonly EntProtoId _wallProto = new("WallSolid");

    private EntityUid _targetWall = EntityUid.Invalid;

    // TODO ATMOS TESTS:
    // - Airtightmap reconstruction/deletion on entities with different blocked directions along with cache reflection
    // - Airtightmap entity rotation
    // - Airtightmap entity movement
    // - Public methods

    /// <summary>
    /// Tests that the reconstructed airtight map reflects properly when an airtight entity is spawned.
    /// </summary>
    [Test]
    public async Task Spawn_ReconstructedUpdatesImmediately()
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
                Is.False,
                "Expected no airtightness for reconstructed AirtightData before spawning an airtight entity.");

            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.False,
                "Expected no airtightness for cached AirtightData before spawning an airtight entity.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to reflect non-airtight state before spawning an airtight entity.");
        }

        // We cannot use the Spawn InteractionTest helper because it runs ticks,
        // which invalidate testing for cached data (ticks would update the cache).
        await Server.WaitPost(delegate
        {
            var coords = new EntityCoordinates(RelevantAtmos.Owner, Vector2.Zero);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        SEntMan.TryGetComponent<AirtightComponent>(_targetWall, out var airtightComp);
        Assert.That(airtightComp, Is.Not.Null, "Expected spawned wall entity to have AirtightComponent.");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(airtightComp.AirBlockedDirection, Is.EqualTo(AtmosDirection.All));
            Assert.That(airtightComp.LastPosition, Is.EqualTo((RelevantAtmos.Owner, Vector2i.Zero)));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
                Is.True,
                "Expected airtightness for reconstructed AirtightData immediately after spawn.");

            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.False,
                "Expected cached AirtightData to remain stale immediately after spawn before atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to still show non-airtight state before an atmos tick.");
        }
    }

    /// <summary>
    /// Tests that the AirtightData cache updates properly when an airtight entity is spawned.
    /// </summary>
    [Test]
    public async Task Spawn_CacheUpdatesOnAtmosTick()
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitPost(delegate
        {
            var coords = new EntityCoordinates(RelevantAtmos.Owner, Vector2.Zero);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
                Is.True,
                "Expected airtightness for reconstructed AirtightData after spawn.");

            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.False,
                "Expected cached AirtightData to be stale after spawn before atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to still show non-airtight state before an atmos tick.");
        }

        // Tick to update cache.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
                Is.True,
                "Expected airtightness for reconstructed AirtightData after atmos tick.");

            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.True,
                "Expected cached AirtightData to reflect airtightness after atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.Invalid),
                "Expected tile to reflect airtight state after atmos tick.");
        }
    }

    /// <summary>
    /// Tests that an airtight reconstruction reflects properly after an entity is deleted.
    /// </summary>
    [Test]
    public async Task Delete_ReconstructedUpdatesImmediately()
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitPost(delegate
        {
            var coords = new EntityCoordinates(RelevantAtmos.Owner, Vector2.Zero);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        // Tick once so cache matches "spawned" state.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
                Is.True,
                "Expected airtightness for reconstructed AirtightData after atmos tick.");

            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.True,
                "Expected cached AirtightData to reflect airtightness after atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.Invalid),
                "Expected tile to reflect airtight state after atmos tick.");
        }

        await Server.WaitPost(delegate
        {
            SEntMan.DeleteEntity(_targetWall);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
                Is.False,
                "Expected no airtightness for reconstructed AirtightData immediately after deletion.");

            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.True,
                "Expected cached AirtightData to remain stale immediately after deletion before atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.Invalid),
                "Expected tile to still show airtight state before atmos tick after deletion.");
        }
    }

    /// <summary>
    /// Tests that the cached airtight map reflects properly when an entity is deleted
    /// </summary>
    [Test]
    public async Task Delete_CacheUpdatesOnAtmosTick()
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitPost(delegate
        {
            var coords = new EntityCoordinates(RelevantAtmos.Owner, Vector2.Zero);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        // Tick once so cache matches "spawned" state.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitPost(delegate
        {
            SEntMan.DeleteEntity(_targetWall);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
                Is.False,
                "Expected no airtightness for reconstructed AirtightData after deletion.");

            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.True,
                "Expected cached AirtightData to still show airtightness after deletion before atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.Invalid),
                "Expected tile to still show airtight state before an atmos tick after deletion.");
        }

        // Tick to update cache to deletion state.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
                Is.False,
                "Expected no airtightness for reconstructed AirtightData after deletion.");

            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.False,
                "Expected cached AirtightData to reflect deletion after atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to reflect non-airtight state after atmos tick.");
        }
    }
}
