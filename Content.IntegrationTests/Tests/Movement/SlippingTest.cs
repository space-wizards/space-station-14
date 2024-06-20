#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Movement;

public sealed class SlippingTest : MovementTest
{
    public sealed class SlipTestSystem : EntitySystem
    {
        public HashSet<EntityUid> Slipped = new();
        public override void Initialize()
        {
            SubscribeLocalEvent<SlipperyComponent, SlipEvent>(OnSlip);
        }

        private void OnSlip(EntityUid uid, SlipperyComponent component, ref SlipEvent args)
        {
            Slipped.Add(args.Slipped);
        }
    }

    [Test]
    public async Task BananaSlipTest()
    {
        var sys = SEntMan.System<SlipTestSystem>();
        await SpawnTarget("TrashBananaPeel");

        var modifier = Comp<MovementSpeedModifierComponent>(Player).SprintSpeedModifier;
        Assert.That(modifier, Is.EqualTo(1), "Player is not moving at full speed.");

        // Player is to the left of the banana peel and has not slipped.
        Assert.That(Delta(), Is.GreaterThan(0.5f));
        Assert.That(sys.Slipped, Does.Not.Contain(SEntMan.GetEntity(Player)));

        // Walking over the banana slowly does not trigger a slip.
        await SetKey(EngineKeyFunctions.Walk, BoundKeyState.Down);
        await Move(DirectionFlag.East, 1f);
        Assert.That(Delta(), Is.LessThan(0.5f));
        Assert.That(sys.Slipped, Does.Not.Contain(SEntMan.GetEntity(Player)));
        AssertComp<KnockedDownComponent>(false, Player);

        // Moving at normal speeds does trigger a slip.
        await SetKey(EngineKeyFunctions.Walk, BoundKeyState.Up);
        await Move(DirectionFlag.West, 1f);
        Assert.That(sys.Slipped, Does.Contain(SEntMan.GetEntity(Player)));
        AssertComp<KnockedDownComponent>(true, Player);
    }
}

