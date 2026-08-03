using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Content.Shared.DeviceNetwork.Components;

namespace Content.IntegrationTests.Tests.DeviceNetwork
{
    [TestFixture]
    [TestOf(typeof(DeviceNetworkComponent))]
    [TestOf(typeof(WiredNetworkComponent))]
    [TestOf(typeof(WirelessNetworkComponent))]
    public sealed class DeviceNetworkTest : GameTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: DummyNetworkDevice
  id: DummyNetworkDevice
  components:
    - type: DeviceNetwork
      transmitFrequency: 100
      receiveFrequency: 100

- type: entity
  name: DummyWiredNetworkDevice
  id: DummyWiredNetworkDevice
  components:
    - type: DeviceNetwork
      deviceNetId: Wired
      transmitFrequency: 0
      receiveFrequency: 0
    - type: WiredNetworkConnection
    - type: ApcPowerReceiver

- type: entity
  name: WirelessNetworkDeviceDummy
  id: WirelessNetworkDeviceDummy
  components:
    - type: DeviceNetwork
      transmitFrequency: 100
      receiveFrequency: 100
      deviceNetId: Wireless
    - type: WirelessNetworkConnection
      range: 100
        ";

        [Test]
        public async Task NetworkDeviceSendAndReceive()
        {
            var pair = Pair;
            var server = pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var deviceNetSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkSystem>();
            var deviceNetTestSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkTestSystem>();

            EntityUid device1 = default;
            EntityUid device2 = default;
            DeviceNetworkComponent networkComponent1 = null;
            DeviceNetworkComponent networkComponent2 = null;

            await server.WaitAssertion(() =>
            {
                var payload = new TestPayload
                {
                    TestString = "test",
                    TestNumber = 1,
                    TestBool = true
                };

                device1 = entityManager.SpawnEntity("DummyNetworkDevice", MapCoordinates.Nullspace);

                Assert.That(entityManager.TryGetComponent(device1, out networkComponent1), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent1.Data.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent1.Data.Address, Is.Not.EqualTo(string.Empty));
                });

                device2 = entityManager.SpawnEntity("DummyNetworkDevice", MapCoordinates.Nullspace);

                Assert.That(entityManager.TryGetComponent(device2, out networkComponent2), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent1.Data.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent2.Data.Address, Is.Not.EqualTo(string.Empty));

                    Assert.That(networkComponent1.Data.Address, Is.Not.EqualTo(networkComponent2.Data.Address));
                });

                deviceNetSystem.SendPacket(device1, networkComponent2.Data.Address, ref payload, networkComponent2.Data.ReceiveFrequency.Value);
                Assert.That(payload, Is.EqualTo(deviceNetTestSystem.LastPayload));
            });
        }

        [Test]
        public async Task WirelessNetworkDeviceSendAndReceive()
        {
            var pair = Pair;
            var server = pair.Server;
            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var deviceNetSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkSystem>();
            var deviceNetTestSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkTestSystem>();

            EntityUid device1 = default;
            EntityUid device2 = default;
            DeviceNetworkComponent networkComponent1 = null;
            DeviceNetworkComponent networkComponent2 = null;
            WirelessNetworkComponent wirelessNetworkComponent = null;

            await server.WaitAssertion(() =>
            {
                device1 = entityManager.SpawnEntity("WirelessNetworkDeviceDummy", coordinates);

                Assert.Multiple(() =>
                {
                    Assert.That(entityManager.TryGetComponent(device1, out networkComponent1), Is.True);
                    Assert.That(entityManager.TryGetComponent(device1, out wirelessNetworkComponent), Is.True);
                });
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent1.Data.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent1.Data.Address, Is.Not.EqualTo(string.Empty));
                });

                device2 = entityManager.SpawnEntity("WirelessNetworkDeviceDummy", new MapCoordinates(new Vector2(0, 50), testMap.MapId));

                Assert.That(entityManager.TryGetComponent(device2, out networkComponent2), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent2.Data.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent2.Data.Address, Is.Not.EqualTo(string.Empty));

                    Assert.That(networkComponent1.Data.Address, Is.Not.EqualTo(networkComponent2.Data.Address));
                });

                var payload = new TestPayload
                {
                    TestString = "test",
                    TestNumber = 1,
                    TestBool = true
                };

                deviceNetSystem.SendPacket(device1, networkComponent2.Data.Address, ref payload, networkComponent2.Data.ReceiveFrequency.Value);

                Assert.That(payload, Is.EqualTo(deviceNetTestSystem.LastPayload));

                wirelessNetworkComponent.Range = 0;

                var secondPayload = new SecondTestPayload
                {
                    TestString = "test",
                    TestNumber = 1,
                    TestBool = true
                };

                deviceNetSystem.SendPacket(device1, networkComponent2.Data.Address, ref secondPayload, networkComponent2.Data.ReceiveFrequency.Value);
                Assert.That(secondPayload, Is.Not.EqualTo(deviceNetTestSystem.LastPayloadSecond));
            });
        }

        [Test]
        public async Task WiredNetworkDeviceSendAndReceive()
        {
            var pair = Pair;
            var server = pair.Server;
            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var deviceNetSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkSystem>();
            var deviceNetTestSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkTestSystem>();

            EntityUid device1 = default;
            EntityUid device2 = default;
            DeviceNetworkComponent networkComponent1 = null;
            DeviceNetworkComponent networkComponent2 = null;
            WiredNetworkComponent wiredNetworkComponent = null;

            await server.WaitRunTicks(2);
            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                device1 = entityManager.SpawnEntity("DummyWiredNetworkDevice", coordinates);

                Assert.Multiple(() =>
                {
                    Assert.That(entityManager.TryGetComponent(device1, out networkComponent1), Is.True);
                    Assert.That(entityManager.TryGetComponent(device1, out wiredNetworkComponent), Is.True);
                });
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent1.Data.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent1.Data.Address, Is.Not.EqualTo(string.Empty));
                });

                device2 = entityManager.SpawnEntity("DummyWiredNetworkDevice", coordinates);

                Assert.That(entityManager.TryGetComponent(device2, out networkComponent2), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent2.Data.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent2.Data.Address, Is.Not.EqualTo(string.Empty));

                    Assert.That(networkComponent1.Data.Address, Is.Not.EqualTo(networkComponent2.Data.Address));
                });

                var payload = new TestPayload
                {
                    TestString = "test",
                    TestNumber = 1,
                    TestBool = true
                };

                deviceNetSystem.SendPacket(device1, networkComponent2.Data.Address, ref payload, networkComponent2.Data.ReceiveFrequency.Value);

                entityManager.SpawnEntity("CableApcExtension", coordinates);

                deviceNetSystem.SendPacket(device1, networkComponent2.Data.Address, ref payload, networkComponent2.Data.ReceiveFrequency.Value);

                Assert.That(payload, Is.EqualTo(deviceNetTestSystem.LastPayload));
            });
        }
    }
}
