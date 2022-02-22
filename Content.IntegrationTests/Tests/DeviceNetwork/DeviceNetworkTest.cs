using System.Threading.Tasks;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.DeviceNetwork
{
    [TestFixture]
    [TestOf(typeof(DeviceNetworkComponent))]
    [TestOf(typeof(WiredNetworkComponent))]
    [TestOf(typeof(WirelessNetworkComponent))]
    public sealed class DeviceNetworkTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: DummyNetworkDevice
  id: DummyNetworkDevice
  components:
    - type: DeviceNetworkComponent
      frequency: 100

- type: entity
  name: DummyWiredNetworkDevice
  id: DummyWiredNetworkDevice
  components:
    - type: DeviceNetworkComponent
      deviceNetId: Wired
    - type: WiredNetworkConnection
    - type: ApcPowerReceiver

- type: entity
  name: DummyWirelessNetworkDevice
  id: DummyWirelessNetworkDevice
  components:
    - type: DeviceNetworkComponent
      frequency: 100
      deviceNetId: Wireless
    - type: WirelessNetworkConnection
      range: 100
        ";

        [Test]
        public async Task NetworkDeviceSendAndReceive()
        {
            var options = new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes,
                ContentBeforeIoC = () => {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<DeviceNetworkTestSystem>();
                }
            };

            var server = StartServer(options);

            await server.WaitIdleAsync();

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

            server.Assert(() => {
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                device1 = entityManager.SpawnEntity("DummyNetworkDevice", MapCoordinates.Nullspace);

                Assert.That(entityManager.TryGetComponent(device1, out networkComponent1), Is.True);
                Assert.That(networkComponent1.Open, Is.True);
                Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));

                device2 = entityManager.SpawnEntity("DummyNetworkDevice", MapCoordinates.Nullspace);

                Assert.That(entityManager.TryGetComponent(device2, out networkComponent2), Is.True);
                Assert.That(networkComponent2.Open, Is.True);
                Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));

                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, networkComponent2.Frequency, payload);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            server.Assert(() => {
                CollectionAssert.AreEquivalent(deviceNetTestSystem.LastPayload, payload);
            });
        }

        [Test]
        public async Task WirelessNetworkDeviceSendAndReceive()
        {
            var options = new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes,
                ContentBeforeIoC = () => {
                    IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<DeviceNetworkTestSystem>();
                }
            };

            var server = StartServer(options);

            await server.WaitIdleAsync();

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

            server.Assert(() => {
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                device1 = entityManager.SpawnEntity("DummyWirelessNetworkDevice", MapCoordinates.Nullspace);

                Assert.That(entityManager.TryGetComponent(device1, out networkComponent1), Is.True);
                Assert.That(entityManager.TryGetComponent(device1, out wirelessNetworkComponent), Is.True);
                Assert.That(networkComponent1.Open, Is.True);
                Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));

                device2 = entityManager.SpawnEntity("DummyWirelessNetworkDevice", new MapCoordinates(new Robust.Shared.Maths.Vector2(0,50), MapId.Nullspace));

                Assert.That(entityManager.TryGetComponent(device2, out networkComponent2), Is.True);
                Assert.That(networkComponent2.Open, Is.True);
                Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));

                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, networkComponent2.Frequency, payload);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            server.Assert(() => {
                CollectionAssert.AreEqual(deviceNetTestSystem.LastPayload, payload);

                payload = new NetworkPayload
                {
                    ["Wirelesstest"] = 5
                };

                wirelessNetworkComponent.Range = 0;

                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, networkComponent2.Frequency, payload);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            server.Assert(() => {
                CollectionAssert.AreNotEqual(deviceNetTestSystem.LastPayload, payload);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task WiredNetworkDeviceSendAndReceive()
        {
            var options = new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes,
                ContentBeforeIoC = () => {
                   IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<DeviceNetworkTestSystem>();
                }
            };

            var server = StartServer(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var deviceNetSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkSystem>();
            var deviceNetTestSystem = entityManager.EntitySysManager.GetEntitySystem<DeviceNetworkTestSystem>();


            EntityUid device1 = default;
            EntityUid device2 = default;
            DeviceNetworkComponent networkComponent1 = null;
            DeviceNetworkComponent networkComponent2 = null;
            WiredNetworkComponent wiredNetworkComponent = null;
            IMapGrid grid = null;

            var testValue = "test";
            var payload = new NetworkPayload
            {
                ["Test"] = testValue,
                ["testnumber"] = 1,
                ["testbool"] = true
            };

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            server.Assert(() => {
                var map = mapManager.CreateNewMapEntity(MapId.Nullspace);
                grid = mapManager.CreateGrid(MapId.Nullspace);

                device1 = entityManager.SpawnEntity("DummyWiredNetworkDevice", MapCoordinates.Nullspace);

                Assert.That(entityManager.TryGetComponent(device1, out networkComponent1), Is.True);
                Assert.That(entityManager.TryGetComponent(device1, out wiredNetworkComponent), Is.True);
                Assert.That(networkComponent1.Open, Is.True);
                Assert.That(networkComponent1.Address, Is.Not.EqualTo(string.Empty));

                device2 = entityManager.SpawnEntity("DummyWiredNetworkDevice", new MapCoordinates(new Robust.Shared.Maths.Vector2(0, 2), MapId.Nullspace));

                Assert.That(entityManager.TryGetComponent(device2, out networkComponent2), Is.True);
                Assert.That(networkComponent2.Open, Is.True);
                Assert.That(networkComponent2.Address, Is.Not.EqualTo(string.Empty));

                Assert.That(networkComponent1.Address, Is.Not.EqualTo(networkComponent2.Address));

                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, networkComponent2.Frequency, payload);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            server.Assert(() => {
                //CollectionAssert.AreNotEqual(deviceNetTestSystem.LastPayload, payload);

                entityManager.SpawnEntity("CableApcExtension", grid.MapToGrid(new MapCoordinates(new Robust.Shared.Maths.Vector2(0, 1), MapId.Nullspace)));

                deviceNetSystem.QueuePacket(device1, networkComponent2.Address, networkComponent2.Frequency, payload);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            server.Assert(() => {
                CollectionAssert.AreEqual(deviceNetTestSystem.LastPayload, payload);
            });

            await server.WaitIdleAsync();
        }
    }
}
