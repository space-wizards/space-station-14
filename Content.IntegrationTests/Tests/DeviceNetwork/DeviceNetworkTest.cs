using System.Numerics;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.IntegrationTests.Tests.DeviceNetwork
{
    [TestFixture]
    [TestOf(typeof(DeviceNetworkComponent))]
    [TestOf(typeof(WiredNetworkComponent))]
    [TestOf(typeof(WirelessNetworkComponent))]
    public sealed class DeviceNetworkTest
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
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var deviceNetSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkSystem>();
            var deviceNetTestSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkTestSystem>();


            EntityUid device1 = default;
            EntityUid device2 = default;
            DeviceNetworkComponent networkComponent1 = null;
            DeviceNetworkComponent networkComponent2 = null;

            var testValue = "test";
            var payload = new NetworkPayload
            {
                ["Test"] = testValue,
                ["testnumber"] = 1,
                ["testbool"] = true
            };

            await server.WaitAssertion(() =>
            {
                device1 = entityManager.SpawnEntity("DummyNetworkDevice", MapCoordinates.Nullspace);

                Assert.That(entityManager.TryGetComponent(device1, out networkComponent1), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent1.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));
                });

                device2 = entityManager.SpawnEntity("DummyNetworkDevice", MapCoordinates.Nullspace);

                Assert.That(entityManager.TryGetComponent(device2, out networkComponent2), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent1.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                    Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));
                });

                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, payload, networkComponent2.ReceiveFrequency.Value);
            });

            await server.WaitRunTicks(2);
            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                Assert.That(payload, Is.EquivalentTo(deviceNetTestSystem.LastPayload));
            });
            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task WirelessNetworkDeviceSendAndReceive()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var deviceNetSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkSystem>();
            var deviceNetTestSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkTestSystem>();

            EntityUid device1 = default;
            EntityUid device2 = default;
            DeviceNetworkComponent networkComponent1 = null;
            DeviceNetworkComponent networkComponent2 = null;
            WirelessNetworkComponent wirelessNetworkComponent = null;

            var testValue = "test";
            var payload = new NetworkPayload
            {
                ["Test"] = testValue,
                ["testnumber"] = 1,
                ["testbool"] = true
            };

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
                    Assert.That(networkComponent1.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));
                });

                device2 = entityManager.SpawnEntity("WirelessNetworkDeviceDummy", new MapCoordinates(new Vector2(0, 50), testMap.MapId));

                Assert.That(entityManager.TryGetComponent(device2, out networkComponent2), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent2.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                    Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));
                });


                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, payload, networkComponent2.ReceiveFrequency.Value);
            });

            await server.WaitRunTicks(2);
            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                Assert.That(payload, Is.EqualTo(deviceNetTestSystem.LastPayload).AsCollection);

                payload = new NetworkPayload
                {
                    ["Wirelesstest"] = 5
                };

                wirelessNetworkComponent.Range = 0;

                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, payload, networkComponent2.ReceiveFrequency.Value);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                Assert.That(payload, Is.Not.EqualTo(deviceNetTestSystem.LastPayload).AsCollection);
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task WiredNetworkDeviceSendAndReceive()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var deviceNetSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkSystem>();
            var deviceNetTestSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkTestSystem>();

            EntityUid device1 = default;
            EntityUid device2 = default;
            DeviceNetworkComponent networkComponent1 = null;
            DeviceNetworkComponent networkComponent2 = null;
            WiredNetworkComponent wiredNetworkComponent = null;
            var grid = testMap.Grid.Comp;

            var testValue = "test";
            var payload = new NetworkPayload
            {
                ["Test"] = testValue,
                ["testnumber"] = 1,
                ["testbool"] = true
            };

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
                    Assert.That(networkComponent1.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));
                });

                device2 = entityManager.SpawnEntity("DummyWiredNetworkDevice", coordinates);

                Assert.That(entityManager.TryGetComponent(device2, out networkComponent2), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(networkComponent2.ReceiveFrequency, Is.Not.Null);
                    Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                    Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));
                });

                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, payload, networkComponent2.ReceiveFrequency.Value);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                //CollectionAssert.AreNotEqual(deviceNetTestSystem.LastPayload, payload);

                entityManager.SpawnEntity("CableApcExtension", coordinates);

                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, payload, networkComponent2.ReceiveFrequency.Value);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                Assert.That(payload, Is.EqualTo(deviceNetTestSystem.LastPayload).AsCollection);
            });

            await pair.CleanReturnAsync();
        }
    }
}
