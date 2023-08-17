using Content.IntegrationTests.Tests.Interaction;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class WindowConstruction : InteractionTest
{
    private const string Window = "Window";
    private const string RWindow = "ReinforcedWindow";

    [Test]
    public async Task ConstructWindow()
    {
        await StartConstruction(Window);
        await Interact(Glass, 5);
        ClientAssertPrototype(Window, ClientTarget);
    }

    [Test]
    public async Task DeconstructWindow()
    {
        await StartDeconstruction(Window);
        await Interact(Screw, Wrench);
        AssertDeleted();
        await AssertEntityLookup((Glass, 2));
    }

    [Test]
    public async Task ConstructReinforcedWindow()
    {
        await StartConstruction(RWindow);
        await Interact(RGlass, 5);
        ClientAssertPrototype(RWindow, ClientTarget);
    }

    [Test]
    public async Task DeonstructReinforcedWindow()
    {
        await StartDeconstruction(RWindow);
        await Interact(
            Weld,
            Screw,
            Pry,
            Weld,
            Screw,
            Wrench);
        AssertDeleted();
        await AssertEntityLookup((RGlass, 2));
    }
}

