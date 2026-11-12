using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests.Hands;

[TestFixture]
[TestOf(typeof(SharedHandsSystem))]
public sealed partial class HandWhitelistTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly SharedHandsSystem _handsSys = default!;

    // Define our dummy entities and tags for testing
    [TestPrototypes]
    private const string Prototypes = @"
- type: Tag
  id: DummyTag

- type: entity
  id: DummyTaggedItem
  components:
  - type: Item
  - type: Tag
    tags:
    - DummyTag

- type: entity
  id: DummyRegularItem
  components:
  - type: Item
";

    [Test]
    public async Task HandWhitelistPickupTest()
    {
        TestContext.Out.WriteLine("--> [TEST-DEBUG] Starting HandWhitelistPickupTest initialization.");
        var sysMan = Server.ResolveDependency<IEntitySystemManager>();

        var map = await Pair.CreateTestMap();
        var coords = map.MapCoords;

        await Server.WaitIdleAsync();

        EntityUid user = default;
        EntityUid taggedItem = default;
        EntityUid regularItem = default;

        await Server.WaitAssertion(() =>
        {
            user = SEntMan.SpawnEntity(null, coords);
            SEntMan.EnsureComponent<HandsComponent>(user);

            // Create a whitelist that strictly requires our dummy tag
            var whitelist = new EntityWhitelist
            {
                Tags = new List<ProtoId<TagPrototype>> { "DummyTag" }
            };

            _handsSys.AddHand(user, "restricted_hand", HandLocation.Right, whitelist: whitelist);

            // Spawn item with and without the required tag
            taggedItem = SEntMan.SpawnEntity("DummyTaggedItem", coords);
            regularItem = SEntMan.SpawnEntity("DummyRegularItem", coords);

            // Attempt to pick up the item that lacks the required tag
            var pickupDenied = _handsSys.TryPickup(user, regularItem);
            Assert.That(pickupDenied, Is.False, "System allowed picking up a non-whitelisted item.");

            // Attempt to pick up the item that has the required tag
            var pickupAllowed = _handsSys.TryPickup(user, taggedItem);
            Assert.That(pickupAllowed, Is.True, "System blocked picking up a whitelisted item.");

            // Verify the state of the HandsComponent accurately reflects the successful pickup
            Assert.That(_handsSys.IsHolding(user, taggedItem, out var holdingHand));
            Assert.That(holdingHand, Is.EqualTo("restricted_hand"));
        });
    }

    [Test]
    public async Task HandBlacklistPickupTest()
    {
        var sysMan = Server.ResolveDependency<IEntitySystemManager>();
        var handSys = sysMan.GetEntitySystem<SharedHandsSystem>();

        var map = await Pair.CreateTestMap();
        var coords = map.MapCoords;

        await Server.WaitIdleAsync();

        EntityUid user = default;
        EntityUid taggedItem = default;
        EntityUid regularItem = default;

        await Server.WaitAssertion(() =>
        {
            user = SEntMan.SpawnEntity(null, coords);
            SEntMan.EnsureComponent<HandsComponent>(user);

            // Create a blacklist that strictly banns tag
            var blacklist = new EntityWhitelist // whitelist of blacklisted items lol
            {
                Tags = new List<ProtoId<TagPrototype>> { "DummyTag" }
            };
            handSys.AddHand(user, "restricted_hand", HandLocation.Right, blacklist: blacklist);

            // Spawn item with and without the tag
            taggedItem = SEntMan.SpawnEntity("DummyTaggedItem", coords);
            regularItem = SEntMan.SpawnEntity("DummyRegularItem", coords);

            // Attempt to pick up the item that has the blacklisted tag
            var pickupOfTagged = handSys.TryPickup(user, taggedItem);
            Assert.That(pickupOfTagged, Is.False, "System allowed picking up a blacklisted item.");

            // Attempt to pick up the item that lacks the blacklisted tag
            var pickupRegular = handSys.TryPickup(user, regularItem);
            Assert.That(pickupRegular, Is.True, "System blocked picking up a non-blacklisted item.");

            // Verify the state of the HandsComponent accurately reflects the successful pickup
            Assert.That(handSys.IsHolding(user, regularItem, out var holdingHand));
            Assert.That(holdingHand, Is.EqualTo("restricted_hand"));
        });
    }
}
