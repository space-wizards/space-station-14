#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
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

    private static readonly string[] EntitiesWithDeviceLinkSink = GameDataScrounger.EntitiesWithComponent("DeviceLinkSink");

    [SidedDependency(Side.Server)] private IMapManager _sMapManager = null!;
    [SidedDependency(Side.Server)] private SharedMapSystem _sMapSystem = null!;
    [SidedDependency(Side.Server)] private DeviceLinkSystem _sDeviceLinkSystem = null!;

    [Test]
    [TestOf(typeof(DeviceLinkSinkComponent))]
    [TestCaseSource(nameof(EntitiesWithDeviceLinkSink))]
    [Description("Ensures all devices that can sink signals will not cause exceptions when signaled.")]
    [RunOnSide(Side.Server)]
    public async Task DeviceLinkSinkAllPortsTest(string protoKey)
    {
        var proto = SProtoMan.Index(protoKey);
        Assert.That(proto.TryGetComponent<DeviceLinkSinkComponent>(out var protoSinkComp, SEntMan.ComponentFactory));

        using (Assert.EnterMultipleScope())
        {
            foreach (var port in protoSinkComp!.Ports)
            {
                // Create a map for each entity/port combo so they can't interfere
                _sMapSystem.CreateMap(out var mapId);
                var grid = _sMapManager.CreateGridEntity(mapId);
                _sMapSystem.SetTile(grid.Owner, grid.Comp, Vector2i.Zero, new Tile(1));
                var coord = new EntityCoordinates(grid.Owner, 0, 0);

                // Spawn the sink entity
                var sinkEnt = SSpawnAtPosition(proto.ID, coord);
                // Get the actual sink component, since the one we got from the prototype isn't initialized.
                var sinkComp = SComp<DeviceLinkSinkComponent>(sinkEnt);

                // Spawn the tester
                var sourceEnt = SSpawnAtPosition(PortTesterProtoId, coord);
                var sourceComp = SComp<DeviceLinkSourceComponent>(sourceEnt);

                // Create a link from the tester's output to the target port on the sink
                _sDeviceLinkSystem.SaveLinks(null,
                    sourceEnt,
                    sinkEnt,
                    [("Output", port.Id)],
                    sourceComp,
                    sinkComp);

                // Send a signal to the port
                Assert.DoesNotThrow(() => { _sDeviceLinkSystem.InvokePort(sourceEnt, "Output", null, sourceComp); },
                    $"Exception thrown while triggering port {port.Id} of the sink device.");

                _sMapSystem.DeleteMap(mapId);
            }
        }
    }
}
