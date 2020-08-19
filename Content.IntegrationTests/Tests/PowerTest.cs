using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System.Threading.Tasks;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class PowerTest : ContentIntegrationTest
    {
        [Test]
        public async Task PowerNetTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var entityMan = IoCManager.Resolve<IEntityManager>();
                mapMan.CreateMap(new MapId(1));
                var grid = mapMan.CreateGrid(new MapId(1));

                var generatorEnt = entityMan.SpawnEntity("DebugGenerator", new GridCoordinates(new Vector2(0, 0), grid.Index));
                var consumerEnt1 = entityMan.SpawnEntity("DebugConsumer", new GridCoordinates(new Vector2(0, 1), grid.Index));
                var consumerEnt2 = entityMan.SpawnEntity("DebugConsumer", new GridCoordinates(new Vector2(0, 2), grid.Index));

                Assert.That(generatorEnt.TryGetComponent<PowerSupplierComponent>(out var supplier));
                Assert.That(consumerEnt1.TryGetComponent<PowerConsumerComponent>(out var consumer1));
                Assert.That(consumerEnt2.TryGetComponent<PowerConsumerComponent>(out var consumer2));

                var supplyRate = 1000; //arbitrary amount of power supply

                supplier.SupplyRate = supplyRate;
                consumer1.DrawRate = supplyRate / 2; //arbitrary draw less than supply
                consumer1.Priority = Priority.First; //power goes to this consumer first
                consumer2.DrawRate = supplyRate * 2; //arbitrary draw greater than supply
                consumer2.Priority = Priority.Last; //excess power should go to this consumer of lower priority

                server.RunTicks(1);

                Assert.That(consumer1.DrawRate, Is.EqualTo(consumer1.ReceivedPower)); //first should be fully powered
                Assert.That(consumer2.ReceivedPower, Is.EqualTo(supplier.SupplyRate - consumer1.ReceivedPower)); //second should get remaining power
            });
        }

        [Test]
        public async Task ApcNetTest()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                var entityMan = IoCManager.Resolve<IEntityManager>();
                mapMan.CreateMap(new MapId(1));
                var grid = mapMan.CreateGrid(new MapId(1));

                var apcEnt = entityMan.SpawnEntity("DebugApc", new GridCoordinates(new Vector2(0, 0), grid.Index));
                var apcExtensionEnt = entityMan.SpawnEntity("ApcExtensionCable", new GridCoordinates(new Vector2(0, 1), grid.Index));
                var powerReceiverEnt = entityMan.SpawnEntity("DebugPowerReceiver", new GridCoordinates(new Vector2(0, 2), grid.Index));

                Assert.That(apcEnt.TryGetComponent<ApcComponent>(out var apc));
                Assert.That(apcExtensionEnt.TryGetComponent<PowerProviderComponent>(out var provider));
                Assert.That(powerReceiverEnt.TryGetComponent<PowerReceiverComponent>(out var receiver));

                apc.Battery.CurrentCharge = 10000; //arbitrary nonzero amount of charge
                receiver.Load = 1; //arbitrary small amount of power

                server.RunTicks(1);

                Assert.That(receiver.Powered);
            });
        }
    }
}
