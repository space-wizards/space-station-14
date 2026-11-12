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

    private const string TagEntityId = "TestDummy";
    private const string DummyTaggedItemId = "DummyTaggedItem";
    private const string DummyRegularItemId = "DummyRegularItem";
    private const string DummyTagId = "DummyTag";
    private const string RestrictedHandId = "restricted_hand";

    // Define our dummy entities and tags for testing
    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {TagEntityId}
  components:
  - type: Hands

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
    public void HandWhitelistPickupTest()
    {
        EntityUid user = default;
        EntityUid taggedItem = default;
        EntityUid regularItem = default;

        user = SSpawn(TagEntityId);

        // Create a whitelist that strictly requires our dummy tag
        var whitelist = new EntityWhitelist
        {
            Tags = new List<ProtoId<TagPrototype>> { DummyTagId }
        };

        _handsSys.AddHand(user, RestrictedHandId, HandLocation.Right, whitelist: whitelist);

        // Spawn item with and without the required tag
        taggedItem = SSpawn(DummyTaggedItemId);
        regularItem = SSpawn(DummyRegularItemId);

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
    public void HandBlacklistPickupTest()
    {
        EntityUid user = default;
        EntityUid taggedItem = default;
        EntityUid regularItem = default;

        user = SSpawn(TagEntityId);

        // Create a blacklist that strictly banns tag
        var blacklist = new EntityWhitelist // whitelist of blacklisted items lol
        {
            Tags = new List<ProtoId<TagPrototype>> { DummyTagId }
        };
        _handsSys.AddHand(user, RestrictedHandId, HandLocation.Right, blacklist: blacklist);

        // Spawn item with and without the tag
        taggedItem = SSpawn(DummyTaggedItemId);
        regularItem = SSpawn(DummyRegularItemId);

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
