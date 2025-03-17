using System.Collections.Generic;
using System.Linq;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.DeviceLinking;

public sealed class DeviceLinkingTest
{
    private const string PortTesterProtoId = "DeviceLinkingSinkPortTester";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {PortTesterProtoId}
  components:
  - type: DeviceLinkSource
    ports:
    - Output
";

    /// <summary>
    /// Spawns every entity that has a <see cref="DeviceLinkSinkComponent"/>
    /// and sends a signal to every port to make sure nothing causes an error.
    /// </summary>
    [Test]
    public async Task AllDeviceLinkSinksWorkTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSys = server.System<SharedMapSystem>();
        var deviceLinkSys = server.System<DeviceLinkSystem>();

        // Find every EntityPrototypes with a DeviceLinkSinkComponent
        var prototypes = server.ProtoMan
            .EnumeratePrototypes<EntityPrototype>()
            .Where(p => !p.Abstract)
            .Where(p => !pair.IsTestPrototype(p))
            .Where(p => p.HasComponent<DeviceLinkSinkComponent>())
            .Select(p => p.ID)
            .Order();

        List<MapId> maps = [];
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var protoId in prototypes)
                {
                    // Create a map for each entity so they can't interfere
                    mapSys.CreateMap(out var mapId);
                    maps.Add(mapId);
                    var grid = mapMan.CreateGridEntity(mapId);
                    mapSys.SetTile(grid.Owner, grid.Comp, Vector2i.Zero, new Tile(1));
                    var coord = new EntityCoordinates(grid.Owner, 0, 0);

                    // Spawn the sink entity
                    var sinkEnt = server.EntMan.SpawnEntity(protoId, coord);
                    Assert.That(server.EntMan.TryGetComponent<DeviceLinkSinkComponent>(sinkEnt, out var sinkComp),
                        $"Prototype {protoId} does not have a DeviceLinkSinkComponent!");

                    // Spawn the tester
                    var sourceEnt = server.EntMan.SpawnEntity(PortTesterProtoId, coord);
                    Assert.That(server.EntMan.TryGetComponent<DeviceLinkSourceComponent>(sourceEnt, out var sourceComp),
                        $"Tester prototype does not have a DeviceLinkSourceComponent!");

                    // Try sending a signal to each port
                    foreach (var port in sinkComp.Ports)
                    {
                        // Create a link from the tester's output to the target port on the sink
                        deviceLinkSys.SaveLinks(null,
                            sourceEnt,
                            sinkEnt,
                            [("Output", port.Id)],
                            sourceComp,
                            sinkComp);

                        // Send a signal to the port
                        deviceLinkSys.InvokePort(sourceEnt, "Output", null, sourceComp);

                        Assert.That(server.EntMan.Deleted(sinkEnt), Is.False, $"{protoId} was deleted after triggering port {port.Id}");
                    }
                }
            });

            // Cleanup created maps
            foreach (var map in maps)
            {
                mapSys.DeleteMap(map);
            }
        });

        await pair.CleanReturnAsync();
    }
}
