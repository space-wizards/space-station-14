#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Wires;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class PanelScrewing : InteractionTest
{
    private static readonly EntProtoId TelecomServer = "TelecomServerFilled";

    private static readonly TestCaseData[] TestCases =
    [
        new TestCaseData(Airlock),
        new TestCaseData(TelecomServer),
    ];

    // Test wires panel on both airlocks & tcomms servers. These both use the same component, but comms may have
    // conflicting interactions due to encryption key removal interactions.
    [Test, TestCaseSource(nameof(TestCases))]
    public async Task WiresPanelScrewing(EntProtoId prototype)
    {
        await SpawnTarget(prototype);
        var comp = Comp<WiresPanelComponent>();

        // Open & close panel
        Assert.That(comp.Open, Is.False);
        await InteractUsing(Screw);
        Assert.That(comp.Open, Is.True);
        await InteractUsing(Screw);
        Assert.That(comp.Open, Is.False);

        // Interrupted DoAfters
        await InteractUsing(Screw, awaitDoAfters: false);
        await CancelDoAfters();
        Assert.That(comp.Open, Is.False);
        await InteractUsing(Screw);
        Assert.That(comp.Open, Is.True);
        await InteractUsing(Screw, awaitDoAfters: false);
        await CancelDoAfters();
        Assert.That(comp.Open, Is.True);
        await InteractUsing(Screw);
        Assert.That(comp.Open, Is.False);
    }
}

