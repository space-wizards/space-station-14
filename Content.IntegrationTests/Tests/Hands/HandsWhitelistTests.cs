using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests.Hands;

[TestFixture]
[TestOf(typeof(SharedHandsSystem))]
public sealed partial class HandWhitelistTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly SharedHandsSystem _handsSys = default!;
    public override PoolSettings PoolSettings => new() { Dirty = true };

    private const string EntityWithHandWhitelist = "TestDummyWithWhitelist";
    private const string EntityWithHandBlackList = "TestDummyWithBlacklist";
    private const string DummyTaggedItemId = "DummyTaggedItem";
    private const string DummyRegularItemId = "DummyRegularItem";
    private const string DummyTagId = "DummyTag";
    private const string RestrictedHandId = "restricted_hand";

    // Define our dummy entities and tags for testing
    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {EntityWithHandWhitelist}
  components:
  - type: Hands
    startingHands:
      {RestrictedHandId}:
        location: Right
        whitelist:
          tags:
          - {DummyTagId}

- type: entity
  id: {EntityWithHandBlackList}
  components:
  - type: Hands
    startingHands:
      {RestrictedHandId}:
        location: Right
        blacklist:
          tags:
          - {DummyTagId}

- type: Tag
  id: {DummyTagId}

- type: entity
  id: {DummyTaggedItemId}
  components:
  - type: Item
  - type: Tag
    tags:
    - {DummyTagId}

- type: entity
  id: {DummyRegularItemId}
  components:
  - type: Item
";

    [Test]
    [RunOnSide(Side.Server)]
    public async Task HandWhitelistPickupTest()
    {
        var user = SSpawn(EntityWithHandWhitelist);

        // Spawn item with and without the required tag
        var taggedItem = SSpawn(DummyTaggedItemId);
        var regularItem = SSpawn(DummyRegularItemId);

        Assume.That(STryComp<HandsComponent>(user, out var _));
        using (Assert.EnterMultipleScope())
        {
            // Attempt to pick up the item that lacks the required tag
            var pickupDenied = _handsSys.TryPickup(user, regularItem);
            Assert.That(pickupDenied, Is.False, "System allowed picking up a non-whitelisted item.");

            // Attempt to pick up the item that has the required tag
            var pickupAllowed = _handsSys.TryPickup(user, taggedItem);
            Assert.That(pickupAllowed, Is.True, "System blocked picking up a whitelisted item.");

            // Verify the state of the HandsComponent accurately reflects the successful pickup
            Assert.That(_handsSys.IsHolding(user, taggedItem, out var holdingHand));
            Assert.That(holdingHand, Is.EqualTo(RestrictedHandId));
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task HandBlacklistPickupTest()
    {
        var user = SSpawn(EntityWithHandBlackList);

        // Spawn item with and without the tag
        var taggedItem = SSpawn(DummyTaggedItemId);
        var regularItem = SSpawn(DummyRegularItemId);

        Assume.That(STryComp<HandsComponent>(user, out var _));
        using (Assert.EnterMultipleScope())
        {
            // Attempt to pick up the item that has the blacklisted tag
            var pickupOfTagged = _handsSys.TryPickup(user, taggedItem);
            Assert.That(pickupOfTagged, Is.False, "System allowed picking up a blacklisted item.");

            // Attempt to pick up the item that lacks the blacklisted tag
            var pickupRegular = _handsSys.TryPickup(user, regularItem);
            Assert.That(pickupRegular, Is.True, "System blocked picking up a non-blacklisted item.");

            // Verify the state of the HandsComponent accurately reflects the successful pickup
            Assert.That(_handsSys.IsHolding(user, regularItem, out var holdingHand));
            Assert.That(holdingHand, Is.EqualTo(RestrictedHandId));
        }
    }
}
