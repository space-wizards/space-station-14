#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.DeviceNetwork;

[TestOf(typeof(DeviceNetworkComponent))]
[TestOf(typeof(WiredNetworkComponent))]
[TestOf(typeof(WirelessNetworkComponent))]
public sealed class DeviceNetworkTest : GameTest
{
    private static readonly EntProtoId CableApcExtension = "CableApcExtension";
    private const string DummyNetworkDevice = "DummyNetworkDevice";
    private const string DummyWiredNetworkDevice = "DummyWiredNetworkDevice";
    private const string WirelessNetworkDeviceDummy = "WirelessNetworkDeviceDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {DummyNetworkDevice}
  id: {DummyNetworkDevice}
  components:
    - type: DeviceNetwork
      transmitFrequency: 100
      receiveFrequency: 100

- type: entity
  name: {DummyWiredNetworkDevice}
  id: {DummyWiredNetworkDevice}
  components:
    - type: DeviceNetwork
      deviceNetId: Wired
      transmitFrequency: 0
      receiveFrequency: 0
    - type: WiredNetworkConnection
    - type: ApcPowerReceiver

- type: entity
  name: {WirelessNetworkDeviceDummy}
  id: {WirelessNetworkDeviceDummy}
  components:
    - type: DeviceNetwork
      transmitFrequency: 100
      receiveFrequency: 100
      deviceNetId: Wireless
    - type: WirelessNetworkConnection
      range: 100
        ";

    [SidedDependency(Side.Server)] private DeviceNetworkSystem _sDeviceNetSystem = null!;
    [SidedDependency(Side.Server)] private DeviceNetworkTestSystem _sDeviceNetTestSystem = null!;

    [Test]
    public async Task NetworkDeviceSendAndReceive()
    {
        EntityUid device1 = default;
        EntityUid device2 = default;
        DeviceNetworkComponent? networkComponent1 = null!;
        DeviceNetworkComponent? networkComponent2 = null!;

        var testValue = "test";
        var payload = new NetworkPayload
        {
            ["Test"] = testValue,
            ["testnumber"] = 1,
            ["testbool"] = true
        };

        await Server.WaitAssertion(() =>
        {
            device1 = SSpawn(DummyNetworkDevice);

            Assert.That(STryComp(device1, out networkComponent1), Is.True);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent1!.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));
            }

            device2 = SSpawn(DummyNetworkDevice);

            Assert.That(STryComp(device2, out networkComponent2), Is.True);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent1.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent2!.Address, Is.Not.EqualTo(string.Empty));

                Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));
            }

            _sDeviceNetSystem.QueuePacket(device1, networkComponent2.Address, payload, networkComponent2.ReceiveFrequency!.Value);
        });

        await RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(payload, Is.EquivalentTo(_sDeviceNetTestSystem.LastPayload));
        });
    }

    [Test]
    public async Task WirelessNetworkDeviceSendAndReceive()
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        EntityUid device1 = default;
        EntityUid device2 = default;
        DeviceNetworkComponent? networkComponent1 = null;
        DeviceNetworkComponent? networkComponent2 = null;
        WirelessNetworkComponent? wirelessNetworkComponent = null;

        var testValue = "test";
        var payload = new NetworkPayload
        {
            ["Test"] = testValue,
            ["testnumber"] = 1,
            ["testbool"] = true
        };

        await Server.WaitAssertion(() =>
        {
            device1 = SSpawnAtPosition(WirelessNetworkDeviceDummy, coordinates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(STryComp(device1, out networkComponent1), Is.True);
                Assert.That(STryComp(device1, out wirelessNetworkComponent), Is.True);
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent1!.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));
            }

            device2 = SSpawnAtPosition(WirelessNetworkDeviceDummy, new EntityCoordinates(TestMap.Grid, new Vector2(0, 50)));

            Assert.That(STryComp(device2, out networkComponent2), Is.True);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent2!.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));
            }


            _sDeviceNetSystem.QueuePacket(device1, networkComponent2.Address, payload, networkComponent2.ReceiveFrequency.Value);
        });

        await RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(payload, Is.EqualTo(_sDeviceNetTestSystem.LastPayload).AsCollection);

            payload = new NetworkPayload
            {
                ["Wirelesstest"] = 5
            };

            wirelessNetworkComponent!.Range = 0;

            _sDeviceNetSystem.QueuePacket(device1, networkComponent2!.Address, payload, networkComponent2.ReceiveFrequency!.Value);
        });

        await RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(payload, Is.Not.EqualTo(_sDeviceNetTestSystem.LastPayload).AsCollection);
        });
    }

    [Test]
    public async Task WiredNetworkDeviceSendAndReceive()
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        EntityUid device1 = default;
        EntityUid device2 = default;
        DeviceNetworkComponent? networkComponent1 = null;
        DeviceNetworkComponent? networkComponent2 = null;
        WiredNetworkComponent? wiredNetworkComponent = null;
        var grid = TestMap.Grid.Comp;

        var testValue = "test";
        var payload = new NetworkPayload
        {
            ["Test"] = testValue,
            ["testnumber"] = 1,
            ["testbool"] = true
        };

        await RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            device1 = SSpawnAtPosition(DummyWiredNetworkDevice, coordinates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(STryComp(device1, out networkComponent1), Is.True);
                Assert.That(STryComp(device1, out wiredNetworkComponent), Is.True);
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent1!.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));
            }

            device2 = SSpawnAtPosition(DummyWiredNetworkDevice, coordinates);

            Assert.That(STryComp(device2, out networkComponent2), Is.True);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent2!.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));
            }

            _sDeviceNetSystem.QueuePacket(device1, networkComponent2.Address, payload, networkComponent2.ReceiveFrequency.Value);
        });

        await RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(payload, Is.Not.EqualTo(_sDeviceNetTestSystem.LastPayload).AsCollection);

            SSpawnAtPosition(CableApcExtension, coordinates);

            _sDeviceNetSystem.QueuePacket(device1, networkComponent2!.Address, payload, networkComponent2.ReceiveFrequency!.Value);
        });

        await RunTicksSync(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(payload, Is.EqualTo(_sDeviceNetTestSystem.LastPayload).AsCollection);
        });
    }
}
