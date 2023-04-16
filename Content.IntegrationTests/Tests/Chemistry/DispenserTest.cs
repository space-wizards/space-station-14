using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using NUnit.Framework;

namespace Content.IntegrationTests.Tests.Chemistry;

public sealed class DispenserTest : InteractionTest
{
    /// <summary>
    ///     Basic test that checks that a beaker can be inserted and ejected from a dispenser.
    /// </summary>
    [Test]
    public async Task InsertEjectBuiTest()
    {
        await SpawnTarget("chem_dispenser");
        ToggleNeedPower();

        // Insert beaker
        await Interact("Beaker");
        Assert.IsNull(Hands.ActiveHandEntity);

        // Open BUI
        await Interact("");

        // Eject beaker via BUI.
        var ev = new ItemSlotButtonPressedEvent(SharedChemMaster.InputSlotName);
        await SendBui(ReagentDispenserUiKey.Key, ev);

        // Beaker is back in the player's hands
        Assert.IsNotNull(Hands.ActiveHandEntity);
        AssertPrototype("Beaker", Hands.ActiveHandEntity);
    }
}
