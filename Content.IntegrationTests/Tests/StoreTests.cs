using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class StoreTests
{

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  name: InventoryPdaDummy
  id: InventoryPdaDummy
  parent: BasePDA
  components:
  - type: Clothing
    QuickEquip: false
    slots:
    - idcard
  - type: Pda
";
    [Test]
    public async Task StoreDiscountAndRefund()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        var serverRandom = server.ResolveDependency<IRobustRandom>();
        serverRandom.SetSeed(534);

        var entManager = server.ResolveDependency<IEntityManager>();

        var mapSystem = server.System<SharedMapSystem>();
        var prototypeManager = server.ProtoMan;

        Assert.That(mapSystem.IsInitialized(testMap.MapId));


        EntityUid human = default;
        EntityUid uniform = default;
        EntityUid pda = default;

        var uplinkSystem = entManager.System<UplinkSystem>();

        var listingPrototypes = prototypeManager.EnumeratePrototypes<ListingPrototype>()
                                                .ToArray();

        var coordinates = testMap.GridCoords;
        await server.WaitAssertion(() =>
        {
            var invSystem = entManager.System<InventorySystem>();

            human = entManager.SpawnEntity("HumanUniformDummy", coordinates);
            uniform = entManager.SpawnEntity("UniformDummy", coordinates);
            pda = entManager.SpawnEntity("InventoryPdaDummy", coordinates);

            Assert.That(invSystem.TryEquip(human, uniform, "jumpsuit"));
            Assert.That(invSystem.TryEquip(human, pda, "id"));

            FixedPoint2 originalBalance = 20;
            uplinkSystem.AddUplink(human, originalBalance, null, true);

            var storeComponent = entManager.GetComponent<StoreComponent>(pda);
            var discountComponent = entManager.GetComponent<StoreDiscountComponent>(pda);
            Assert.That(
                discountComponent.Discounts,
                Has.Exactly(3).Items,
                $"After applying discount total discounted items count was expected to be '3' "
                + $"but was actually {discountComponent.Discounts.Count}- this can be due to discount "
                + $"categories settings (maxItems, weight) not being realistically set, or default "
                + $"discounted count being changed from '3' in StoreDiscountSystem.InitializeDiscounts."
            );
            var discountedListingItems = storeComponent.FullListingsCatalog
                                                       .Where(x => x.IsCostModified)
                                                       .OrderBy(x => x.ID)
                                                       .ToArray();
            Assert.That(discountComponent.Discounts
                                         .Select(x => x.ListingId.Id),
                Is.EquivalentTo(discountedListingItems.Select(x => x.ID)),
                $"{nameof(StoreComponent)}.{nameof(StoreComponent.FullListingsCatalog)} does not contain all "
                + $"items that are marked as discounted, or they don't have flag '{nameof(ListingDataWithCostModifiers.IsCostModified)}'"
                + $"flag as 'true'. This marks the fact that cost modifier of discount is not applied properly!"
            );

            // Refund action requests re-generation of listing items so we will be re-acquiring items from component a lot of times.
            var itemIds = discountedListingItems.Select(x => x.ID);
            foreach (var itemId in itemIds)
            {
                Assert.Multiple(() =>
                {
                    storeComponent.RefundAllowed = true;

                    var discountedListingItem = storeComponent.FullListingsCatalog.First(x => x.ID == itemId);
                    var plainDiscountedCost = discountedListingItem.Cost[UplinkSystem.TelecrystalCurrencyPrototype];

                    var prototype = listingPrototypes.First(x => x.ID == discountedListingItem.ID);

                    var prototypeCost = prototype.Cost[UplinkSystem.TelecrystalCurrencyPrototype];
                    var discountDownTo = prototype.DiscountDownTo[UplinkSystem.TelecrystalCurrencyPrototype];
                    Assert.That(plainDiscountedCost.Value, Is.GreaterThanOrEqualTo(discountDownTo.Value), "Expected discounted cost to be greater then DiscountDownTo value.");
                    Assert.That(plainDiscountedCost.Value, Is.LessThan(prototypeCost.Value), "Expected discounted cost to be lower then prototype cost.");


                    var buyMsg = new StoreBuyListingMessage(discountedListingItem.ID){Actor = human};
                    server.EntMan.EventBus.RaiseComponentEvent(pda, storeComponent, buyMsg);

                    var newBalance = storeComponent.Balance[UplinkSystem.TelecrystalCurrencyPrototype];
                    Assert.That(newBalance.Value, Is.EqualTo((originalBalance - plainDiscountedCost).Value), "Expected to have balance reduced by discounted cost");
                    Assert.That(
                        discountedListingItem.IsCostModified,
                        Is.False,
                        $"Expected item cost to not be modified after Buying discounted item."
                    );
                    var costAfterBuy = discountedListingItem.Cost[UplinkSystem.TelecrystalCurrencyPrototype];
                    Assert.That(costAfterBuy.Value, Is.EqualTo(prototypeCost.Value), "Expected cost after discount refund to be equal to prototype cost.");

                    var refundMsg = new StoreRequestRefundMessage { Actor = human };
                    server.EntMan.EventBus.RaiseComponentEvent(pda, storeComponent, refundMsg);

                    // get refreshed item after refund re-generated items
                    discountedListingItem = storeComponent.FullListingsCatalog.First(x => x.ID == itemId);

                    var afterRefundBalance = storeComponent.Balance[UplinkSystem.TelecrystalCurrencyPrototype];
                    Assert.That(afterRefundBalance.Value, Is.EqualTo(originalBalance.Value), "Expected refund to return all discounted cost value.");
                    Assert.That(
                        discountComponent.Discounts.First(x => x.ListingId == discountedListingItem.ID).Count,
                        Is.EqualTo(0),
                        "Discounted count should still be zero even after refund."
                    );

                    Assert.That(
                        discountedListingItem.IsCostModified,
                        Is.False,
                        $"Expected item cost to not be modified after Buying discounted item (even after refund was done)."
                    );
                    var costAfterRefund = discountedListingItem.Cost[UplinkSystem.TelecrystalCurrencyPrototype];
                    Assert.That(costAfterRefund.Value, Is.EqualTo(prototypeCost.Value), "Expected cost after discount refund to be equal to prototype cost.");
                });
            }

        });

        await pair.CleanReturnAsync();
    }
}
