#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class PowerTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: GeneratorDummy
  id: GeneratorDummy
  components:
  - type: NodeContainer
    nodes:
      output:
        !type:AdjacentNode
        nodeGroupID: HVPower
  - type: PowerSupplier
    supplyRate: 3000
  - type: Anchorable
  - type: SnapGrid
    offset: Center

- type: entity
  name: ConsumerDummy
  id: ConsumerDummy
  components:
  - type: SnapGrid
    offset: Center
  - type: NodeContainer
    nodes:
      input:
        !type:AdjacentNode
        nodeGroupID: HVPower
  - type: PowerConsumer
    drawRate: 50

- type: entity
  name: SubstationDummy
  id: SubstationDummy
  components:
  - type: Battery
    maxCharge: 1000
    startingCharge: 1000
  - type: NodeContainer
    nodes:
      input:
        !type:AdjacentNode
        nodeGroupID: HVPower
      output:
        !type:AdjacentNode
        nodeGroupID: MVPower
  - type: PowerConsumer
  - type: BatteryStorage
    activeDrawRate: 1500
  - type: PowerSupplier
    voltage: Medium
  - type: BatteryDischarger
    activeSupplyRate: 1000
  - type: SnapGrid
    offset: Center

- type: entity
  name: ApcDummy
  id: ApcDummy
  components:
  - type: Battery
    maxCharge: 10000
    startingCharge: 10000
  - type: BatteryStorage
    activeDrawRate: 1000
  - type: PowerProvider
    voltage: Apc
  - type: Apc
    voltage: Apc
  - type: PowerConsumer
    voltage: Medium
  - type: NodeContainer
    nodes:
      input:
        !type:AdjacentNode
        nodeGroupID: MVPower
      output:
        !type:AdjacentNode
        nodeGroupID: Apc
  - type: SnapGrid
    offset: Center
  - type: UserInterface
    interfaces:
    - key: enum.ApcUiKey.Key
      type: ApcBoundUserInterface
  - type: AccessReader
    access: [['Engineering']]

- type: entity
  name: ApcExtensionCableDummy
  id: ApcExtensionCableDummy
  components:
  - type: NodeContainer
    nodes:
      apc:
        !type:AdjacentNode
        nodeGroupID: Apc
      wire:
        !type:AdjacentNode
        nodeGroupID: WireNet
  - type: PowerProvider
    voltage: Apc
  - type: Wire
    wireType: Apc
  - type: SnapGrid
    offset: Center

- type: entity
  name: PowerReceiverDummy
  id: PowerReceiverDummy
  components:
  - type: PowerReceiver
  - type: SnapGrid
    offset: Center
