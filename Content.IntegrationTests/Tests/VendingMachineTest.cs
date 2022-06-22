using System.Threading.Tasks;
using Content.Shared.VendingMachines;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(VendingMachineInventoryPrototype))]
    public sealed class VendingMachineTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            await server.WaitAssertion(() =>
            {
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                foreach (var vendorProto in prototypeManager.EnumeratePrototypes<VendingMachineInventoryPrototype>())
                {
                    foreach (var (item, _) in vendorProto.StartingInventory)
                    {
                        try
                        {
                            prototypeManager.Index<EntityPrototype>(item);
                        }
                        catch (UnknownPrototypeException)
                        {
                            throw new UnknownPrototypeException($"Unknown prototype {item} on vending inventory {vendorProto.Name}");
                        }
                    }
                }
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
