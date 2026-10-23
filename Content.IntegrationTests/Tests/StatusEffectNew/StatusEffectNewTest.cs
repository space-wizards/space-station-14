using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using static Content.IntegrationTests.Tests.StatusEffectNew.StatusEffectNewTestPrototypes;

namespace Content.IntegrationTests.Tests.StatusEffectNew;

[TestFixture]
[TestOf(typeof(StatusEffectsSystem))]
public sealed class StatusEffectNewTest : InteractionTest
{
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _sStatusSystem = default!;

    // Timespan offsets for use in these tests, should line up with the timing of the server's tickrate.
    /// <summary>
    /// baseline difference between the following tick rates (saves having to hardcode this everywhere)
    /// </summary>
    private const int Ten = 10;
    /// <summary>
    /// calculates duration of ten game ticks as a timespan, based on current tickrate.
    /// </summary>
    private TimeSpan TenTicks => SGameTiming.TickPeriod * Ten;
    /// <summary>
    /// duration of twenty game ticks as a timespan
    /// </summary>
    private TimeSpan TwentyTicks => TenTicks * 2;
    /// <summary>
    /// duration of thirty game ticks as a timespan
    /// </summary>
    private TimeSpan ThirtyTicks => TenTicks * 3;

    [Test, Description("Test that valid status effects can be added with TrySetStatusEffectDuration (and that invalid ones are rejected)")]
    public async Task TestAddNewStatusEffects_TrySetStatusEffectDuration()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA), Is.True, "adding an indefinite status effect via TrySetStatusEffectDuration failed!");
                curTime = STiming.CurTime;
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, TenTicks), Is.True, "adding a status effect with duration via TrySetStatusEffectDuration failed!");
                // zero duration should be rejected
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero), Is.False, "adding a status effect with zero duration via TrySetStatusEffectDuration didn't get rejected!");
                // checking to ensure that a negative duration gets rejected
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusD, -TenTicks), Is.False, "adding a status effect with negative duration via TrySetStatusEffectDuration didn't get rejected!");
            }
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player");
            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
            Assert.That(timeA.EndEffectTime, Is.Null, "Status effect A did not have an end time of null");

            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.True, "Status effect B was not found on the player");
            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
            Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect B did not have an end time of 10 game ticks after start time");

            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.False, "Status effect C was found despite having zero duration");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusD), Is.False, "Status effect D was found despite having a negative duration");
        }
    }

    [Test, Description("Test that valid status effects can be added with TrySetStatusEffectDuration (and that invalid ones are rejected)")]
    public async Task TestAddNewStatusEffects_TryAddStatusEffectDuration()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, null), Is.True, "adding an indefinite status effect via TryAddStatusEffectDuration failed!");
                curTime = STiming.CurTime;
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusB, TenTicks), Is.True, "adding a status effect with duration via TryAddStatusEffectDuration failed!");
                // zero duration should be rejected
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero), Is.False, "adding a status effect with zero duration via TryAddStatusEffectDuration didn't get rejected!");
                // checking to ensure that a negative duration gets rejected
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusD, -TenTicks), Is.False, "adding a status effect with negative duration via TryAddStatusEffectDuration didn't get rejected!");
            }
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player");
            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
            Assert.That(timeA.EndEffectTime, Is.Null, "Status effect A did not have an end time of null");

            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.True, "Status effect B was not found on the player");
            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
            Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect B did not have an end time of 10 game ticks after start time");

            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.False, "Status effect C was found despite having zero duration");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusD), Is.False, "Status effect D was found despite having a negative duration");
        }
    }
    
    [Test, Description("Test that the delays of status effects can be set")]
    public async Task TestSetDelays()
    {
        var effectQuery = SEntMan.GetEntityQuery<StatusEffectComponent>();
        var curTimeA = TimeSpan.Zero;
        var curTimeB = TimeSpan.Zero;
        await Server.WaitAssertion(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, duration: TwentyTicks); // should end 20 ticks from curtime
            curTimeA = STiming.CurTime;
            Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, duration: TenTicks, delay: TenTicks), Is.True, "CallingTrySetStatusEffectDuration to add effect with delay returned false!"); 
            // should start 10 ticks from curtime and end 20 ticks from curtime
            curTimeB = STiming.CurTime;
        });

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
        Assume.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out var idStatusA),  Is.True, "Status effect A was not found on the player");
        Assume.That(effectQuery.TryComp(idStatusA, out var compStatusA), Is.True, "Status effect A component was not found on the player");

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
        Assume.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusB, out var idStatusB),  Is.True, "Status effect B was not found on the player");
        Assume.That(effectQuery.TryComp(idStatusB, out var compStatusB), Is.True, "Status effect B component was not found on the player");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(timeA.StartEffectTime, Is.EqualTo(curTimeA).Within(SGameTiming.TickPeriod), "Status effect A did not start immediately");
            Assert.That(timeA.EndEffectTime, Is.EqualTo(curTimeA + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect A (immediate, 20 tick duration) will not end twenty ticks from now"); 
            Assert.That(compStatusA!.Applied, Is.True, "Status effect A was not applied on the player, despite not having a delay");

            Assert.That(timeB.StartEffectTime, Is.EqualTo(curTimeB + TenTicks).Within(SGameTiming.TickPeriod), "Status effect B is not going to start after a delay of ten ticks");
            Assert.That(timeB.EndEffectTime, Is.EqualTo(curTimeB + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect B (10 tick delay, 10 tick duration) will not end 20 ticks from now");
            Assert.That(compStatusB!.Applied, Is.False, "Status effect B was applied on the player, despite delay not being over.");
        }

        // wait for effect B's delay to end
        await Pair.RunUntilSynced();
        await Pair.RunTicksSync(Ten);

        _sStatusSystem.TryGetTime(SPlayer, StatusB, out var latterTimeB);
        Assert.That(compStatusB!.Applied, Is.True, $"Status effect B had not been applied to the player, despite its delay being over. (cur time: {STiming.CurTime}, b delay: {latterTimeB.StartEffectTime?.ToString() ?? "dunno" })");
    }

    [Test, Description("Testing setting a negative delay for a status effect. TODO is this implementation desired behaviour?")]
    public async Task TestSetNegativeDelay()
    {
        // This tests the current implementation of setting negative delays.
        // TODO Is this actually desired behaviour?
        //  Current implementation accepts negative effect delays, treats the effect as having started _x_ amount of time ago.
        //  If method implementation changes to reject negative delays/treat them as zero/etc, please update this test.
        var effectQuery = SEntMan.GetEntityQuery<StatusEffectComponent>();
        var curTimeA = TimeSpan.Zero;
        await Server.WaitAssertion(() =>
        {
            // lasts 20 ticks, but started 10 ticks ago
            Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, duration: TwentyTicks, delay: -TenTicks), Is.True, "Setting status effect with negative delay did not return true");
            curTimeA = STiming.CurTime;
        });

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
        Assume.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out var idStatusA), Is.True, "_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out idStatusA) somehow failed.");
        Assume.That(effectQuery.TryComp(idStatusA, out var compStatusA), Is.True, "effectQuery.TryComp(idStatusA, out compStatusA) somehow failed.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(timeA.StartEffectTime, Is.EqualTo(curTimeA - TenTicks).Within(SGameTiming.TickPeriod), "Status effect A is not going to start after a delay of -ten ticks (See TODO comments in test method for more info)");
            Assert.That(timeA.EndEffectTime, Is.EqualTo(curTimeA - TenTicks + (TwentyTicks)).Within(SGameTiming.TickPeriod), "Status effect A (-10 tick delay, 20 tick duration) will not end 10 ticks from now");
            Assert.That(compStatusA!.Applied, Is.True, "Status effect A was not applied on the player despite having a negative delay.");
        }
    }

    [Test, Description("Test that the expected status effects are present on the targeted mobs, and that expired status effects are removed from the mobs")]
    public async Task TestExpectedStatusEffects()
    {
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA);
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, TenTicks);
        });

        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player");
        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.True, "Status effect B was not found on the player");
        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.False, "Status effect C was found on the player despite never being given");

        // exact duration of effect, avoids hardcoded values I guess
        await Pair.RunUntilSynced();
        await Pair.RunTicksSync(Ten);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player after 10 ticks, despite having indefinite duration");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.False, "Status effect B was still on the player after it should have expired");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.False, "Status effect C was found on the player despite never being given");
        }
    }
    
    [Test, Description("Test that status effects can manually be removed")]
    public async Task TestManuallyRemoveStatusEffect()
    {
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA);
        });

        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player");
        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.False, "Status effect B was found on the player despite never being given");

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusA), Is.True, "TryRemoveStatusEffect for Status A failed!");
                Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusB), Is.False, "TryRemoveStatusEffect for Status B (not on the player) somehow succeeded despite this effect not being on the player!?");
            }
        });
        await Server.WaitRunTicks(1); // have to wait for queued deletion of status effects
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.False, "Status effect A was still on the player after being removed!");
    }

    [Test, Description("Testing TryAddTime and TryRemoveTime to add and subtract effect duration")]
    public async Task TestAddRemoveEffectTime()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = STiming.CurTime;
        });

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        Assume.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected end time of 10 ticks after current time");

        Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, TwentyTicks), Is.True, "TryAddTime adding 20 ticks to duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTenPlusTwenty);
        Assert.That(timePlusTenPlusTwenty.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected updated end time of 30 ticks after current time (was 10, 20 added)");

        Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusA, TenTicks), Is.True, "TryRemoveTime removing ten ticks from duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTenPlusTwentyMinusTen);
        Assert.That(timePlusTenPlusTwentyMinusTen.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected updated end time of 20 ticks after current time (was 30, 10 removed)");
    }
    
    [Test, Description("Testing TryAddTime with null args (should make status effect last indefinitely)")]
    public async Task TestAddEffectTime_Null()
    {
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
        });

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        Assume.That(time.EndEffectTime, Is.Not.Null, "Can't properly test setting duration to null when it's already null...");

        Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, null), Is.True, "TryAddTime setting duration to null did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePermanent);
        Assert.That(timePermanent.EndEffectTime, Is.Null, "TryAddTime setting to null did not set the duration to null"); 
    }
    
    [Test, Description("Testing TryRemoveTime with null args (should remove status effect)")]
    public async Task TestRemoveEffectTime_Null()
    {
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
        });

        Assume.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player (can't really test removing time from it if it's not present...)");
        Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusA,  null), Is.True, "TryRemoveTime with param of null did not return true");
        await Pair.RunUntilSynced();
        await Pair.RunTicksSync(1); // status effect removal is queued to next tick.
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA),  Is.False, "TryRemoveTime set to null did not remove the status effect (TryRemoveTime is supposed to remove status effect if null is given)");
    }
    
    [Test, Description("Testing TryAddStatusEffectDuration to adjust status effect duration.")]
    public async Task TestAddEffectTime_TryAddStatusEffectDuration()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = STiming.CurTime;
        });

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        Assume.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected end time of 10 ticks after current time");

        Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, TwentyTicks), Is.True, "TryAddStatusEffectDuration adding 20 ticks to duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTenPlusTwenty);
        Assert.That(timePlusTenPlusTwenty.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected updated end time of 30 ticks after current time (was 10, 20 added)");

        Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, -TenTicks), Is.True, "TryAddStatusEffectDuration removing ten ticks from duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTenPlusTwentyMinusTen);
        Assert.That(timePlusTenPlusTwentyMinusTen.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected updated end time of 20 ticks after current time (was 30, 10 removed)");
    }
    
    [Test, Description("Testing TryAddStatusEffectDuration to adjust status effect duration with null duration args")]
    public async Task TestAddEffectTime_TryAddStatusEffectDuration_Null()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = STiming.CurTime;
        });

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        Assume.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks), "Status effect A did not have expected end time of 10 ticks after current time");

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
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = STiming.CurTime;
        });

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        Assume.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected end time of 10 ticks after current time");

        Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, ThirtyTicks), Is.True, "TrySetDuration setting to 30 ticks did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusThirty);
        Assert.That(timePlusThirty.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected updated end time of 30 ticks after current time");

        Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, TwentyTicks), Is.True, "TrySetDuration setting to 20 ticks did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTwenty);
        Assert.That(timePlusTwenty.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected updated end time of 20 ticks after current time");
    }
    
    [Test, Description("Testing TrySetDuration to adjust status effect duration to null")]
    public async Task TestAddEffectTime_TrySetDuration_Null()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks);
            curTime = STiming.CurTime;
        });

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time), Is.True, "Could not get time info for effect A");
        Assume.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected end time of 10 ticks after current time");

        Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, null), Is.True, "TrySetDuration setting to null did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePermanent);
        Assert.That(timePermanent.EndEffectTime, Is.Null, "TrySetDuration setting to null did not set the duration to null");
    }

    [Test, Description("Testing reducing delays to status effects")]
    public async Task TestReduceStatusEffectDelay()
    {
        var effectQuery = SEntMan.GetEntityQuery<StatusEffectComponent>();
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks, TwentyTicks);
            curTime = STiming.CurTime;
        });

        Assume.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out var idStatusA),  Is.True, "Status effect A was not found on the player");
        Assume.That(effectQuery.TryComp(idStatusA, out var compStatusA), Is.True, "Status effect A component was not found on the player");
        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time1), Is.True, "Could not get time info for effect A");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(time1.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected start time 20 ticks after current time");
            Assert.That(time1.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected end time of 30 ticks after current time");
            Assert.That(compStatusA!.Applied, Is.False, "Status effect A was applied to the player, despite delay not being over");
        }

        Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks, TenTicks), Is.True, "TrySetStatusEffectDuration reducing to 10 tick delay did not return true");
        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time2), Is.True, "Could not get time info for effect A (delay 10, duration 10, end time should be 20)");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(time2.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected start time 10 ticks after current time (delay 10, duration 10, end time should be 20)");
            Assert.That(time2.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected end time of 20 ticks after current time  (delay 10, duration 10, end time should be 20)");
            Assert.That(compStatusA!.Applied, Is.False, "Status effect A was applied to the player, despite reduced delay not being over");
        }

        Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks, null), Is.True, "TrySetStatusEffectDuration setting to null delay did not return true");
        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time3), Is.True, "Could not get time info for effect A (delay null, duration 10, end time should be 10)");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(time3.StartEffectTime, Is.EqualTo(curTime).Within(SGameTiming.TickPeriod), "Status effect A did not have expected start time of immediately (delay null, duration 10, end time should be 10)");
            Assert.That(time3.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected end time of 10 ticks after current time  (delay null, duration 10, end time should be 10)");
        }
        await Server.WaitRunTicks(1); // need to wait for callbacks and such to resolve themselves in order for the "applied" status to update 
        Assert.That(compStatusA!.Applied, Is.True, "Status effect A was not applied to the player after delay was reduced to zero");
    }

    [Test, Description("Testing trying to increase delays to status effects (unsupported(?) operation, weird edge case, see comments)")]
    public async Task TestIncreaseStatusEffectDelay_TrySetStatusEffectDuration_WeirdEdgeCaseConsistencyTest()
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitPost(() =>
        {
            _sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks, TenTicks);
            curTime = STiming.CurTime;
        });

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time1), Is.True, "Could not get time info for effect A");
        Assume.That(time1.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected start time 10 ticks after current time");
        Assume.That(time1.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect A did not have expected end time of 20 ticks after current time");

        // TODO: do we still want this method to return true when a longer (invalid) delay has been given?
        //  * if delay is given, current implementation calculates new end time as (current time + delay + duration)
        //  * underlying `UpdateStatusEffectDelay` method rejects any delays longer than existing delay
        //  * however, this isn't factored in when calculating new effect end time - diff between current delay and desired delay is added to duration.
        //  is this intended behaviour?
        //  this test has been written based on current implementation (in case any systems rely on this weird behaviour), please adjust if implementation changes 
        Assume.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks, TwentyTicks), Is.True, "Weird TrySetStatusEffectDuration - should(?) return true in this edge case - see TODO in test method.");

        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time2), Is.True, "Could not get time info for effect A after trying to increase delay");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(time2.StartEffectTime, Is.EqualTo(time1.StartEffectTime).Within(SGameTiming.TickPeriod), "Unsupported attempt at increasing delay of status effect A somehow increased the delay - see TODO in this test method");
            Assert.That(time2.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(SGameTiming.TickPeriod), "Unsupported attempt at increasing delay of status effect A did not postpone effect end time - see TODO in this test method");
        }
    }
}