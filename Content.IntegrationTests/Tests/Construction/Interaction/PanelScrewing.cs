using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Wires;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class PanelScrewing : InteractionTest
{
    // Test wires panel on both airlocks & tcomms servers. These both use the same component, but comms may have
    // conflicting interactions due to encryption key removal interactions.
    [Test]
    [TestCase("Airlock")]
    [TestCase("TelecomServerFilled")]
    public async Task WiresPanelScrewing(string prototype)
    {
        await SpawnTarget(prototype);
        var comp = Comp<WiresPanelComponent>();

        // Open & close panel
        Assert.That(comp.Open, Is.False);
        await Interact(Screw);
        Assert.That(comp.Open, Is.True);
        await Interact(Screw);
        Assert.That(comp.Open, Is.False);

        // Interrupted DoAfters
        await Interact(Screw, awaitDoAfters: false);
        await CancelDoAfters();
        Assert.That(comp.Open, Is.False);
        await Interact(Screw);
        Assert.That(comp.Open, Is.True);
        await Interact(Screw, awaitDoAfters: false);
        await CancelDoAfters();
        Assert.That(comp.Open, Is.True);
        await Interact(Screw);
        Assert.That(comp.Open, Is.False);
    }
}

