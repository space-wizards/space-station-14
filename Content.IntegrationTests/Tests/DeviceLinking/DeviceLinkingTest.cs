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
        var compFact = server.ResolveDependency<IComponentFactory>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSys = server.System<SharedMapSystem>();
        var deviceLinkSys = server.System<DeviceLinkSystem>();

        var prototypes = server.ProtoMan.EnumeratePrototypes<EntityPrototype>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in prototypes)
                {
                    if (proto.Abstract || pair.IsTestPrototype(proto))
                        continue;

                    if (!proto.TryGetComponent<DeviceLinkSinkComponent>(out var protoSinkComp, compFact))
                        continue;

                    foreach (var port in protoSinkComp.Ports)
                    {
                        // Create a map for each entity/port combo so they can't interfere
                        mapSys.CreateMap(out var mapId);
                        var grid = mapMan.CreateGridEntity(mapId);
                        mapSys.SetTile(grid.Owner, grid.Comp, Vector2i.Zero, new Tile(1));
                        var coord = new EntityCoordinates(grid.Owner, 0, 0);

                        // Spawn the sink entity
                        var sinkEnt = server.EntMan.SpawnEntity(proto.ID, coord);
                        // Get the actual sink component, since the one we got from the prototype doesn't have its owner set up
                        Assert.That(server.EntMan.TryGetComponent<DeviceLinkSinkComponent>(sinkEnt, out var sinkComp),
                            $"{proto.ID} does not have a DeviceLinkSinkComponent!");

                        // Spawn the tester
                        var sourceEnt = server.EntMan.SpawnEntity(PortTesterProtoId, coord);
                        Assert.That(server.EntMan.TryGetComponent<DeviceLinkSourceComponent>(sourceEnt, out var sourceComp),
                            $"Tester prototype does not have a DeviceLinkSourceComponent!");

                        // Create a link from the tester's output to the target port on the sink
                        deviceLinkSys.SaveLinks(null,
                            sourceEnt,
                            sinkEnt,
                            [("Output", port.Id)],
                            sourceComp,
                            sinkComp);

                        // Send a signal to the port
                        Assert.DoesNotThrow(() => { deviceLinkSys.InvokePort(sourceEnt, "Output", null, sourceComp); },
                            $"Exception thrown while triggering port {port.Id} of sink device {proto.ID}");

                        mapSys.DeleteMap(mapId);
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
