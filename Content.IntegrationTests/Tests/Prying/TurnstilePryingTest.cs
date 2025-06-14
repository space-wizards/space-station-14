#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Doors;
using Content.Server.Doors.Systems;
using Content.Server.Wires;
using Content.Shared.Doors.Components;

namespace Content.IntegrationTests.Tests.Prying;

public sealed class TurnstilePryingTest : InteractionTest
{
    /// <summary>
    /// Generic test to test the interaction of prying a turnstile
    /// </summary>
    /// <param name="pryingToolProtoId"></param>
    /// <param name="bolted"></param>
    /// <param name="solenoidBypassed"></param>
    [Test, Combinatorial]
    public async Task GenericTurnstilePryingTest(
        [Values(null, Pry, PryPowered, ForcedPryer)]
        string? pryingToolProtoId,
        [Values] bool bolted,
        [Values] bool solenoidBypassed)
    {
        var expectedState = GetExpectedValue(pryingToolProtoId, bolted, solenoidBypassed);
        await SpawnTarget(Turnstile);
        await RunTicks(1);

        Assert.That(TryComp<TurnstileComponent>(out var turnstileComponent), "Turnstile does not have TurnstileComponent?");

        var boltSys = SEntMan.System<BoltSystem>();
        var wiresSys = SEntMan.System<WiresSystem>();

        // Make sure the door does not auto close for long DoAfters on powered doors
        await RunTicks(1);

        if (bolted)
        {
            Assert.That(TryComp<DoorBoltComponent>(out var doorBoltComp), "Turnstile does not have DoorBoltComponent?");
            await Server.WaitPost(() => boltSys.TrySetBoltsDown((STarget.Value, doorBoltComp!), true));
            await RunTicks(1);
        }

        if (solenoidBypassed)
        {
            Assert.That(TryComp<WiresComponent>(out var wiresComponent));
            var wires = wiresSys.TryGetWires<TurnstileSolenoidWireAction>(STarget.Value, wiresComponent).ToList();
            Assert.That(wires, Is.Not.Empty, "Solenoid wire not found");
            foreach (var wire in wires)
            {
                Assert.That(wire.Action, Is.Not.Null, "Solenoid wire action is null");
                await Server.WaitPost(() => wire.Action.Cut(STarget.Value, wire));
                break;
            }
        }

        // Initial condition checks
        Assert.That(turnstileComponent, Is.Not.Null);
        Assert.That(turnstileComponent.SolenoidBypassed, Is.EqualTo(solenoidBypassed), "Turnstile has incorrect solenoid bypass state");
        Assert.That(boltSys.IsBolted(STarget.Value), Is.EqualTo(bolted), "Turnstile has incorrect bolted state.");
        Assert.That(turnstileComponent.PriedExceptions, Is.Empty, "Turnstile pried exceptions are not empty");

        // Attempt prying, use hands if null
        if (pryingToolProtoId is not null)
            await InteractUsing(pryingToolProtoId);
        else
            await Interact();

        // Ensure that the player is now in the list of pried exceptions.
        Assert.That(turnstileComponent.PriedExceptions.AsReadOnly().ContainsKey(SPlayer), Is.EqualTo(expectedState), "Turnstile final pried state is incorrect");
    }

    /// <summary>
    /// Get the expected state for a given interaction test case
    /// </summary>
    /// <param name="pryingToolProtoId"></param>
    /// <param name="bolted"></param>
    /// <param name="solenoidBypassed"></param>
    /// <returns></returns>
    private static bool GetExpectedValue(string? pryingToolProtoId, bool bolted, bool solenoidBypassed)
    {
        switch (pryingToolProtoId)
        {
            // Hand pry
            case null:
                // Can't hand pry turnstile
                return false;
            case Pry:
                // Basic tool can pry the turnstile only if the solenoid is bypassed and it's not bolted.
                return !bolted && solenoidBypassed;
            case PryPowered:
                // Powered tool can only pry if the turnstile is not bolted.
                return !bolted;
            case ForcedPryer:
                // Forced pry can always bypass a turnstile
                return true;
        }

        Assert.Fail("Unknown prying tool");
        return false;
    }
}
