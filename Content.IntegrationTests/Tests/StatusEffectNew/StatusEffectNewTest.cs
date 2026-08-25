using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Helpers;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;
using static Content.IntegrationTests.Tests.StatusEffectNew.StatusEffectNewTestPrototypes;

namespace Content.IntegrationTests.Tests.StatusEffectNew;

[TestFixture]
[TestOf(typeof(StatusEffectsSystem))]
public sealed class StatusEffectNewTest : InteractionTest
{
    
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _sStatusSystem = default!;
    [SidedDependency(Side.Server)] private readonly IGameTiming _gameTiming = default!;
    
    [Test, Description("Test that the durations of status effects can be set")]
    public async Task TestDurations()
    {

        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA);
            curTime = _gameTiming.CurTime;
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, OneMinute);
        });

        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA);
        
        Assert.That(timeA.EndEffectTime, Is.Null, "Status effect A did not have an end time of null");
        
        _sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB);
        Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + OneMinute), "Status effect B did not have an end time of one minute from start time");
    }
    
    [Test, Description("Test that the delays of status effects can be set")]
    public async Task TestDelays()
    {
        var curTimeA = TimeSpan.Zero;
        var curTimeB = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, duration: OneMinute);
            curTimeA = _gameTiming.CurTime;
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, duration: OneMinute, delay: OneMinute);
            curTimeB = _gameTiming.CurTime;
        });

        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA);
        
        Assert.That(timeA.StartEffectTime, Is.EqualTo(curTimeA), "Status effect A did not start immediately");
        
        _sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB);
        Assert.That(timeB.StartEffectTime, Is.EqualTo(curTimeB + OneMinute), "Status effect B is not going to start after a delay of one minute");
    }

    [Test, Description("Test that the expected status effects are present on the targeted mobs, and that expired status effects are removed from the mobs")]
    public async Task TestExpectedStatusEffects()
    {
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, TenTicks);
        });
        
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out var uidStatusA), Is.True, "Status effect A was not found on the player");
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusB, out var uidStatusB), Is.True, "Status effect B was not found on the player");
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusC, out var uidStatusC), Is.False, "Status effect C was found on the player despite never being given");

        await Server.WaitRunTicks(10);
        
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out var uidStatusA_2), Is.True, "Status effect A was not found on the player after 10 ticks");
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusB, out var uidStatusB_2), Is.False, "Status effect B was still on the player after it should have expired");
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusC, out var uidStatusC_2), Is.False, "Status effect C was found on the player despite never being given");

    }
    
    [Test, Description("Test that status effects can manually be removed")]
    public async Task TestManuallyRemoveStatusEffect()
    {
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA);
        });
        
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out var uidStatusA), Is.True, "Status effect A was not found on the player");
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusB, out var uidStatusB), Is.False, "Status effect B was found on the player despite never being given");

        Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusA), Is.True, "TryRemoveStatusEffect for Status A failed!");
        Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusB), Is.False, "TryRemoveStatusEffect for Status B (not on the player) somehow succeeded despite this effect not being on the player!?");
        
        await Server.WaitRunTicks(1); // have to wait for queued deletion of status effects
        
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out var uidStatusA_2), Is.False, "Status effect A was still on the player after being removed!");

    }
}