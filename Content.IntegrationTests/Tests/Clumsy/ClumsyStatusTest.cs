using System.Numerics;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Helpers;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Events;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Climbing.Systems;
using Content.Shared.Clumsy;
using Content.Shared.Medical;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameObjects;
using static Content.IntegrationTests.Tests.Clumsy.ClumsyTestPrototypes;

namespace Content.IntegrationTests.Tests.Clumsy;

[TestFixture]
[TestOf(typeof(ClumsyStatusEffectSystem))]
public sealed class ClumsyStatusTest : InteractionTest
{
    private sealed class CatchListenerSystem : TestListenerSystem<CatchAttemptEvent>;
    private sealed class ClimbListenerSystem : TestListenerSystem<SelfBeforeClimbEvent>;
    private sealed class DefibListenerSystem : TestListenerSystem<SelfBeforeDefibrillatorZapsEvent>;
    private sealed class GunListenerSystem : TestListenerSystem<SelfBeforeGunShotEvent>;
    private sealed class InjectListenerSystem : TestListenerSystem<SelfBeforeInjectEvent>;

    [SidedDependency(Side.Server)] private readonly ClimbSystem _sClimbSystem = default!;
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _sStatusSystem = default!;
    [SidedDependency(Side.Server)] private readonly ThrowingSystem _sThrowSystem = default!;

    [Test, Description("Test that a ball thrown at someone clumsy is not caught.")]
    public async Task TestClumsyCatch()
    {
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, ClumsyStatusAll100);
        });

        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, ClumsyStatusAll100), Is.True);

        await Server.WaitPost(() =>
        {
            var location = SEntMan.EnsureComponent<TransformComponent>(SPlayer).Coordinates;
            var ball = SSpawnAtPosition(BallProto, location);

            _sThrowSystem.TryThrow(ball, Vector2.Zero); // Direction doesn't matter because it spawned on top of the player
        });

        foreach (var ev in GetEvents<CatchAttemptEvent>(SPlayer))
        {
            Assert.That(ev.Cancelled, Is.True, "Clumsy mob didn't cancel a catch event.");
        }
    }

    [Test, Description("Test that a clumsy mob shocks themselves with a defibrillator.")]
    public async Task TestClumsyDefib()
    {
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, ClumsyStatusAll100);
        });

        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, ClumsyStatusAll100), Is.True);

        await SpawnTarget(TargetProto);

        await PlaceInHands(DefibProto);
        await Interact();
        await AwaitDoAfters();

        foreach (var ev in GetEvents<SelfBeforeDefibrillatorZapsEvent>(SPlayer))
        {
            Assert.That(ev.DefibTarget, Is.EqualTo(ev.EntityUsingDefib), "Clumsy mob didn't target themself with a defibrillator.");
        }
    }

    [Test, Description("Test that a gun explodes in a clumsy mob's face and stuns them.")]
    public async Task TestClumsyGun()
    {
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<MobStateComponent>(SPlayer); // So that we are a valid target for SharedStunSystem.StunId
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);

            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, ClumsyStatusAll100);
        });

        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, ClumsyStatusAll100), Is.True);

        await SpawnTarget(TargetProto);

        await PlaceInHands(GunProto);
        await UseInHand(); // Chamber the gun
        await RunSeconds(0.5f); // Guns have a cooldown when picking them up.
        await AttemptShoot(Target);

        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, SharedStunSystem.StunId), Is.True, "Clumsy mob wasn't stunned from shooting a gun.");
        foreach (var ev in GetEvents<SelfBeforeGunShotEvent>(SPlayer))
        {
            Assert.That(ev.Cancelled, Is.True, "Clumsy mob didn't cancel gun shoot event.");
        }
    }

    [Test, Description("Test that a clumsy mob injects themselves with a syringe.")]
    public async Task TestClumsyInject()
    {
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, ClumsyStatusAll100);
        });

        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, ClumsyStatusAll100), Is.True);

        await PlaceInHands(SyringeProto);
        await SpawnTarget(TargetProto);

        await Interact();
        await AwaitDoAfters();

        foreach (var ev in GetEvents<SelfBeforeInjectEvent>(SPlayer))
        {
            Assert.That(ev.EntityUsingInjector, Is.EqualTo(ev.TargetGettingInjected), "Clumsy mob didn't target themself with an injector.");
        }
    }

    [Test, Description("Test that a clumsy mob fails to climb and stuns themselves.")]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GameTableBonk), true)]
    public async Task TestClumsyClimb()
    {
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<ClimbingComponent>(SPlayer); // So that we can climb tables
            SEntMan.EnsureComponent<MobStateComponent>(SPlayer); // So that we are a valid target for SharedStunSystem.StunId
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);

            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, ClumsyStatusAll100);
        });

        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, ClumsyStatusAll100), Is.True);

        await Server.WaitPost(() =>
        {
            var location = SEntMan.EnsureComponent<TransformComponent>(SPlayer).Coordinates;
            var table = SSpawnAtPosition(TableProto, location);

            SEntMan.EnsureComponent<ClimbingComponent>(SPlayer);
            _sClimbSystem.TryClimb(SPlayer, SPlayer, table, out _);
        });

        await AwaitDoAfters();

        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, SharedStunSystem.StunId), Is.True, "Clumsy mob wasn't stunned climbing a table.");
        foreach (var ev in GetEvents<SelfBeforeClimbEvent>(SPlayer))
        {
            Assert.That(ev.Cancelled, Is.True, "Clumsy mob didn't cancel climb event.");
        }
    }
}
