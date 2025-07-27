#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Doors.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Doors.Components;

namespace Content.IntegrationTests.Tests.Prying;

public sealed class AirlockPryingTest : InteractionTest
{

    /// <summary>
    /// Generic test to test the interaction of prying a door with or without a tool, powered/unpowered, bolted/unbolted
    /// </summary>
    /// <param name="initialState"></param>
    /// <param name="pryingToolProtoId"></param>
    /// <param name="bolted"></param>
    /// <param name="powered"></param>
    [Test, Combinatorial]
    public async Task GenericAirlockPryingTest(
        [Values(DoorState.Closed, DoorState.Open, DoorState.Welded)]
        DoorState initialState,
        [Values(null, Pry, PryPowered, ForcedPryer)]
        string? pryingToolProtoId,
        [Values] bool bolted,
        [Values] bool powered)
    {
        var expectedState = GetExpectedState(initialState, pryingToolProtoId, bolted, powered);
        await SpawnTarget(Airlock);
        // We need power to bolt and unbolt, so always spawn an APC
        var apcEntity = await SpawnEntity("APCBasic", SEntMan.GetCoordinates(TargetCoords));

        await RunTicks(1);

        Assert.That(TryComp<AirlockComponent>(out var airlockComp), "Airlock does not have AirlockComponent?");

        var airlockSys = SEntMan.System<AirlockSystem>();
        var doorSys = SEntMan.System<DoorSystem>();
        var apcSys = SEntMan.System<ApcSystem>();

        // Make sure the door does not auto close for long DoAfters on powered doors
        await Server.WaitPost(() => airlockSys.SetAutoCloseDelayModifier(airlockComp!, 0.0f));
        await Server.WaitPost(() => doorSys.SetState(SEntMan.GetEntity(Target.Value), initialState));
        await RunTicks(1);

        if (bolted)
        {
            Assert.That(TryComp<DoorBoltComponent>(out var doorBoltComp), "Airlock does not have DoorBoltComponent?");
            await Server.WaitPost(() => doorSys.TrySetBoltDown((STarget.Value, doorBoltComp!), true));
            await RunTicks(1);
        }

        if (!powered)
        {
            // If it should be powered, we can toggle it off now.
            await Server.WaitPost(() => apcSys.ApcToggleBreaker(apcEntity));
            await RunTicks(1);
        }

        // Initial condition checks
        Assert.That(airlockComp!.Powered, Is.EqualTo(powered), "Airlock power state incorrect for this test.");
        Assert.That(TryComp<DoorComponent>(out var doorComp), "Airlock does not have DoorComponent?");
        Assert.That(doorComp!.State, Is.EqualTo(initialState), "Airlock has incorrect initial state");
        Assert.That(doorSys.IsBolted(STarget.Value), Is.EqualTo(bolted), "Airlock has incorrected bolted state.");

        // Attempt prying, use hands if null
        if (pryingToolProtoId is not null)
            await InteractUsing(pryingToolProtoId);
        else
            await Interact();

        Assert.That(doorComp.State, Is.EqualTo(expectedState), "Airlock has incorrect final state");

    }

    /// <summary>
    /// Get the expected state for a given interaction test case
    /// </summary>
    /// <param name="initialState"></param>
    /// <param name="pryingToolProtoId"></param>
    /// <param name="bolted"></param>
    /// <param name="powered"></param>
    /// <returns></returns>
    private static DoorState GetExpectedState(DoorState initialState, string? pryingToolProtoId, bool bolted, bool powered)
    {
        // Just a simple map for the "successful" door interactions
        var normalTransitions = new Dictionary<DoorState, DoorState>()
        {
            [DoorState.Closed] = DoorState.Opening,
            [DoorState.Open] = DoorState.Closing,
        };

        // Nothing can open welded doors
        if (initialState == DoorState.Welded)
            return initialState;

        // From this point forward we don't have to worry about welded doors
        switch (pryingToolProtoId)
        {
            // Hand pry
            case null:
                // Ignore these cases because an interaction will not initiate a pry action
                if (powered && !bolted)
                    Assert.Ignore("Hand interaction on powered door will not pry.");
                // Cannot hand pry bolted or powered doors
                return bolted || powered
                    ? initialState
                    : normalTransitions[initialState];
            case Pry:
                // Cannot pry bolted or powered doors
                return bolted || powered
                    ? initialState
                    : normalTransitions[initialState];
            case PryPowered:
                // Can pry powered doors, but not bolted
                return bolted
                    ? initialState
                    : normalTransitions[initialState];
            case ForcedPryer:
                // I don't think this is intended, but forced prying cannot close bolted doors
                // This is stopped in the "BeforeDoorClosedEvent" handler in SharedBoltSystem
                // But for now only zombies and some animals can force pry so who cares about prying bolts closed?
                return bolted && initialState == DoorState.Open
                    ? initialState
                    : normalTransitions[initialState];
        }

        Assert.Fail("Unknown prying tool");
        return initialState;
    }
}
