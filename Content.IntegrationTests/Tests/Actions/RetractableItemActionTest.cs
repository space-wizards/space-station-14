#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.RetractableItemAction;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Actions;

public sealed class RetractableItemActionTest : InteractionTest
{
    private static readonly EntProtoId ArmBladeActionProtoId = "ActionRetractableItemArmBlade";

    /// <summary>
    /// Gives the player the arm blade action, then activates it and makes sure they are given the blade.
    /// Afterwards, uses the action again to retract the blade and makes sure their hand is empty.
    /// </summary>
    [Test]
    public async Task ArmBladeActivateDeactivateTest()
    {
        var actionsSystem = Server.System<SharedActionsSystem>();
        var handsSystem = Server.System<SharedHandsSystem>();
        var playerUid = SEntMan.GetEntity(Player);

        await Server.WaitAssertion(() =>
        {
            // Make sure the player's hand starts empty
            var heldItem = handsSystem.GetActiveItem((playerUid, Hands));
            Assert.That(heldItem, Is.Null, $"Player is holding an item ({SEntMan.ToPrettyString(heldItem)}) at start of test.");

            // Inspect the action prototype to find the item it spawns
            var armBladeActionProto = ProtoMan.Index(ArmBladeActionProtoId);

            // Find the component
            Assert.That(armBladeActionProto.TryGetComponent<RetractableItemActionComponent>(out var actionComp, SEntMan.ComponentFactory));
            // Get the item protoId from the component
            var spawnedProtoId = actionComp!.SpawnedPrototype;

            // Add the action to the player
            var actionUid = actionsSystem.AddAction(playerUid, ArmBladeActionProtoId);
            // Make sure the player has the action now
            Assert.That(actionUid, Is.Not.Null, "Failed to add action to player.");
            var actionEnt = actionsSystem.GetAction(actionUid);

            // Make sure the player's hand is still empty
            heldItem = handsSystem.GetActiveItem((playerUid, Hands));
            Assert.That(heldItem, Is.Null, $"Player is holding an item ({SEntMan.ToPrettyString(heldItem)}) after adding action.");

            // Activate the arm blade
            actionsSystem.PerformAction(ToServer(Player), actionEnt!.Value);

            // Make sure the player is now holding the expected item
            heldItem = handsSystem.GetActiveItem((playerUid, Hands));
            Assert.That(heldItem, Is.Not.Null, $"Expected player to be holding {spawnedProtoId} but was holding nothing.");
            AssertPrototype(spawnedProtoId, SEntMan.GetNetEntity(heldItem));

            // Use the action again to retract the arm blade
            actionsSystem.PerformAction(ToServer(Player), actionEnt.Value);

            // Make sure the player's hand is empty again
            heldItem = handsSystem.GetActiveItem((playerUid, Hands));
            Assert.That(heldItem, Is.Null, $"Player is still holding an item ({SEntMan.ToPrettyString(heldItem)}) after second use.");
        });
    }
}
