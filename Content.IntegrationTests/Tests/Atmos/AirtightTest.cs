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
    private EntityUid _targetRotationEnt = EntityUid.Invalid;

    #region Prototypes

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: AirtightDirectionalRotationTest
  parent: WindowDirectional
  components:
  - type: Airtight
    airBlockedDirection: North
    fixAirBlockedDirectionInitialize: true
    noAirWhenFullyAirBlocked: false
";

    #endregion

    #region Component and Helper Assertions

    /*
     Tests for asserting that proper ComponentInit and other events properly work.
     */

    [Test]
    public async Task Component_InitDataCorrect()
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitPost(delegate
        {
            var coords = new EntityCoordinates(RelevantAtmos.Owner, Vector2.Zero);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        SEntMan.TryGetComponent<AirtightComponent>(_targetWall, out var airtightComp);
        Assert.That(airtightComp, Is.Not.Null, "Expected spawned wall entity to have AirtightComponent.");

        // The data on the component itself should reflect full blockage.
        // It should also hold the proper last position.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(airtightComp.AirBlockedDirection, Is.EqualTo(AtmosDirection.All));
            Assert.That(airtightComp.LastPosition, Is.EqualTo((RelevantAtmos.Owner, Vector2i.Zero)));
        }
    }

    [Test]
    [TestCase(AtmosDirection.North)]
    [TestCase(AtmosDirection.South)]
    [TestCase(AtmosDirection.East)]
    [TestCase(AtmosDirection.West)]
    public async Task MultiTile_Component_InitDataCorrect(AtmosDirection direction)
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        var offsetVec = Vector2i.Zero.Offset(direction);
        await Server.WaitPost(delegate
        {
            var coords = new EntityCoordinates(RelevantAtmos.Owner, offsetVec);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        SEntMan.TryGetComponent<AirtightComponent>(_targetWall, out var airtightComp);
        Assert.That(airtightComp, Is.Not.Null, "Expected spawned wall entity to have AirtightComponent.");

        // The data on the component itself should reflect full blockage.
        // It should also hold the proper last position.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(airtightComp.AirBlockedDirection, Is.EqualTo(AtmosDirection.All));
            Assert.That(airtightComp.LastPosition, Is.EqualTo((RelevantAtmos.Owner, offsetVec)));
        }
    }

    #endregion

    #region Single Tile Assertion

    /*
     Tests for asserting single tile airtightness state on both reconstructed and cached data.
     These tests just spawn a wall in the center and make sure that both reconstructed and cached
     airtight data reflect the expected states both immediately after the action and after an atmos tick.
     */

    /// <summary>
    /// Tests that the reconstructed airtight map reflects properly when an airtight entity is spawned.
    /// </summary>
    [Test]
    public async Task Spawn_ReconstructedUpdatesImmediately()
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        // Before an entity is spawned, the tile in question should be completely unblocked.
        // This should be reflected in a reconstruction.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
                Is.False,
                "Expected no airtightness for reconstructed AirtightData before spawning an airtight entity.");
        }

        // We cannot use the Spawn InteractionTest helper because it runs ticks,
        // which invalidate testing for cached data (ticks would update the cache).
        await Server.WaitPost(delegate
        {
            var coords = new EntityCoordinates(RelevantAtmos.Owner, Vector2.Zero);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        // Now, immediately after spawn, the reconstructed data should reflect airtightness.
        Assert.That(
            SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
            Is.True,
            "Expected airtightness for reconstructed AirtightData immediately after spawn.");
    }

    /// <summary>
    /// Tests that the AirtightData cache updates properly when an airtight entity is spawned.
    /// </summary>
    [Test]
    public async Task Spawn_CacheUpdatesOnAtmosTick()
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        // Space should be blank before spawn.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.False,
                "Expected cached AirtightData to be unblocked before spawning an airtight entity.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to be completely unblocked before spawning an airtight entity.");

            Assert.That(tile.AirtightData.BlockedDirections,
                Is.EqualTo(AtmosDirection.Invalid),
                "Expected AirtightData to reflect non-airtight state before spawning an airtight entity.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                Assert.That(curTile, Is.Not.Null, $"Center tile does not hold expected reference to adjacent tile in direction {direction}.");
            }
        }

        await Server.WaitPost(delegate
        {
            var coords = new EntityCoordinates(RelevantAtmos.Owner, Vector2.Zero);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        // Now, immediately after spawn, the reconstructed data should reflect airtightness,
        // but the cached data should still be stale.
        // This goes the same for the references, which haven't been updated, as well as the AirtightData.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.False,
                "Expected cached AirtightData to remain stale immediately after spawn before atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to still show non-airtight state before an atmos tick.");

            Assert.That(tile.AirtightData.BlockedDirections,
                Is.EqualTo(AtmosDirection.Invalid),
                "Expected AirtightData to reflect non-airtight state after spawn before an atmos tick.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                Assert.That(curTile, Is.Not.Null, $"Center tile does not hold expected reference to adjacent tile in direction {direction}.");
            }
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

            Assert.That(tile.AirtightData.BlockedDirections,
                Is.EqualTo(AtmosDirection.All),
                "Expected AirtightData to reflect airtight state after spawn before an atmos tick.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                Assert.That(curTile, Is.Null, $"Center tile holds unexpected reference to adjacent tile in direction {direction}.");
            }
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

        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        Assert.That(
            SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
            Is.True,
            "Expected airtightness for reconstructed AirtightData before deletion.");

        await Server.WaitPost(delegate
        {
            SEntMan.DeleteEntity(_targetWall);
        });

        Assert.That(
            SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
            Is.False,
            "Expected no airtightness for reconstructed AirtightData immediately after deletion.");

        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        Assert.That(
            SAtmos.IsTileAirBlocked(ProcessEnt.Owner, Vector2i.Zero, mapGridComp: ProcessEnt.Comp3),
            Is.False,
            "Expected no airtightness for reconstructed AirtightData after atmos tick.");
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

        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitPost(delegate
        {
            SEntMan.DeleteEntity(_targetWall);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.True,
                "Expected cached AirtightData to remain stale immediately after deletion before atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.Invalid),
                "Expected tile to still show airtight state before atmos tick after deletion.");

            Assert.That(tile.AirtightData.BlockedDirections,
                Is.EqualTo(AtmosDirection.All),
                "Expected AirtightData to reflect non-airtight state before after deletion before an atmos tick.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                Assert.That(curTile, Is.Null, $"Center tile holds unexpected reference to adjacent tile in direction {direction}.");
            }
        }

        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                SAtmos.IsTileAirBlockedCached(RelevantAtmos, Vector2i.Zero),
                Is.False,
                "Expected cached AirtightData to reflect deletion after atmos tick.");

            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to reflect non-airtight state after atmos tick.");

            Assert.That(tile.AirtightData.BlockedDirections,
                Is.EqualTo(AtmosDirection.Invalid),
                "Expected AirtightData to reflect non-airtight state after atmos tick.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                Assert.That(curTile, Is.Not.Null, $"Center tile does not hold expected reference to adjacent tile in direction {direction}.");
            }
        }
    }

    #endregion

    #region Multi-Tile Assertion

    /*
     Tests for asserting multi-tile airtightness state on cached data.
     These tests spawn multiple entities and check that the center unblocked entity
     properly reflects partial airtightness states.

     Note that reconstruction won't save you in the case where you're surrounded by airtight entities,
     as those don't show up in the reconstruction. Thus, only cached data tests are done here.
     */

    /// <summary>
    /// Tests that the cached airtight map reflects properly when airtight entities are spawned
    /// along the cardinal directions.
    /// </summary>
    /// <param name="atmosDirection">The direction to spawn the airtight entity in.</param>
    [Test]
    [TestCase(AtmosDirection.North)]
    [TestCase(AtmosDirection.South)]
    [TestCase(AtmosDirection.East)]
    [TestCase(AtmosDirection.West)]
    public async Task MultiTile_Spawn_CacheUpdatesOnAtmosTick(AtmosDirection atmosDirection)
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        // Tile should be completely unblocked.
        using (Assert.EnterMultipleScope())
        {
            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to be completely unblocked before spawning an airtight entity.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                Assert.That(curTile, Is.Not.Null, $"Center tile does not hold expected reference to adjacent tile in direction {direction}.");
            }
        }

        await Server.WaitPost(delegate
        {
            var offsetVec = Vector2i.Zero.Offset(atmosDirection);
            var coords = new EntityCoordinates(RelevantAtmos.Owner, offsetVec);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        using (Assert.EnterMultipleScope())
        {
            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to still show non-airtight state before an atmos tick.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                Assert.That(curTile, Is.Not.Null, $"Center tile does not hold expected reference to adjacent tile in direction {direction}.");
            }
        }

        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        using (Assert.EnterMultipleScope())
        {
            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All & ~atmosDirection),
                "Expected tile to reflect airtight state after atmos tick.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                if (direction == atmosDirection)
                {
                    Assert.That(curTile, Is.Null, $"Center tile holds unexpected reference to adjacent tile in direction {direction}.");
                }
                else
                {
                    Assert.That(curTile, Is.Not.Null, $"Center tile does not hold expected reference to adjacent tile in direction {direction}.");
                }
            }
        }
    }

    /// <summary>
    /// Tests that the cached airtight map reflects properly when an airtight entity is deleted
    /// along a cardinal direction.
    /// </summary>
    /// <param name="atmosDirection">The direction the airtight entity is spawned and then deleted in.</param>
    [Test]
    [TestCase(AtmosDirection.North)]
    [TestCase(AtmosDirection.South)]
    [TestCase(AtmosDirection.East)]
    [TestCase(AtmosDirection.West)]
    public async Task MultiTile_Delete_CacheUpdatesOnAtmosTick(AtmosDirection atmosDirection)
    {
        // Ensure grid/atmos is initialized.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitPost(delegate
        {
            var offsetVec = Vector2i.Zero.Offset(atmosDirection);
            var coords = new EntityCoordinates(RelevantAtmos.Owner, offsetVec);
            _targetWall = SEntMan.SpawnAtPosition(_wallProto, coords);
        });

        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitPost(delegate
        {
            SEntMan.DeleteEntity(_targetWall);
        });

        using (Assert.EnterMultipleScope())
        {
            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All & ~atmosDirection),
                "Expected tile to remain stale immediately after deletion before an atmos tick.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                if (direction == atmosDirection)
                {
                    Assert.That(curTile, Is.Null, $"Center tile holds unexpected reference to adjacent tile in direction {direction}.");
                }
                else
                {
                    Assert.That(curTile, Is.Not.Null, $"Center tile does not hold expected reference to adjacent tile in direction {direction}.");
                }
            }
        }

        // Tick to update cache after deletion.
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        using (Assert.EnterMultipleScope())
        {
            var tile = RelevantAtmos.Comp.Tiles[Vector2i.Zero];
            Assert.That(tile.AdjacentBits,
                Is.EqualTo(AtmosDirection.All),
                "Expected tile to reflect non-airtight state after deletion after atmos tick.");

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection)(1 << i);
                var curTile = tile.AdjacentTiles[i];
                Assert.That(curTile, Is.Not.Null, $"Center tile does not hold expected reference to adjacent tile in direction {direction}.");
            }
        }
    }

    #endregion

    #region Rotation Assertion

    /// <summary>
    /// Asserts that an airtight entity with a directional air blocked direction
    /// properly reflects rotation on spawn.
    /// </summary>
    /// <param name="degrees">The degrees to rotate the entity on spawn.</param>
    /// <param name="expected">The expected blocked direction after rotation.</param>
    /// <remarks>Yeah, so here I learned that RT handles rotation directions
    /// as positive == counterclockwise.</remarks>
    [Test]
    [TestCase(0f, AtmosDirection.North)]
    [TestCase(90f, AtmosDirection.West)]
    [TestCase(180f, AtmosDirection.South)]
    [TestCase(270f, AtmosDirection.East)]
    [TestCase(-90f, AtmosDirection.East)]
    [TestCase(-180f, AtmosDirection.South)]
    [TestCase(-270f, AtmosDirection.West)]
    public async Task Rotation_AirBlockedDirectionsOnSpawn(float degrees, AtmosDirection expected)
    {
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        var rotation = Angle.FromDegrees(degrees);

        await Server.WaitPost(delegate
        {
            var coords = new EntityCoordinates(RelevantAtmos.Owner, Vector2.Zero);
            _targetRotationEnt = SEntMan.SpawnAtPosition("AirtightDirectionalRotationTest", coords);

            Transform.SetLocalRotation(_targetRotationEnt, rotation);
        });

        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitAssertion(delegate
        {
            using (Assert.EnterMultipleScope())
            {
                SEntMan.TryGetComponent<AirtightComponent>(_targetRotationEnt, out var airtight);
                Assert.That(airtight, Is.Not.Null);

                var initial = (AtmosDirection)airtight.InitialAirBlockedDirection;
                Assert.That(initial,
                    Is.EqualTo(AtmosDirection.North),
                    "Directional airtight entity should block North on spawn.");

                Assert.That(airtight.AirBlockedDirection,
                    Is.EqualTo(expected),
                    $"Expected AirBlockedDirection to be {expected} after rotating by {degrees} degrees on spawn.");

                // i dont trust you airtightsystem
                if (degrees is 90f or 270f)
                {
                    Assert.That(expected,
                        Is.Not.EqualTo(initial),
                        "Rotated directions should differ for 90/270 degrees.");
                }
            }
        });
    }

    #endregion
}
