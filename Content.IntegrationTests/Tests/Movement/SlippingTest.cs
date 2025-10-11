#nullable enable
using Content.IntegrationTests.Tests.Helpers;
using Content.Shared.Movement.Components;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Movement;

public sealed class SlippingTest : MovementTest
{
    public sealed class SlipTestSystem : TestListenerSystem<SlipEvent>;

    [Test]
    public async Task BananaSlipTest()
    {
        await SpawnTarget("TrashBananaPeel");

        var modifier = Comp<MovementSpeedModifierComponent>(Player).SprintSpeedModifier;
        Assert.That(modifier, Is.EqualTo(1), "Player is not moving at full speed.");

        // Player is to the left of the banana peel.
        Assert.That(Delta(), Is.GreaterThan(0.5f));

        // Walking over the banana slowly does not trigger a slip.
        await SetKey(EngineKeyFunctions.Walk, BoundKeyState.Down);
        await AssertFiresEvent<SlipEvent>(async () => await Move(DirectionFlag.East, 1f), count: 0);

        Assert.That(Delta(), Is.LessThan(0.5f));
        AssertComp<KnockedDownComponent>(false, Player);

        // Moving at normal speeds does trigger a slip.
        await SetKey(EngineKeyFunctions.Walk, BoundKeyState.Up);
        await AssertFiresEvent<SlipEvent>(async () => await Move(DirectionFlag.West, 1f));

        // And the person that slipped was the player
        AssertEvent<SlipEvent>(predicate: @event => @event.Slipped == SPlayer);
        AssertComp<KnockedDownComponent>(true, Player);
    }
}

