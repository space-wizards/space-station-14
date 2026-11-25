using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Kitchen;

public sealed partial class HandheldGrinderInteractionTest : InteractionTest
{
    private static readonly EntProtoId Mortar = "MortarAndPestle";
    private static readonly EntProtoId Juicer = "HandheldJuicer";

    private static readonly EntProtoId SteelSheet = "SheetSteel1";
    private const string Banana = "TestFoodBanana";

    [TestPrototypes]
    private const string Prototypes = @$"
# A modified banana that can only be juiced.
- type: entity
  parent: FoodBanana
  id: {Banana}
  components:
  - type: Extractable
    grindableSolutionName: null
";

    /// <summary>
    /// Spawns a mortar and grinds steel in it, then grinds an ungrindable banana.
    /// Does the same with a juicer, but first a banana and then steel.
    /// </summary>
    [Test]
    public async Task GrindAndJuiceInHandheldGrindersTest()
    {
        var grinderSys = SEntMan.System<SharedReagentGrinderSystem>();
        var solutionSys = SEntMan.System<SharedSolutionContainerSystem>();
        var stackSys = SEntMan.System<SharedStackSystem>();

        // Spawn an empty mortar
        await SpawnTarget(Mortar);
        var grinderComp = Comp<HandheldGrinderComponent>();

        // Spawn steel sheets and get what solution they should grind into.
        var sheetsEnt = await PlaceInHands(SteelSheet, 2);
        var expectedGrinderSol = grinderSys.GetGrinderSolution(ToServer(sheetsEnt), GrinderProgram.Grind);
        Assert.That(expectedGrinderSol, Is.Not.Null); // We expect a solution to exist, would suck if it didn't.

        await Interact();

        Assert.That(grinderComp.GrinderSolution, Is.Not.Null); // The grinder needs to have its valid solution resolved after interaction.
        Assert.That(expectedGrinderSol.Contents.SequenceEqual(grinderComp.GrinderSolution.Value.Comp.Solution.Contents)); // Check if the solution is the one we expected.
        Assert.That(stackSys.GetCount(ToServer(sheetsEnt)), Is.EqualTo(1)); // We had 2 steel, now we have one.

        await Interact();

        Assert.That(SEntMan.EntityExists(ToServer(sheetsEnt)), Is.False); // We use the other sheet, so we used all of our steel.


        // Spawn a new grinder
        await SpawnTarget(Mortar);
        grinderComp = Comp<HandheldGrinderComponent>();

        // Manually resolve the solution because the system only resolves it after a VALID interaction, and here we test an invalid one.
        solutionSys.ResolveSolution(STarget.Value, grinderComp.SolutionName, ref grinderComp.GrinderSolution);
        Assert.That(grinderComp.GrinderSolution, Is.Not.Null);

        var bananaEnt = await PlaceInHands(Banana);

        await Interact();

        Assert.That(grinderComp.GrinderSolution.Value.Comp.Solution.Volume.Float(), Is.Not.GreaterThan(0f)); // The banana shouldn't have been ground, since it can only be juiced.
        Assert.That(SEntMan.EntityExists(ToServer(bananaEnt))); // Banana should exist since the interaction failed.


        // Now we test the juicer, so we spawn one.
        await SpawnTarget(Juicer);
        grinderComp = Comp<HandheldGrinderComponent>();
        bananaEnt = await PlaceInHands(Banana);
        var expectedJuicerSol = grinderSys.GetGrinderSolution(ToServer(bananaEnt), GrinderProgram.Juice);
        Assert.That(expectedJuicerSol, Is.Not.Null); // We expect a solution to exist, would suck if it didn't.

        await Interact();

        Assert.That(grinderComp.GrinderSolution, Is.Not.Null); // Juicer has a valid solution.
        Assert.That(expectedJuicerSol.Contents.SequenceEqual(grinderComp.GrinderSolution.Value.Comp.Solution.Contents)); // The banana has been juiced.
        Assert.That(SEntMan.EntityExists(ToServer(bananaEnt)), Is.False); // Banana was juiced and therefore should no longer exist.


        // Spawn a new juicer
        await SpawnTarget(Juicer);
        grinderComp = Comp<HandheldGrinderComponent>();

        // Manually resolve the solution because the system only resolves it after a VALID interaction, and here we test an invalid one.
        solutionSys.ResolveSolution(STarget.Value, grinderComp.SolutionName, ref grinderComp.GrinderSolution);
        Assert.That(grinderComp.GrinderSolution, Is.Not.Null);

        sheetsEnt = await PlaceInHands(SteelSheet, 2);

        await Interact();

        Assert.That(grinderComp.GrinderSolution.Value.Comp.Solution.Volume.Float() , Is.Not.GreaterThan(0f)); // The steel cannot be juiced.
        Assert.That(SEntMan.EntityExists(ToServer(sheetsEnt))); // Steel was not used so it should exist.
        Assert.That(stackSys.GetCount(ToServer(sheetsEnt)), Is.EqualTo(2)); // Steel was not used, we should have two.
    }
}
