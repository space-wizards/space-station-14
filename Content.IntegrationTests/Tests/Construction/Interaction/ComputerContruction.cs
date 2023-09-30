using Content.IntegrationTests.Tests.Interaction;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class ComputerConstruction : InteractionTest
{
    private const string Computer = "Computer";
    private const string ComputerId = "ComputerId";
    private const string ComputerFrame = "ComputerFrame";
    private const string IdBoard = "IDComputerCircuitboard";

    [Test]
    public async Task ConstructComputer()
    {
        // Place ghost
        await StartConstruction(Computer);

        // Initial interaction (ghost turns into real entity)
        await Interact(Steel, 5);
        ClientAssertPrototype(ComputerFrame, ClientTarget);
        Target = CTestSystem.Ghosts[ClientTarget!.Value.GetHashCode()];
        ClientTarget = null;

        // Perform construction steps
        await Interact(
            Wrench,
            IdBoard,
            Screw,
            (Cable, 5),
            (Glass, 2),
            Screw);

        // Construction finished, target entity was replaced with a new one:
        AssertPrototype(ComputerId, Target);
    }

    [Test]
    public async Task DeconstructComputer()
    {
        // Spawn initial entity
        await StartDeconstruction(ComputerId);

        // Initial interaction turns id computer into generic computer
        await Interact(Screw);
        AssertPrototype(ComputerFrame);

        // Perform deconstruction steps
        await Interact(
            Pry,
            Cut,
            Screw,
            Pry,
            Wrench,
            Weld);

        // construction finished, entity no longer exists.
        AssertDeleted();

        // Check expected entities were dropped.
        await AssertEntityLookup(
            IdBoard,
            (Cable, 5),
            (Steel, 5),
            (Glass, 2));
    }

    [Test]
    public async Task ChangeComputer()
    {
        // Spawn initial entity
        await SpawnTarget(ComputerId);

        // Initial interaction turns id computer into generic computer
        await Interact(Screw);
        AssertPrototype(ComputerFrame);

        // Perform partial deconstruction steps
        await Interact(
            Pry,
            Cut,
            Screw,
            Pry);

        // Entity should still exist
        AssertPrototype(ComputerFrame);

        // Begin re-constructing with a new circuit board
        await Interact(
            "CargoRequestComputerCircuitboard",
            Screw,
            (Cable, 5),
            (Glass, 2),
            Screw);

        // Construction finished, target entity was replaced with a new one:
        AssertPrototype("ComputerCargoOrders");
    }
}

