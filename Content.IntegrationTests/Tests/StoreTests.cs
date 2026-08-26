#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.PDA.Ringer;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests;

public sealed class StoreTests : GameTest
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
    [Description("Tests that a traitor PDA works as a store, that it can purchase, discount and refund items.")]
    public async Task StoreDiscountAndRefund()
    {
        await Pair.CreateTestMap();

        Assume.That(TestMap, Is.Not.Null);

        var serverRandom = Server.ResolveDependency<IRobustRandom>();
        serverRandom.SetSeed(534);

        var mapSystem = Server.System<SharedMapSystem>();

        Assume.That(mapSystem.IsInitialized(TestMap.MapId));

        EntityUid human = default;
        EntityUid uniform = default;
        EntityUid pda = default;

        var uplinkSystem = Server.System<UplinkSystem>();
        var ringerSystem = Server.System<RingerSystem>();

        var listingPrototypes = SProtoMan.EnumeratePrototypes<ListingPrototype>()
                                         .ToArray();

        var coordinates = TestMap.GridCoords;
        await Server.WaitAssertion(() =>
        {
            var invSystem = Server.System<InventorySystem>();
            var mindSystem = Server.System<SharedMindSystem>();

            human = SSpawnAtPosition("MobHuman", coordinates);
            uniform = SSpawnAtPosition("UniformDummy", coordinates);
            pda = SSpawnAtPosition("InventoryPdaDummy", coordinates);

            Assume.That(invSystem.TryEquip(human, uniform, "jumpsuit"));
            Assume.That(invSystem.TryEquip(human, pda, "id"));

            var mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, human, mind: mind);

            FixedPoint2 originalBalance = 20;
            uplinkSystem.AddUplink(human, originalBalance, out var notes, pda, true);

            Assert.That(notes != null);
            ringerSystem.TryMatchRingtoneToStore(notes!, out var storeEnt);
            Assert.That(storeEnt.HasValue);
            var storeComponent = SEntMan.GetComponent<StoreComponent>(storeEnt.Value);
            var discountComponent = SEntMan.GetComponent<StoreDiscountComponent>(storeEnt.Value);
            Assert.That(
                discountComponent.Discounts,
                Has.Exactly(6).Items,
                $"After applying discount total discounted items count was expected to be '6' "
                + $"but was actually {discountComponent.Discounts.Count}- this can be due to discount "
                + $"categories settings (maxItems, weight) not being realistically set, or default "
                + $"discounted count being changed from '6' in StoreDiscountSystem.InitializeDiscounts."
            );
            var discountedListingItems = storeComponent.FullListingsCatalog
                                                       .Where(x => x.IsCostModified)
                                                       .OrderBy(x => x.ID);
            Assert.That(discountComponent.Discounts
                                         .Select(x => x.ListingId.Id),
                Is.EquivalentTo(discountedListingItems.Select(x => x.ID)),
                $"{nameof(StoreComponent)}.{nameof(StoreComponent.FullListingsCatalog)} does not contain all "
                + $"items that are marked as discounted, or they don't have flag '{nameof(ListingDataWithCostModifiers.IsCostModified)}'"
                + $"flag as 'true'. This marks the fact that cost modifier of discount is not applied properly!"
            );

            // The storeComponent returns discounted items with conditions randomly, so we remove these to sanitize the data.
            foreach (var discountedItem in discountedListingItems)
                discountedItem.Conditions = null;

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


                    var buyMsg = new StoreBuyListingMessage(discountedListingItem.ID, null) { Actor = human };
                    SEntMan.EventBus.RaiseLocalEvent(storeEnt.Value, buyMsg);

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
                    SEntMan.EventBus.RaiseLocalEvent(storeEnt.Value, refundMsg);

                    // get refreshed item after refund re-generated items
                    discountedListingItem = storeComponent.FullListingsCatalog.First(x => x.ID == itemId);

                    // The storeComponent can give a discounted item a condition at random, so we remove it to sanitize the data.
                    discountedListingItem.Conditions = null;

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
    }
}
