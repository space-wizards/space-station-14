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
    public async Task TestSetDurations()
    {

        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA);
            curTime = _gameTiming.CurTime;
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, OneMinute);
            
            // checking to ensure that a negative duration gets rejected
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, MinusTenTicks);
        });

        
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player");
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
        
        Assert.That(timeA.EndEffectTime, Is.Null, "Status effect A did not have an end time of null");
        
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.True, "Status effect B was not found on the player");
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
        Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + OneMinute), "Status effect B did not have an end time of one minute from start time");
        
        
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.False, "Status effect C was found despite having a negative duration");
    }
    
    [Test, Description("Test that the delays of status effects can be set")]
    public async Task TestSetDelays()
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

        
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
        Assert.That(timeA.StartEffectTime, Is.EqualTo(curTimeA), "Status effect A did not start immediately");
        
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
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
        
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player");
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.True, "Status effect B was not found on the player");
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.False, "Status effect C was found on the player despite never being given");

        await Server.WaitRunTicks(10);
        
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player after 10 ticks");
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.False, "Status effect B was still on the player after it should have expired");
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.False, "Status effect C was found on the player despite never being given");

    }
    
    [Test, Description("Test that status effects can manually be removed")]
    public async Task TestManuallyRemoveStatusEffect()
    {
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA);
        });
        
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player");
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.False, "Status effect B was found on the player despite never being given");

        Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusA), Is.True, "TryRemoveStatusEffect for Status A failed!");
        Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusB), Is.False, "TryRemoveStatusEffect for Status B (not on the player) somehow succeeded despite this effect not being on the player!?");
        
        await Server.WaitRunTicks(1); // have to wait for queued deletion of status effects
        
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.False, "Status effect A was still on the player after being removed!");
    }

    [Test, Description("Testing TryAddTime and TryRemoveTime to add and subtract effect duration")]
    public async Task TestAddRemoveEffectTime()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = _gameTiming.CurTime;
        });
        
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        
        Assert.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected end time of 10 ticks after current time");
        
        Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, TenTicks), Is.True, "TryAddTime adding ten ticks to duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTen);
        Assert.That(timePlusTen.EndEffectTime, Is.EqualTo(curTime + (TenTicks * 2)), "Status effect A did not have expected updated end time of 20 ticks after current time");
        
        Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusA, TenTicks), Is.True, "TryRemoveTime removing ten ticks from duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTenMinusTen);
        Assert.That(timePlusTenMinusTen.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected updated end time of 10 ticks after current time");
        
    }
    
    [Test, Description("Testing TryAddTime and TryRemoveTime with null args")]
    public async Task TestAddRemoveEffectTime_Null()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = _gameTiming.CurTime;
        });
        
        
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        Assert.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected end time of 10 ticks after current time");
        
        Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, null), Is.True, "TryAddTime setting duration to null did not return true");
        
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePermanent);
        Assert.That(timePermanent.EndEffectTime, Is.Null, "TryAddTime setting to null did not set the duration to null");
        Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusA,  null), Is.True, "TryRemoveTime with param of null did not return true");
        
        await Server.WaitRunTicks(1); // status effect removal is queued to next tick.
        
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out _),  Is.False, "TryRemoveTime set to null did not remove the status effect (TryRemoveTime is supposed to remove status effect if null is given)");
    }
    
    [Test, Description("Testing TryAddStatusEffectDuration to adjust status effect duration.")]
    public async Task TestAddEffectTime_TryAddStatusEffectDuration()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = _gameTiming.CurTime;
        });
        
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        
        Assert.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected end time of 10 ticks after current time");
        
        Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, TenTicks), Is.True, "TryAddStatusEffectDuration adding ten ticks to duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTen);
        Assert.That(timePlusTen.EndEffectTime, Is.EqualTo(curTime + (TenTicks * 2)), "Status effect A did not have expected updated end time of 20 ticks after current time");
        
        Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, -TenTicks), Is.True, "TryAddStatusEffectDuration subtracting ten ticks from duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTenMinusTen);
        Assert.That(timePlusTenMinusTen.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected updated end time of 10 ticks after current time");
    }
    
    [Test, Description("Testing TryAddStatusEffectDuration to adjust status effect duration with null duration args")]
    public async Task TestAddEffectTime_TryAddStatusEffectDuration_Null()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = _gameTiming.CurTime;
            
        });
        
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        Assert.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected end time of 10 ticks after current time");
        Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, null), Is.True, "TryAddStatusEffectDuration setting duration to null did not return true");
        
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePermanent);
        Assert.That(timePermanent.EndEffectTime, Is.Null, "TryAddStatusEffectDuration setting to null did not set the duration to null");
    }
    
    
    [Test, Description("Testing TrySetDuration to adjust status effect duration.")]
    public async Task TestAddEffectTime_TrySetDuration()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = _gameTiming.CurTime;
        });
        
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        
        Assert.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected end time of 10 ticks after current time");
        
        var twentyTicks = TenTicks * 2;
        
        Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, twentyTicks), Is.True, "TrySetDuration setting to 20 ticks did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTen);
        Assert.That(timePlusTen.EndEffectTime, Is.EqualTo(curTime + twentyTicks), "Status effect A did not have expected updated end time of 20 ticks after current time");
        
        Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, TenTicks), Is.True, "TrySetDuration setting to 10 ticks did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTenMinusTen);
        Assert.That(timePlusTenMinusTen.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected updated end time of 10 ticks after current time");
        
    }
    
    [Test, Description("Testing TrySetDuration to adjust status effect duration to null")]
    public async Task TestAddEffectTime_TrySetDuration_Null()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<TestListenerComponent>(SPlayer);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = _gameTiming.CurTime;
        });
        
        Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        Assert.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected end time of 10 ticks after current time");
        Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, null), Is.True, "TrySetDuration setting to null did not return true");
        
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePermanent);
        Assert.That(timePermanent.EndEffectTime, Is.Null, "TrySetDuration setting to null did not set the duration to null");
    }
}