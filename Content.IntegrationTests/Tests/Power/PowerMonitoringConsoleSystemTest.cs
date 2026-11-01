using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Coordinates;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Power;

[TestFixture]
public sealed class PowerMonitoringConsoleSystemTest : GameTest
{
    [SidedDependency(Side.Server)] private SharedMapSystem _mapSys = default!;

    /// <summary>
    /// Ensures that the power monitoring console properly handles monitored devices breaking on consecutive ticks.
    /// </summary>
    [Test]
    public async Task TestConsoleUpdateStability()
    {
        Queue<EntityUid> panels = [];

        // Prepare test grid; we're just connecting two solar panels together with HV cable to create a very simple
        // power network.
        await Server.WaitPost(() =>
        {
            _mapSys.CreateMap(out var mapId);
            var grid = _mapSys.CreateGridEntity(mapId);
            for (var i = 0; i < 2; i++)
            {
                _mapSys.SetTile(grid, new Vector2i(0, i), new Tile(1));
                var origin = grid.Owner.ToCoordinates(0, i);
                SEntMan.SpawnAttachedTo("CableHV", origin);
                panels.Enqueue(SEntMan.SpawnAttachedTo("SolarPanel", origin));
            }
        });

        // Wait for power net to refresh
        await Server.WaitRunTicks(1);

        // Delete solar panels in creation order to force grouping master changes on consecutive ticks
        while (panels.Count > 0)
        {
            await Server.WaitAssertion(() =>
            {
                var panel = panels.Dequeue();
                // Queueing deletion instead of deleting immediately because this is ultimately what happens when a
                // solar panel gets destroyed from damage, we're just bypassing a bunch of intermediate systems
                // interactions (DamageableSystem->DestructibleSystem->ConstructionSystem (via
                // ChangeConstructionNodeBehavior)).
                SQueueDel(panel);
            });
            // Update should succeed
            Assert.DoesNotThrowAsync(() => Server.WaitRunTicks(1));
        }
    }
}
