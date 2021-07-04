#nullable enable
using System.Threading.Tasks;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.Coordinates;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests
{
    [Parallelizable(ParallelScope.Fixtures)]
    [TestFixture]
    public class PowerTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  id: GeneratorDummy
  components:
  - type: NodeContainer
    nodes:
      output:
        !type:CableDeviceNode
        nodeGroupID: HVPower
  - type: PowerSupplier
  - type: Transform
    anchored: true

- type: entity
  id: ConsumerDummy
  components:
  - type: Transform
    anchored: true
  - type: NodeContainer
    nodes:
      input:
        !type:CableDeviceNode
        nodeGroupID: HVPower
  - type: PowerConsumer

- type: entity
  id: ChargingBatteryDummy
  components:
  - type: Transform
    anchored: true
  - type: NodeContainer
    nodes:
      output:
        !type:CableDeviceNode
        nodeGroupID: HVPower
  - type: PowerNetworkBattery
  - type: Battery
  - type: BatteryCharger

- type: entity
  id: DischargingBatteryDummy
  components:
  - type: Transform
    anchored: true
  - type: NodeContainer
    nodes:
      output:
        !type:CableDeviceNode
        nodeGroupID: HVPower
  - type: PowerNetworkBattery
  - type: Battery
  - type: BatteryDischarger

- type: entity
  id: FullBatteryDummy
  components:
  - type: Transform
    anchored: true
  - type: NodeContainer
    nodes:
      output:
        !type:CableDeviceNode
        nodeGroupID: HVPower
      input:
        !type:CableTerminalPortNode
        nodeGroupID: HVPower
  - type: PowerNetworkBattery
  - type: Battery
  - type: BatteryDischarger
    node: output
  - type: BatteryCharger
    node: input


#- type: entity
#  name: SubstationDummy
#  id: SubstationDummy
#  components:
#  - type: Battery
#    maxCharge: 1000
#    startingCharge: 1000
#  - type: NodeContainer
#    nodes:
#      input:
#        !type:CableNode
#        nodeGroupID: HVPower
#      output:
#        !type:CableNode
#        nodeGroupID: MVPower
#  - type: PowerConsumer
#  - type: BatteryStorage
#    activeDrawRate: 1500
#  - type: PowerSupplier
#    voltage: Medium
#  - type: BatteryDischarger
#    activeSupplyRate: 1000
#  - type: Transform
#    anchored: true
#
#- type: entity
#  name: ApcDummy
#  id: ApcDummy
#  components:
#  - type: Battery
#    maxCharge: 10000
#    startingCharge: 10000
#  - type: BatteryStorage
#    activeDrawRate: 1000
#  - type: PowerProvider
#    voltage: Apc
#  - type: Apc
#    voltage: Apc
#  - type: PowerConsumer
#    voltage: Medium
#  - type: NodeContainer
#    nodes:
#      input:
#        !type:CableNode
#        nodeGroupID: MVPower
#      output:
#        !type:CableNode
#        nodeGroupID: Apc
#  - type: Transform
#    anchored: true
#  - type: UserInterface
#    interfaces:
#    - key: enum.ApcUiKey.Key
#      type: ApcBoundUserInterface
#  - type: AccessReader
#    access: [['Engineering']]
#
#- type: entity
#  name: ApcExtensionCableDummy
#  id: ApcExtensionCableDummy
#  components:
#  - type: NodeContainer
#    nodes:
#      apc:
#        !type:CableNode
#        nodeGroupID: Apc
#      wire:
#        !type:AdjacentNode
#        nodeGroupID: WireNet
#  - type: PowerProvider
#    voltage: Apc
#  - type: Cable
#    cableType: Apc
#  - type: Transform
#    anchored: true
#
#- type: entity
#  name: PowerReceiverDummy
#  id: PowerReceiverDummy
#  components:
#  - type: PowerReceiver
#  - type: Transform
#    anchored: true
";

        private ServerIntegrationInstance _server = default!;
        private IMapManager _mapManager = default!;
        private IEntityManager _entityManager = default!;
        private IGameTiming _gameTiming = default!;

        [OneTimeSetUp]
        public async Task Setup()
        {
            var options = new ServerIntegrationOptions {ExtraPrototypes = Prototypes};
            _server = StartServerDummyTicker(options);

            await _server.WaitIdleAsync();
            _mapManager = _server.ResolveDependency<IMapManager>();
            _entityManager = _server.ResolveDependency<IEntityManager>();
            _gameTiming = _server.ResolveDependency<IGameTiming>();
        }

        /// <summary>
        ///     Test small power net with a simple surplus of power over the loads.
        /// </summary>
        [Test]
        public async Task TestSimpleSurplus()
        {
            const float loadPower = 200;
            PowerSupplierComponent supplier = default!;
            PowerConsumerComponent consumer1 = default!;
            PowerConsumerComponent consumer2 = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Power only works when anchored
                grid.SetTile(new Vector2i(0, 0), new Tile(1));
                grid.SetTile(new Vector2i(0, 1), new Tile(1));
                grid.SetTile(new Vector2i(0, 2), new Tile(1));

                for (var i = 0; i < 3; i++)
                {
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                var generatorEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates());
                var consumerEnt1 = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 1));
                var consumerEnt2 = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 2));

                supplier = generatorEnt.GetComponent<PowerSupplierComponent>();
                consumer1 = consumerEnt1.GetComponent<PowerConsumerComponent>();
                consumer2 = consumerEnt2.GetComponent<PowerConsumerComponent>();

                // Plenty of surplus and tolerance
                supplier.MaxSupply = loadPower * 4;
                supplier.SupplyRampTolerance = loadPower * 4;
                consumer1.DrawRate = loadPower;
                consumer2.DrawRate = loadPower;
            });

            _server.RunTicks(1); //let run a tick for PowerNet to process power

            _server.Assert(() =>
            {
                // Assert both consumers fully powered
                Assert.That(consumer1.ReceivedPower, Is.EqualTo(consumer1.DrawRate).Within(0.1));
                Assert.That(consumer2.ReceivedPower, Is.EqualTo(consumer2.DrawRate).Within(0.1));

                // Assert that load adds up on supply.
                Assert.That(supplier.CurrentSupply, Is.EqualTo(loadPower * 2).Within(0.1));
            });

            await _server.WaitIdleAsync();
        }


        /// <summary>
        ///     Test small power net with a simple deficit of power over the loads.
        /// </summary>
        [Test]
        public async Task TestSimpleDeficit()
        {
            const float loadPower = 200;
            PowerSupplierComponent supplier = default!;
            PowerConsumerComponent consumer1 = default!;
            PowerConsumerComponent consumer2 = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Power only works when anchored
                grid.SetTile(new Vector2i(0, 0), new Tile(1));
                grid.SetTile(new Vector2i(0, 1), new Tile(1));
                grid.SetTile(new Vector2i(0, 2), new Tile(1));

                for (var i = 0; i < 3; i++)
                {
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                var generatorEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates());
                var consumerEnt1 = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 1));
                var consumerEnt2 = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 2));

                supplier = generatorEnt.GetComponent<PowerSupplierComponent>();
                consumer1 = consumerEnt1.GetComponent<PowerConsumerComponent>();
                consumer2 = consumerEnt2.GetComponent<PowerConsumerComponent>();

                // Too little supply, both consumers should get 33% power.
                supplier.MaxSupply = loadPower;
                supplier.SupplyRampTolerance = loadPower;
                consumer1.DrawRate = loadPower;
                consumer2.DrawRate = loadPower * 2;
            });

            _server.RunTicks(1); //let run a tick for PowerNet to process power

            _server.Assert(() =>
            {
                // Assert both consumers get 33% power.
                Assert.That(consumer1.ReceivedPower, Is.EqualTo(consumer1.DrawRate / 3).Within(0.1));
                Assert.That(consumer2.ReceivedPower, Is.EqualTo(consumer2.DrawRate / 3).Within(0.1));

                // Supply should be maxed out
                Assert.That(supplier.CurrentSupply, Is.EqualTo(supplier.MaxSupply).Within(0.1));
            });

            await _server.WaitIdleAsync();
        }

        [Test]
        public async Task TestSupplyRamp()
        {
            PowerSupplierComponent supplier = default!;
            PowerConsumerComponent consumer = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Power only works when anchored
                grid.SetTile(new Vector2i(0, 0), new Tile(1));
                grid.SetTile(new Vector2i(0, 1), new Tile(1));
                grid.SetTile(new Vector2i(0, 2), new Tile(1));

                for (var i = 0; i < 3; i++)
                {
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                var generatorEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates());
                var consumerEnt = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 2));

                supplier = generatorEnt.GetComponent<PowerSupplierComponent>();
                consumer = consumerEnt.GetComponent<PowerConsumerComponent>();

                // Supply has enough total power but needs to ramp up to match.
                supplier.MaxSupply = 400;
                supplier.SupplyRampRate = 400;
                supplier.SupplyRampTolerance = 100;
                consumer.DrawRate = 400;
            });

            // Exact values can/will be off by a tick, add tolerance for that.
            var tickRate = (float) _gameTiming.TickPeriod.TotalSeconds;
            var tickDev = 400 * tickRate * 1.1f;

            _server.RunTicks(1);

            _server.Assert(() =>
            {
                // First tick, supply should be delivering 100 W (max tolerance) and start ramping up.
                Assert.That(supplier.CurrentSupply, Is.EqualTo(100).Within(0.1));
                Assert.That(consumer.ReceivedPower, Is.EqualTo(100).Within(0.1));
            });

            _server.RunTicks(14);

            _server.Assert(() =>
            {
                // After 15 ticks (0.25 seconds), supply ramp pos should be at 100 W and supply at 100, approx.
                Assert.That(supplier.CurrentSupply, Is.EqualTo(200).Within(tickDev));
                Assert.That(supplier.SupplyRampPosition, Is.EqualTo(100).Within(tickDev));
                Assert.That(consumer.ReceivedPower, Is.EqualTo(200).Within(tickDev));
            });

            _server.RunTicks(45);

            _server.Assert(() =>
            {
                // After 1 second total, ramp should be at 400 and supply should be at 400, everybody happy.
                Assert.That(supplier.CurrentSupply, Is.EqualTo(400).Within(tickDev));
                Assert.That(supplier.SupplyRampPosition, Is.EqualTo(400).Within(tickDev));
                Assert.That(consumer.ReceivedPower, Is.EqualTo(400).Within(tickDev));
            });

            await _server.WaitIdleAsync();
        }

        [Test]
        public async Task TestBatteryRamp()
        {
            const float startingCharge = 100_000;

            PowerNetworkBatteryComponent netBattery = default!;
            BatteryComponent battery = default!;
            PowerConsumerComponent consumer = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Power only works when anchored
                grid.SetTile(new Vector2i(0, 0), new Tile(1));
                grid.SetTile(new Vector2i(0, 1), new Tile(1));
                grid.SetTile(new Vector2i(0, 2), new Tile(1));

                for (var i = 0; i < 3; i++)
                {
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                var generatorEnt = _entityManager.SpawnEntity("DischargingBatteryDummy", grid.ToCoordinates());
                var consumerEnt = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 2));

                netBattery = generatorEnt.GetComponent<PowerNetworkBatteryComponent>();
                battery = generatorEnt.GetComponent<BatteryComponent>();
                consumer = consumerEnt.GetComponent<PowerConsumerComponent>();

                battery.MaxCharge = startingCharge;
                battery.CurrentCharge = startingCharge;
                netBattery.MaxSupply = 400;
                netBattery.SupplyRampRate = 400;
                netBattery.SupplyRampTolerance = 100;
                consumer.DrawRate = 400;
            });

            // Exact values can/will be off by a tick, add tolerance for that.
            var tickRate = (float) _gameTiming.TickPeriod.TotalSeconds;
            var tickDev = 400 * tickRate * 1.1f;

            _server.RunTicks(1);

            _server.Assert(() =>
            {
                // First tick, supply should be delivering 100 W (max tolerance) and start ramping up.
                Assert.That(netBattery.CurrentSupply, Is.EqualTo(100).Within(0.1));
                Assert.That(consumer.ReceivedPower, Is.EqualTo(100).Within(0.1));
            });

            _server.RunTicks(14);

            _server.Assert(() =>
            {
                // After 15 ticks (0.25 seconds), supply ramp pos should be at 100 W and supply at 100, approx.
                Assert.That(netBattery.CurrentSupply, Is.EqualTo(200).Within(tickDev));
                Assert.That(netBattery.SupplyRampPosition, Is.EqualTo(100).Within(tickDev));
                Assert.That(consumer.ReceivedPower, Is.EqualTo(200).Within(tickDev));

                // Trivial integral to calculate expected power spent.
                const double spentExpected = (200 + 100) / 2.0 * 0.25;
                Assert.That(battery.CurrentCharge, Is.EqualTo(startingCharge - spentExpected).Within(tickDev));
            });

            _server.RunTicks(45);

            _server.Assert(() =>
            {
                // After 1 second total, ramp should be at 400 and supply should be at 400, everybody happy.
                Assert.That(netBattery.CurrentSupply, Is.EqualTo(400).Within(tickDev));
                Assert.That(netBattery.SupplyRampPosition, Is.EqualTo(400).Within(tickDev));
                Assert.That(consumer.ReceivedPower, Is.EqualTo(400).Within(tickDev));

                // Trivial integral to calculate expected power spent.
                const double spentExpected = (400 + 100) / 2.0 * 0.75 + 400 * 0.25;
                Assert.That(battery.CurrentCharge, Is.EqualTo(startingCharge - spentExpected).Within(tickDev));
            });

            await _server.WaitIdleAsync();
        }


        [Test]
        public async Task TestSimpleBatteryChargeDeficit()
        {
            PowerSupplierComponent supplier = default!;
            BatteryComponent battery = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Power only works when anchored
                grid.SetTile(new Vector2i(0, 0), new Tile(1));
                grid.SetTile(new Vector2i(0, 1), new Tile(1));
                grid.SetTile(new Vector2i(0, 2), new Tile(1));

                for (var i = 0; i < 3; i++)
                {
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                var generatorEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates());
                var batteryEnt = _entityManager.SpawnEntity("ChargingBatteryDummy", grid.ToCoordinates(0, 2));

                supplier = generatorEnt.GetComponent<PowerSupplierComponent>();
                var netBattery = batteryEnt.GetComponent<PowerNetworkBatteryComponent>();
                battery = batteryEnt.GetComponent<BatteryComponent>();

                supplier.MaxSupply = 500;
                supplier.SupplyRampTolerance = 500;
                battery.MaxCharge = 100000;
                netBattery.MaxChargeRate = 1000;
                netBattery.Efficiency = 0.5f;
            });

            _server.RunTicks(30); // 60 TPS, 0.5 seconds

            _server.Assert(() =>
            {
                // half a second @ 500 W = 250
                // 50% efficiency, so 125 J stored total.
                Assert.That(battery.CurrentCharge, Is.EqualTo(125).Within(0.1));
                Assert.That(supplier.CurrentSupply, Is.EqualTo(500).Within(0.1));
            });

            await _server.WaitIdleAsync();
        }

        [Test]
        public async Task TestFullBattery()
        {
            PowerConsumerComponent consumer = default!;
            PowerSupplierComponent supplier = default!;
            PowerNetworkBatteryComponent netBattery = default!;
            BatteryComponent battery = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Power only works when anchored
                for (var i = 0; i < 4; i++)
                {
                    grid.SetTile(new Vector2i(0, i), new Tile(1));
                }

                for (var i = 0; i < 4; i++)
                {
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                var terminal = _entityManager.SpawnEntity("CableTerminal", grid.ToCoordinates(0, 1));
                terminal.Transform.LocalRotation = Angle.FromDegrees(180);

                var batteryEnt = _entityManager.SpawnEntity("FullBatteryDummy", grid.ToCoordinates(0, 2));
                var supplyEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates(0, 0));
                var consumerEnt = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 3));

                consumer = consumerEnt.GetComponent<PowerConsumerComponent>();
                supplier = supplyEnt.GetComponent<PowerSupplierComponent>();
                netBattery = batteryEnt.GetComponent<PowerNetworkBatteryComponent>();
                battery = batteryEnt.GetComponent<BatteryComponent>();

                // Consumer needs 1000 W, supplier can only provide 800, battery fills in the remaining 200.
                consumer.DrawRate = 1000;
                supplier.MaxSupply = 800;
                supplier.SupplyRampTolerance = 800;

                netBattery.MaxSupply = 400;
                netBattery.SupplyRampTolerance = 400;
                netBattery.SupplyRampRate = 100_000;
                battery.MaxCharge = 100_000;
                battery.CurrentCharge = 100_000;
            });

            // Run some ticks so everything is stable.
            _server.RunTicks(60);

            // Exact values can/will be off by a tick, add tolerance for that.
            var tickRate = (float) _gameTiming.TickPeriod.TotalSeconds;
            var tickDev = 400 * tickRate * 1.1f;

            _server.Assert(() =>
            {
                Assert.That(consumer.ReceivedPower, Is.EqualTo(consumer.DrawRate).Within(0.1));
                Assert.That(supplier.CurrentSupply, Is.EqualTo(supplier.MaxSupply).Within(0.1));

                // Battery's current supply includes passed-through power from the supply.
                // Assert ramp position is correct to make sure it's only supplying 200 W for real.
                Assert.That(netBattery.CurrentSupply, Is.EqualTo(1000).Within(0.1));
                Assert.That(netBattery.SupplyRampPosition, Is.EqualTo(200).Within(0.1));

                const int expectedSpent = 200;
                Assert.That(battery.CurrentCharge, Is.EqualTo(battery.MaxCharge - expectedSpent).Within(tickDev));
            });

            await _server.WaitIdleAsync();
        }

        [Test]
        public async Task TestFullBatteryEfficiencyPassThrough()
        {
            PowerConsumerComponent consumer = default!;
            PowerSupplierComponent supplier = default!;
            PowerNetworkBatteryComponent netBattery = default!;
            BatteryComponent battery = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Power only works when anchored
                for (var i = 0; i < 4; i++)
                {
                    grid.SetTile(new Vector2i(0, i), new Tile(1));
                }

                for (var i = 0; i < 4; i++)
                {
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                var terminal = _entityManager.SpawnEntity("CableTerminal", grid.ToCoordinates(0, 1));
                terminal.Transform.LocalRotation = Angle.FromDegrees(180);

                var batteryEnt = _entityManager.SpawnEntity("FullBatteryDummy", grid.ToCoordinates(0, 2));
                var supplyEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates(0, 0));
                var consumerEnt = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 3));

                consumer = consumerEnt.GetComponent<PowerConsumerComponent>();
                supplier = supplyEnt.GetComponent<PowerSupplierComponent>();
                netBattery = batteryEnt.GetComponent<PowerNetworkBatteryComponent>();
                battery = batteryEnt.GetComponent<BatteryComponent>();

                // Consumer needs 1000 W, supply and battery can only provide 400 each.
                // BUT the battery has 50% input efficiency, so 50% of the power of the supply gets lost.
                consumer.DrawRate = 1000;
                supplier.MaxSupply = 400;
                supplier.SupplyRampTolerance = 400;

                netBattery.MaxSupply = 400;
                netBattery.SupplyRampTolerance = 400;
                netBattery.SupplyRampRate = 100_000;
                netBattery.Efficiency = 0.5f;
                battery.MaxCharge = 1_000_000;
                battery.CurrentCharge = 1_000_000;
            });

            // Run some ticks so everything is stable.
            _server.RunTicks(60);

            // Exact values can/will be off by a tick, add tolerance for that.
            var tickRate = (float) _gameTiming.TickPeriod.TotalSeconds;
            var tickDev = 400 * tickRate * 1.1f;

            _server.Assert(() =>
            {
                Assert.That(consumer.ReceivedPower, Is.EqualTo(600).Within(0.1));
                Assert.That(supplier.CurrentSupply, Is.EqualTo(supplier.MaxSupply).Within(0.1));

                Assert.That(netBattery.CurrentSupply, Is.EqualTo(600).Within(0.1));
                Assert.That(netBattery.SupplyRampPosition, Is.EqualTo(400).Within(0.1));

                const int expectedSpent = 400;
                Assert.That(battery.CurrentCharge, Is.EqualTo(battery.MaxCharge - expectedSpent).Within(tickDev));
            });

            await _server.WaitIdleAsync();
        }

        [Test]
        public async Task TestFullBatteryEfficiencyDemandPassThrough()
        {
            PowerConsumerComponent consumer1 = default!;
            PowerConsumerComponent consumer2 = default!;
            PowerSupplierComponent supplier = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Map layout here is
                // C - consumer
                // B - battery
                // G - generator
                // B - battery
                // C - consumer
                // Connected in the only way that makes sense.

                // Power only works when anchored
                for (var i = 0; i < 5; i++)
                {
                    grid.SetTile(new Vector2i(0, i), new Tile(1));
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                _entityManager.SpawnEntity("CableTerminal", grid.ToCoordinates(0, 2));
                var terminal = _entityManager.SpawnEntity("CableTerminal", grid.ToCoordinates(0, 2));
                terminal.Transform.LocalRotation = Angle.FromDegrees(180);

                var batteryEnt1 = _entityManager.SpawnEntity("FullBatteryDummy", grid.ToCoordinates(0, 1));
                var batteryEnt2 = _entityManager.SpawnEntity("FullBatteryDummy", grid.ToCoordinates(0, 3));
                var supplyEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates(0, 2));
                var consumerEnt1 = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 0));
                var consumerEnt2 = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 4));

                consumer1 = consumerEnt1.GetComponent<PowerConsumerComponent>();
                consumer2 = consumerEnt2.GetComponent<PowerConsumerComponent>();
                supplier = supplyEnt.GetComponent<PowerSupplierComponent>();
                var netBattery1 = batteryEnt1.GetComponent<PowerNetworkBatteryComponent>();
                var netBattery2 = batteryEnt2.GetComponent<PowerNetworkBatteryComponent>();
                var battery1 = batteryEnt1.GetComponent<BatteryComponent>();
                var battery2 = batteryEnt2.GetComponent<BatteryComponent>();

                // There are two loads, 500 W and 1000 W respectively.
                // The 500 W load is behind a 50% efficient battery,
                // so *effectively* it needs 2x as much power from the supply to run.
                // Assert that both are getting 50% power.
                // Batteries are empty and only a bridge.

                consumer1.DrawRate = 500;
                consumer2.DrawRate = 1000;
                supplier.MaxSupply = 1000;
                supplier.SupplyRampTolerance = 1000;

                battery1.MaxCharge = 1_000_000;
                battery2.MaxCharge = 1_000_000;

                netBattery1.MaxChargeRate = 1_000;
                netBattery2.MaxChargeRate = 1_000;

                netBattery1.Efficiency = 0.5f;

                netBattery1.MaxSupply = 1_000_000;
                netBattery2.MaxSupply = 1_000_000;

                netBattery1.SupplyRampTolerance = 1_000_000;
                netBattery2.SupplyRampTolerance = 1_000_000;
            });

            // Run some ticks so everything is stable.
            _server.RunTicks(10);

            _server.Assert(() =>
            {
                Assert.That(consumer1.ReceivedPower, Is.EqualTo(250).Within(0.1));
                Assert.That(consumer2.ReceivedPower, Is.EqualTo(500).Within(0.1));
                Assert.That(supplier.CurrentSupply, Is.EqualTo(supplier.MaxSupply).Within(0.1));
            });

            await _server.WaitIdleAsync();
        }

        /// <summary>
        ///     Test that power is distributed proportionally, even through batteries.
        /// </summary>
        [Test]
        public async Task TestBatteriesProportional()
        {
            PowerConsumerComponent consumer1 = default!;
            PowerConsumerComponent consumer2 = default!;
            PowerSupplierComponent supplier = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Map layout here is
                // C - consumer
                // B - battery
                // G - generator
                // B - battery
                // C - consumer
                // Connected in the only way that makes sense.

                // Power only works when anchored
                for (var i = 0; i < 5; i++)
                {
                    grid.SetTile(new Vector2i(0, i), new Tile(1));
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                _entityManager.SpawnEntity("CableTerminal", grid.ToCoordinates(0, 2));
                var terminal = _entityManager.SpawnEntity("CableTerminal", grid.ToCoordinates(0, 2));
                terminal.Transform.LocalRotation = Angle.FromDegrees(180);

                var batteryEnt1 = _entityManager.SpawnEntity("FullBatteryDummy", grid.ToCoordinates(0, 1));
                var batteryEnt2 = _entityManager.SpawnEntity("FullBatteryDummy", grid.ToCoordinates(0, 3));
                var supplyEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates(0, 2));
                var consumerEnt1 = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 0));
                var consumerEnt2 = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 4));

                consumer1 = consumerEnt1.GetComponent<PowerConsumerComponent>();
                consumer2 = consumerEnt2.GetComponent<PowerConsumerComponent>();
                supplier = supplyEnt.GetComponent<PowerSupplierComponent>();
                var netBattery1 = batteryEnt1.GetComponent<PowerNetworkBatteryComponent>();
                var netBattery2 = batteryEnt2.GetComponent<PowerNetworkBatteryComponent>();
                var battery1 = batteryEnt1.GetComponent<BatteryComponent>();
                var battery2 = batteryEnt2.GetComponent<BatteryComponent>();

                consumer1.DrawRate = 500;
                consumer2.DrawRate = 1000;
                supplier.MaxSupply = 1000;
                supplier.SupplyRampTolerance = 1000;

                battery1.MaxCharge = 1_000_000;
                battery2.MaxCharge = 1_000_000;

                netBattery1.MaxChargeRate = 20;
                netBattery2.MaxChargeRate = 20;

                netBattery1.MaxSupply = 1_000_000;
                netBattery2.MaxSupply = 1_000_000;

                netBattery1.SupplyRampTolerance = 1_000_000;
                netBattery2.SupplyRampTolerance = 1_000_000;
            });

            // Run some ticks so everything is stable.
            _server.RunTicks(60);

            _server.Assert(() =>
            {
                // NOTE: MaxChargeRate on batteries actually skews the demand.
                // So that's why the tolerance is so high, the charge rate is so *low*,
                // and we run so many ticks to stabilize.
                Assert.That(consumer1.ReceivedPower, Is.EqualTo(333.333).Within(10));
                Assert.That(consumer2.ReceivedPower, Is.EqualTo(666.666).Within(10));
                Assert.That(supplier.CurrentSupply, Is.EqualTo(supplier.MaxSupply).Within(0.1));
            });

            await _server.WaitIdleAsync();
        }

        [Test]
        public async Task TestBatteryEngineCut()
        {
            PowerConsumerComponent consumer = default!;
            PowerSupplierComponent supplier = default!;
            PowerNetworkBatteryComponent netBattery = default!;
            BatteryComponent battery = default!;

            _server.Post(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Power only works when anchored
                for (var i = 0; i < 4; i++)
                {
                    grid.SetTile(new Vector2i(0, i), new Tile(1));
                }

                for (var i = 0; i < 4; i++)
                {
                    _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, i));
                }

                var terminal = _entityManager.SpawnEntity("CableTerminal", grid.ToCoordinates(0, 1));
                terminal.Transform.LocalRotation = Angle.FromDegrees(180);

                var batteryEnt = _entityManager.SpawnEntity("FullBatteryDummy", grid.ToCoordinates(0, 2));
                var supplyEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates(0, 0));
                var consumerEnt = _entityManager.SpawnEntity("ConsumerDummy", grid.ToCoordinates(0, 3));

                consumer = consumerEnt.GetComponent<PowerConsumerComponent>();
                supplier = supplyEnt.GetComponent<PowerSupplierComponent>();
                netBattery = batteryEnt.GetComponent<PowerNetworkBatteryComponent>();
                battery = batteryEnt.GetComponent<BatteryComponent>();

                // Consumer needs 1000 W, supplier can only provide 800, battery fills in the remaining 200.
                consumer.DrawRate = 1000;
                supplier.MaxSupply = 1000;
                supplier.SupplyRampTolerance = 1000;

                netBattery.MaxSupply = 1000;
                netBattery.SupplyRampTolerance = 200;
                netBattery.SupplyRampRate = 10;
                battery.MaxCharge = 100_000;
                battery.CurrentCharge = 100_000;
            });

            // Run some ticks so everything is stable.
            _server.RunTicks(5);

            _server.Assert(() =>
            {
                // Supply and consumer are fully loaded/supplied.
                Assert.That(consumer.ReceivedPower, Is.EqualTo(consumer.DrawRate).Within(0.5));
                Assert.That(supplier.CurrentSupply, Is.EqualTo(supplier.MaxSupply).Within(0.5));

                // Cut off the supplier
                supplier.Enabled = false;
                // Remove tolerance on battery too.
                netBattery.SupplyRampTolerance = 5;
            });

            _server.RunTicks(3);

            _server.Assert(() =>
            {
                // Assert that network drops to 0 power and starts ramping up
                Assert.That(consumer.ReceivedPower, Is.LessThan(50).And.GreaterThan(0));
                Assert.That(netBattery.CurrentReceiving, Is.EqualTo(0));
                Assert.That(netBattery.CurrentSupply, Is.GreaterThan(0));
            });

            await _server.WaitIdleAsync();
        }

        /// <summary>
        ///     Test that <see cref="CableTerminalNode"/> correctly isolates two networks.
        /// </summary>
        [Test]
        public async Task TestTerminalNodeGroups()
        {
            CableNode leftNode = default!;
            CableNode rightNode = default!;
            Node batteryInput = default!;
            Node batteryOutput = default!;

            _server.Assert(() =>
            {
                var map = _mapManager.CreateMap();
                var grid = _mapManager.CreateGrid(map);

                // Power only works when anchored
                for (var i = 0; i < 4; i++)
                {
                    grid.SetTile(new Vector2i(0, i), new Tile(1));
                }

                var leftEnt = _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, 0));
                _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, 1));
                _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, 2));
                var rightEnt = _entityManager.SpawnEntity("HVCable", grid.ToCoordinates(0, 3));

                var terminal = _entityManager.SpawnEntity("CableTerminal", grid.ToCoordinates(0, 1));
                terminal.Transform.LocalRotation = Angle.FromDegrees(180);

                var battery = _entityManager.SpawnEntity("FullBatteryDummy", grid.ToCoordinates(0, 2));
                var batteryNodeContainer = battery.GetComponent<NodeContainerComponent>();

                leftNode = leftEnt.GetComponent<NodeContainerComponent>().GetNode<CableNode>("power");
                rightNode = rightEnt.GetComponent<NodeContainerComponent>().GetNode<CableNode>("power");

                batteryInput = batteryNodeContainer.GetNode<Node>("input");
                batteryOutput = batteryNodeContainer.GetNode<Node>("output");
            });

            // Run ticks to allow node groups to update.
            _server.RunTicks(1);

            _server.Assert(() =>
            {
                Assert.That(batteryInput.NodeGroup, Is.EqualTo(leftNode.NodeGroup));
                Assert.That(batteryOutput.NodeGroup, Is.EqualTo(rightNode.NodeGroup));

                Assert.That(leftNode.NodeGroup, Is.Not.EqualTo(rightNode.NodeGroup));
            });

            await _server.WaitIdleAsync();
        }

        /*[Test]
        public async Task ApcChargingTest()
        {
            var options = new ServerIntegrationOptions {ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            BatteryComponent apcBattery = default!;
            PowerSupplierComponent substationSupplier = default!;

            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var _entityManager = IoCManager.Resolve<I_entityManagerager>();
                mapMan.CreateMap(new MapId(1));
                var grid = mapMan.CreateGrid(new MapId(1));

                // Power only works when anchored
                grid.SetTile(new Vector2i(0, 0), new Tile(1));
                grid.SetTile(new Vector2i(0, 1), new Tile(1));
                grid.SetTile(new Vector2i(0, 2), new Tile(1));

                var generatorEnt = _entityManager.SpawnEntity("GeneratorDummy", grid.ToCoordinates());
                var substationEnt = _entityManager.SpawnEntity("SubstationDummy", grid.ToCoordinates(0, 1));
                var apcEnt = _entityManager.SpawnEntity("ApcDummy", grid.ToCoordinates(0, 2));

                var generatorSupplier = generatorEnt.GetComponent<PowerSupplierComponent>();

                substationSupplier = substationEnt.GetComponent<PowerSupplierComponent>();
                var substationStorage = substationEnt.GetComponent<BatteryStorageComponent>();
                var substationDischarger = substationEnt.GetComponent<BatteryDischargerComponent>();

                apcBattery = apcEnt.GetComponent<BatteryComponent>();
                var apcStorage = apcEnt.GetComponent<BatteryStorageComponent>();

                generatorSupplier.SupplyRate = 1000; //arbitrary nonzero amount of power
                substationStorage.ActiveDrawRate = 1000; //arbitrary nonzero power draw
                substationDischarger.ActiveSupplyRate =
                    500; //arbitirary nonzero power supply less than substation storage draw
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
            var options = new ServerIntegrationOptions {ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            ApcPowerReceiverComponent receiver = default!;

            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var _entityManager = IoCManager.Resolve<I_entityManagerager>();
                var mapId = new MapId(1);
                mapMan.CreateMap(mapId);
                var grid = mapMan.CreateGrid(mapId);

                // Power only works when anchored
                grid.SetTile(new Vector2i(0, 0), new Tile(1));
                grid.SetTile(new Vector2i(0, 1), new Tile(1));
                grid.SetTile(new Vector2i(0, 2), new Tile(1));

                var apcEnt = _entityManager.SpawnEntity("ApcDummy", grid.ToCoordinates(0, 0));
                var apcExtensionEnt = _entityManager.SpawnEntity("ApcExtensionCableDummy", grid.ToCoordinates(0, 1));
                var powerReceiverEnt = _entityManager.SpawnEntity("PowerReceiverDummy", grid.ToCoordinates(0, 2));

                var apc = apcEnt.GetComponent<ApcComponent>();
                var provider = apcExtensionEnt.GetComponent<ApcPowerProviderComponent>();
                receiver = powerReceiverEnt.GetComponent<ApcPowerReceiverComponent>();
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
        }*/
    }
}
