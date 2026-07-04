using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.DeviceLinking;

public sealed class DeviceLinkingTest : GameTest
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

    private static string[] _entitiesWithDeviceLinkSink = GameDataScrounger.EntitiesWithComponent("DeviceLinkSink");

    [Test]
    [TestOf(typeof(DeviceLinkSinkComponent))]
    [TestCaseSource(nameof(_entitiesWithDeviceLinkSink))]
    [Description("Ensures all devices that can sink signals will not cause exceptions when signaled.")]
    public async Task DeviceLinkSinkAllPortsTest(string protoKey)
    {
        var pair = Pair;
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var compFact = server.ResolveDependency<IComponentFactory>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSys = server.System<SharedMapSystem>();
        var deviceLinkSys = server.System<DeviceLinkSystem>();

        await server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                var proto = protoMan.Index(protoKey);
                Assert.That(proto.TryGetComponent<DeviceLinkSinkComponent>(out var protoSinkComp, compFact));

                foreach (var port in protoSinkComp!.Ports)
                {
                    // Create a map for each entity/port combo so they can't interfere
                    mapSys.CreateMap(out var mapId);
                    var grid = mapMan.CreateGridEntity(mapId);
                    mapSys.SetTile(grid.Owner, grid.Comp, Vector2i.Zero, new Tile(1));
                    var coord = new EntityCoordinates(grid.Owner, 0, 0);

                    // Spawn the sink entity
                    var sinkEnt = server.EntMan.SpawnEntity(proto.ID, coord);
                    // Get the actual sink component, since the one we got from the prototype isn't initialized.
                    var sinkComp = server.EntMan.GetComponent<DeviceLinkSinkComponent>(sinkEnt);

                    // Spawn the tester
                    var sourceEnt = server.EntMan.SpawnEntity(PortTesterProtoId, coord);
                    var sourceComp = server.EntMan.GetComponent<DeviceLinkSourceComponent>(sourceEnt);

                    // Create a link from the tester's output to the target port on the sink
                    deviceLinkSys.SaveLinks(null,
                        sourceEnt,
                        sinkEnt,
                        [("Output", port.Id)],
                        sourceComp,
                        sinkComp);

                    // Send a signal to the port
                    Assert.DoesNotThrow(() => { deviceLinkSys.InvokePort(sourceEnt, "Output", null, sourceComp); },
                        $"Exception thrown while triggering port {port.Id} of the sink device.");

                    mapSys.DeleteMap(mapId);
                }
            }
        });
    }
}
