#nullable enable
using System.Threading.Tasks;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Chemistry.Reagent;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Solutions
{
    [TestFixture]
    public sealed class ReagentDispenserTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestReagentDispenserInventory()
        {
            var server = StartServer();
            await server.WaitIdleAsync();
            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<ReagentDispenserInventoryPrototype>())
                {
                    foreach (var chem in proto.Inventory)
                    {
                        Assert.That(protoManager.HasIndex<ReagentPrototype>(chem), $"Unable to find chem {chem} in ReagentDispenserInventory {proto.ID}");
                    }
                }
            });
        }
    }
}
