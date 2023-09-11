using Content.Client.Chemistry.UI;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;

namespace Content.IntegrationTests.Tests.Chemistry;

public sealed class DispenserTest : InteractionTest
{
    /// <summary>
    ///     Basic test that checks that a beaker can be inserted and ejected from a dispenser.
    /// </summary>
    [Test]
    public async Task InsertEjectBuiTest()
    {
        await SpawnTarget("ChemDispenser");
        ToggleNeedPower();

        // Insert beaker
        await Interact("Beaker");
        Assert.That(Hands.ActiveHandEntity, Is.Null);

        // Open BUI
        await Interact();

        // Eject beaker via BUI.
        var ev = new ItemSlotButtonPressedEvent(SharedChemMaster.InputSlotName);
        await SendBui(ReagentDispenserUiKey.Key, ev);

        // Beaker is back in the player's hands
        Assert.That(Hands.ActiveHandEntity, Is.Not.Null);
        AssertPrototype("Beaker", SEntMan.GetNetEntity(Hands.ActiveHandEntity));

        // Re-insert the beaker
        await Interact();
        Assert.That(Hands.ActiveHandEntity, Is.Null);

        // Re-eject using the button directly instead of sending a BUI event. This test is really just a test of the
        // bui/window helper methods.
        await ClickControl<ReagentDispenserWindow>(nameof(ReagentDispenserWindow.EjectButton));
        await RunTicks(5);
        Assert.That(Hands.ActiveHandEntity, Is.Not.Null);
        AssertPrototype("Beaker", SEntMan.GetNetEntity(Hands.ActiveHandEntity));
    }
}
