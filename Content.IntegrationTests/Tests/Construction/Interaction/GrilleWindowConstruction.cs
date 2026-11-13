#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

/// <summary>
///     Check that we can build grilles on top of windows, but not the other way around.
/// </summary>
public sealed class GrilleWindowConstruction : InteractionTest
{
    private static readonly EntProtoId Grille = "Grille";
    private static readonly EntProtoId Window = "Window";

    [Test]
    public async Task WindowOnGrille()
    {
        // Construct Grille
        await StartConstruction(Grille);
        await InteractUsing(Rod, 10);
        ClientAssertPrototype(Grille, Target);
        var grille = Target;

        // Construct Window
        await StartConstruction(Window);
        await InteractUsing(Glass, 10);
        ClientAssertPrototype(Window, Target);

        // Deconstruct Window
        await Interact(Screw, Wrench);
        AssertDeleted();

        // Deconstruct Grille
        Target = grille;
        await InteractUsing(Cut);
        AssertDeleted();
    }

    private static readonly TestCaseData[] BlockerTestCases =
    [
        new TestCaseData(Grille, Grille),
        new TestCaseData(Window, Grille),
        new TestCaseData(Window, Window),
    ];

    [Test, TestCaseSource(nameof(BlockerTestCases))]
    public async Task ConstructionBlocker(EntProtoId first, EntProtoId second)
    {
        // Spawn blocking entity
        await SpawnTarget(first);

        // Further construction attempts fail - blocked by first entity interaction.
        await Client.WaitPost(() =>
        {
            var proto = ProtoMan.Index<ConstructionPrototype>(second);
            Assert.That(CConSys.TrySpawnGhost(proto, CEntMan.GetCoordinates(TargetCoords), Direction.South, out _), Is.False);
        });
    }
}

