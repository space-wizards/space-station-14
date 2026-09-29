#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.DeviceNetwork;

[TestOf(typeof(DeviceNetworkComponent))]
[TestOf(typeof(WiredNetworkComponent))]
[TestOf(typeof(WirelessNetworkComponent))]
public sealed class DeviceNetworkTest : GameTest
{
    private const string DummyNetworkDevice = "DummyNetworkDevice";
    private const string DummyWiredNetworkDevice = "DummyWiredNetworkDevice";
    private const string WirelessNetworkDeviceDummy = "WirelessNetworkDeviceDummy";
    private static readonly EntProtoId CableApcExtension = "CableApcExtension";

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

    [SidedDependency(Side.Server)] private DeviceNetworkSystem _sDeviceNetworkSystem = default!;
    [SidedDependency(Side.Server)] private DeviceNetworkTestSystem _sDeviceNetworkTestSystem = default!;

    [Test]
    public async Task NetworkDeviceSendAndReceive()
    {
        EntityUid device1 = default;
        EntityUid device2 = default;
        DeviceNetworkComponent networkComponent1 = null!;
        DeviceNetworkComponent networkComponent2 = null!;

        await Server.WaitAssertion(() =>
        {
            var payload = new TestPayload
            {
                TestString = "test",
                TestNumber = 1,
                TestBool = true
            };

            device1 = SSpawn(DummyNetworkDevice);
            networkComponent1 = SComp<DeviceNetworkComponent>(device1);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent1.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));
            }

            device2 = SSpawn(DummyNetworkDevice);
            networkComponent2 = SComp<DeviceNetworkComponent>(device2);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent1.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));
            }

            _sDeviceNetworkSystem.SendPacket(device1, networkComponent2.Address, ref payload, networkComponent2.ReceiveFrequency!.Value);
            Assert.That(payload, Is.EqualTo(_sDeviceNetworkTestSystem.LastPayload));
        });
    }

    [Test]
    public async Task WirelessNetworkDeviceSendAndReceive()
    {
        await CreateTestMap();
        var coordinates = TestMap.GridCoords;

        EntityUid device1 = default;
        EntityUid device2 = default;
        DeviceNetworkComponent networkComponent1 = null!;
        DeviceNetworkComponent networkComponent2 = null!;
        WirelessNetworkComponent wirelessNetworkComponent = null!;

        await Server.WaitAssertion(() =>
        {
            device1 = SSpawnAtPosition(WirelessNetworkDeviceDummy, coordinates);
            networkComponent1 = SComp<DeviceNetworkComponent>(device1);
            wirelessNetworkComponent = SComp<WirelessNetworkComponent>(device1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent1.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));
            }

            device2 = SSpawnAtPosition(WirelessNetworkDeviceDummy, new EntityCoordinates(TestMap.Grid, new Vector2(0, 50)));

            networkComponent2 = SComp<DeviceNetworkComponent>(device2);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent2.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));
            }

            var payload = new TestPayload
            {
                TestString = "test",
                TestNumber = 1,
                TestBool = true
            };

            _sDeviceNetworkSystem.SendPacket(device1, networkComponent2.Address, ref payload, networkComponent2.ReceiveFrequency.Value);

            Assert.That(payload, Is.EqualTo(_sDeviceNetworkTestSystem.LastPayload));

            wirelessNetworkComponent.Range = 0;

            var secondPayload = new SecondTestPayload
            {
                TestString = "test",
                TestNumber = 1,
                TestBool = true
            };

            _sDeviceNetworkSystem.SendPacket(device1, networkComponent2.Address, ref secondPayload, networkComponent2.ReceiveFrequency.Value);
            Assert.That(secondPayload, Is.Not.EqualTo(_sDeviceNetworkTestSystem.LastPayloadSecond));
        });
    }

    [Test]
    public async Task WiredNetworkDeviceSendAndReceive()
    {
        await CreateTestMap();
        var coordinates = TestMap.GridCoords;

        EntityUid device1 = default;
        EntityUid device2 = default;
        DeviceNetworkComponent networkComponent1 = null!;
        DeviceNetworkComponent networkComponent2 = null!;
        WiredNetworkComponent wiredNetworkComponent = null!;

        await Server.WaitAssertion(() =>
        {
            device1 = SSpawnAtPosition(DummyWiredNetworkDevice, coordinates);
            networkComponent1 = SComp<DeviceNetworkComponent>(device1);
            wiredNetworkComponent = SComp<WiredNetworkComponent>(device1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent1.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));
            }

            device2 = SSpawnAtPosition(DummyWiredNetworkDevice, coordinates);
            networkComponent2 = SComp<DeviceNetworkComponent>(device2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(networkComponent2.ReceiveFrequency, Is.Not.Null);
                Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));
            }

            var payload = new TestPayload
            {
                TestString = "test",
                TestNumber = 1,
                TestBool = true
            };

            _sDeviceNetworkSystem.SendPacket(device1, networkComponent2.Address, ref payload, networkComponent2.ReceiveFrequency.Value);

            SSpawnAtPosition(CableApcExtension, coordinates);

            _sDeviceNetworkSystem.SendPacket(device1, networkComponent2.Address, ref payload, networkComponent2.ReceiveFrequency.Value);

            Assert.That(payload, Is.EqualTo(_sDeviceNetworkTestSystem.LastPayload));
        });
    }
}
