using System.Threading.Tasks;
using Content.Server.Cargo.Components;
using Content.Shared.Cargo;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class CargoTests : ContentIntegrationTest
{
    [Test]
    public async Task TestGalacticMarketProducts()
    {
        var server = StartServerDummyTicker();
        await server.WaitIdleAsync();

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            foreach (var ent in protoManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (!ent.Components.TryGetValue("GalacticMarket", out var comp)) continue;

                var market = (GalacticMarketComponent) comp;

                foreach (var product in market.Products)
                {
                    // This top assert is probably caught by the TryIndex but juusssttt in case it ever gets reverted.
                    Assert.That(protoManager.HasIndex<CargoProductPrototype>(product.ID), $"Unable to find cargo product for {product.ID} on prototype {ent}");
                    Assert.That(protoManager.HasIndex<EntityPrototype>(product.Product), $"Unable to find cargo entity for {product.Product} on prototype {ent}");
                }
            }
        });
    }
}