";
        [Test]
        public async Task PowerNetTest()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            PowerSupplierComponent supplier = default!;
            PowerConsumerComponent consumer1 = default!;
            PowerConsumerComponent consumer2 = default!;

            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var entityMan = IoCManager.Resolve<IEntityManager>();
                mapMan.CreateMap(new MapId(1));
                var grid = mapMan.CreateGrid(new MapId(1));

                var generatorEnt = entityMan.SpawnEntity("GeneratorDummy", grid.ToCoordinates());
                var consumerEnt1 = entityMan.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 1));
                var consumerEnt2 = entityMan.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 2));

                if (generatorEnt.TryGetComponent(out PhysicsComponent? physics))
                {
                    physics.BodyType = BodyType.Static;
                }

                supplier = generatorEnt.GetComponent<PowerSupplierComponent>();
                consumer1 = consumerEnt1.GetComponent<PowerConsumerComponent>();
                consumer2 = consumerEnt2.GetComponent<PowerConsumerComponent>();

                var supplyRate = 1000; //arbitrary amount of power supply

                supplier.SupplyRate = supplyRate;
                consumer1.DrawRate = supplyRate / 2; //arbitrary draw less than supply
                consumer2.DrawRate = supplyRate * 2; //arbitrary draw greater than supply

                consumer1.Priority = Priority.First; //power goes to this consumer first
                consumer2.Priority = Priority.Last; //any excess power should go to low priority consumer
            });

            server.RunTicks(1); //let run a tick for PowerNet to process power

            server.Assert(() =>
            {
                Assert.That(consumer1.DrawRate, Is.EqualTo(consumer1.ReceivedPower)); //first should be fully powered
                Assert.That(consumer2.ReceivedPower, Is.EqualTo(supplier.SupplyRate - consumer1.ReceivedPower)); //second should get remaining power
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task ApcChargingTest()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            BatteryComponent apcBattery = default!;
            PowerSupplierComponent substationSupplier = default!;

            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var entityMan = IoCManager.Resolve<IEntityManager>();
                mapMan.CreateMap(new MapId(1));
                var grid = mapMan.CreateGrid(new MapId(1));

                var generatorEnt = entityMan.SpawnEntity("GeneratorDummy", grid.ToCoordinates());
                var substationEnt = entityMan.SpawnEntity("SubstationDummy", grid.ToCoordinates(0, 1));
                var apcEnt = entityMan.SpawnEntity("ApcDummy", grid.ToCoordinates(0, 2));

                var generatorSupplier = generatorEnt.GetComponent<PowerSupplierComponent>();

                substationSupplier = substationEnt.GetComponent<PowerSupplierComponent>();
                var substationStorage = substationEnt.GetComponent<BatteryStorageComponent>();
                var substationDischarger = substationEnt.GetComponent<BatteryDischargerComponent>();

                apcBattery = apcEnt.GetComponent<BatteryComponent>();
                var apcStorage = apcEnt.GetComponent<BatteryStorageComponent>();

                generatorSupplier.SupplyRate = 1000; //arbitrary nonzero amount of power
                substationStorage.ActiveDrawRate = 1000; //arbitrary nonzero power draw
                substationDischarger.ActiveSupplyRate = 500; //arbitirary nonzero power supply less than substation storage draw
                apcStorage.ActiveDrawRate = 500; //arbitrary nonzero power draw
                apcBattery.MaxCharge = 100; //abbitrary nonzero amount of charge
                apcBattery.CurrentCharge = 0; //no charge
            });

            server.RunTicks(5); //let run a few ticks for PowerNets to reevaluate and start charging apc

            server.Assert(() =>
            {
                Assert.That(substationSupplier.SupplyRate, Is.Not.EqualTo(0)); //substation should be providing power
                Assert.That(apcBattery.CurrentCharge, Is.Not.EqualTo(0)); //apc battery should have gained charge
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task ApcNetTest()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            PowerReceiverComponent receiver = default!;

            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var entityMan = IoCManager.Resolve<IEntityManager>();
                var mapId = new MapId(1);
                mapMan.CreateMap(mapId);
                var grid = mapMan.CreateGrid(mapId);

                var apcEnt = entityMan.SpawnEntity("ApcDummy", grid.ToCoordinates(0, 0));
                var apcExtensionEnt = entityMan.SpawnEntity("ApcExtensionCableDummy", grid.ToCoordinates(0, 1));
                var powerReceiverEnt = entityMan.SpawnEntity("PowerReceiverDummy", grid.ToCoordinates(0, 2));

                var apc = apcEnt.GetComponent<ApcComponent>();
                var provider = apcExtensionEnt.GetComponent<PowerProviderComponent>();
                receiver = powerReceiverEnt.GetComponent<PowerReceiverComponent>();
                var battery = apcEnt.GetComponent<BatteryComponent>();

                provider.PowerTransferRange = 5; //arbitrary range to reach receiver
                receiver.PowerReceptionRange = 5; //arbitrary range to reach provider

                battery.MaxCharge = 10000; //arbitrary nonzero amount of charge
                battery.CurrentCharge = battery.MaxCharge; //fill battery

                receiver.Load = 1; //arbitrary small amount of power
            });

            server.RunTicks(1); //let run a tick for ApcNet to process power

            server.Assert(() =>
            {
                Assert.That(receiver.Powered);
            });

            await server.WaitIdleAsync();
        }
    }
}
