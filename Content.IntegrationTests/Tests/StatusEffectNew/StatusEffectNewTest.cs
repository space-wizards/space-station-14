using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
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
    /// <summary>
    /// duration of forty game ticks as a timespan
    /// </summary>
    private TimeSpan FortyTicks => TenTicks * 4;
    /// <summary>
    /// duration of fifty game ticks as a timespan
    /// </summary>
    private TimeSpan FiftyTicks => TenTicks * 5;

    /// <summary>
    /// wiggle room for tick-based duration comparisons (5 ticks of leniency, half the gap between the other timespans)
    /// </summary>
    private TimeSpan WiggleRoom => SGameTiming.TickPeriod * (int)(Ten / 2);

    #region Add new status effects (no delay)
    
    /// <summary>
    /// Helper method to test the various methods which can be used to add a new status effect to an entity without a delay
    /// (these being TrySetStatusEffectDuration, TryAddStatusEffectDuration, and TryUpdateStatusEffectDuration).
    /// Call this with each method (and the method name) to try applying statuses with them, and ensure they have been applied.
    /// Also makes sure that a status effect with a non-indefinite duration is removed at the appropriate time.
    /// </summary>
    /// <param name="addStatusEffectMethod">The method from StatusEffectsSystem being used to add these status effects.</param>
    /// <param name="methodName">The name of the method we passed to this (for use in error messages)</param>
    private async Task TestAddNewStatusEffects_NoDelay(Func<EntityUid, EntProtoId, TimeSpan?, TimeSpan?, bool> addStatusEffectMethod, string methodName)
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                curTime = STiming.CurTime;
                Assert.That(addStatusEffectMethod(SPlayer, StatusA, null, null), Is.True, $"Adding an indefinite status effect via {methodName} failed!");
                Assert.That(addStatusEffectMethod(SPlayer, StatusB, TenTicks, null), Is.True, $"Adding a status effect with duration via {methodName} failed!");
                // zero duration should be rejected
                Assert.That(addStatusEffectMethod(SPlayer, StatusC, TimeSpan.Zero, null), Is.False, $"Adding a status effect with zero duration via {methodName} didn't get rejected!");
                // checking to ensure that a negative duration gets rejected
                Assert.That(addStatusEffectMethod(SPlayer, StatusD, -TenTicks, null), Is.False, $"Adding a status effect with negative duration via {methodName} didn't get rejected!");
            }
        });

        await Pair.RunUntilSynced();

        // check initial state
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player");
            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
            Assert.That(timeA.EndEffectTime, Is.Null, "Status effect A did not have an end time of null");

            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.True, "Status effect B was not found on the player");
            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
            Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Status effect B did not have an end time of 10 game ticks after start time");

            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.False, "Status effect C was found despite having zero duration");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusD), Is.False, "Status effect D was found despite having a negative duration");
        }

        // wait 10 ticks (B duration should be over)
        await Pair.RunUntilSynced();
        await Pair.RunTicksSync(Ten);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A (indefinite) is no longer on the player after 10 ticks!");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.False, "Status effect B (10 tick duration) is still on the player after 10 ticks!");
        }
    }

    [Test, Description("Test that valid status effects can be added with TrySetStatusEffectDuration (and that invalid ones are rejected) - non-delay variant")]
    public async Task TrySetStatusEffectDuration_AddNewEffects_NoDelay()
    {
        await TestAddNewStatusEffects_NoDelay(_sStatusSystem.TrySetStatusEffectDuration, "TrySetStatusEffectDuration");
    }

    [Test, Description("Test that valid status effects can be added with TryAddStatusEffectDuration (and that invalid ones are rejected) - non-delay variant")]
    public async Task TryAddStatusEffectDuration_AddNewEffects_NoDelay()
    {
        await TestAddNewStatusEffects_NoDelay(_sStatusSystem.TryAddStatusEffectDuration, "TryAddStatusEffectDuration");
    }

    [Test, Description("Test that valid status effects can be added with TryUpdateStatusEffectDuration (and that invalid ones are rejected) - non-delay variant")]
    public async Task TryUpdateStatusEffectDuration_AddNewEffects_NoDelay()
    {
        await TestAddNewStatusEffects_NoDelay(_sStatusSystem.TryUpdateStatusEffectDuration, "TryUpdateStatusEffectDuration");
    }

    #endregion Add new status effects (no delay)

    #region Add new status effects (with delay)

    /// <summary>
    /// Helper method to test the various methods which can be used to add a new status effect to an entity with a delay
    /// (these being TrySetStatusEffectDuration, TryAddStatusEffectDuration, and TryUpdateStatusEffectDuration).
    /// Call this with each method (and the method name) to try applying statuses with them, that the delays are correct,
    /// the status is applied correctly after the delay, and that the status effects are removed at the appropriate times
    /// </summary>
    /// <param name="addStatusEffectMethod">The method from StatusEffectsSystem being used to add these status effects.</param>
    /// <param name="methodName">The name of the method we passed to this (for use in error messages)</param>
    private async Task TestAddNewStatusEffects_WithDelay(Func<EntityUid, EntProtoId, TimeSpan?, TimeSpan?, bool> addStatusEffectMethod, string methodName)
    {
        var effectQuery = SEntMan.GetEntityQuery<StatusEffectComponent>();
        var curTime = TimeSpan.Zero;
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                curTime = STiming.CurTime;
                // A should start now, null delay, and end 20 ticks from curtime
                Assert.That(addStatusEffectMethod(SPlayer, StatusA, TwentyTicks, null), Is.True, $"Calling {methodName} to add effect without delay returned false!" );
                // B should start now, zero delay, and end 20 ticks from curtime
                Assert.That(addStatusEffectMethod(SPlayer, StatusB, TwentyTicks, TimeSpan.Zero), Is.True, $"Calling {methodName} to add effect with zero delay returned false!" );
                // C should start 10 ticks from now, and last 10 ticks (and end 20 ticks from curtime)
                Assert.That(addStatusEffectMethod(SPlayer, StatusC, TenTicks, TenTicks), Is.True, $"Calling {methodName} to add effect with delay returned false!"); 
                // D should start 10 ticks ago, and last thirty ticks (ending 20 ticks from curTime) - NEGATIVE DELAY!
                Assert.That(addStatusEffectMethod(SPlayer, StatusD, ThirtyTicks, -TenTicks), Is.True, $"Calling {methodName} to add effect with negative delay returned false!");
            }
        });

        await Pair.RunUntilSynced();

        // these will be needed later.
        StatusEffectComponent compStatusA;
        StatusEffectComponent compStatusB;
        StatusEffectComponent compStatusC;
        StatusEffectComponent compStatusD;
        
        using (Assert.EnterMultipleScope())
        {
            // faffing around obtaining references
            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A [null delay, 20 duration]");
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out var idStatusA),  Is.True, "Status effect A was not found on the player [null delay, 20 duration]");
            Assert.That(effectQuery.TryComp(idStatusA, out compStatusA), Is.True, "Status effect A component was not found on the player [null delay, 20 duration]");

            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B [zero delay, 20 duration]");
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusB, out var idStatusB),  Is.True, "Status effect B was not found on the player [zero delay, 20 duration]");
            Assert.That(effectQuery.TryComp(idStatusB, out compStatusB), Is.True, "Status effect B component was not found on the player [zero delay, 20 duration]");

            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C [10 delay, 10 duration]");
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusC, out var idStatusC),  Is.True, "Status effect C was not found on the player [10 delay, 10 duration]");
            Assert.That(effectQuery.TryComp(idStatusC, out compStatusC), Is.True, "Status effect C component was not found on the player [10 delay, 10 duration]");

            Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D [-10 delay, 30 duration]");
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusD, out var idStatusD),  Is.True, "Status effect D was not found on the player [-10 delay, 30 duration]");
            Assert.That(effectQuery.TryComp(idStatusD, out compStatusD), Is.True, "Status effect D component was not found on the player [-10 delay, 30 duration]");

            // assertions about initial state of the status effects
            Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "Status effect A start time is not currentTime! [null delay, 20 duration]");
            Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Status effect A [null delay, 20 duration] will not end twenty ticks from now"); 
            Assert.That(compStatusA!.Applied, Is.True, "Status effect A was not applied on the player, despite not having a delay [null delay, 20 duration]");

            Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "Status effect B start time is not currentTime! [zero delay, 20 duration]");
            Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Status effect B [zero delay, 20 duration] will not end twenty ticks from now"); 
            Assert.That(compStatusB!.Applied, Is.True, "Status effect B was not applied on the player, despite having a delay of zero! [zero delay, 20 duration]");

            Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Status effect C is not going to start after a delay of ten ticks [10 delay, 10 duration]");
            Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Status effect C (10 tick delay, 10 tick duration) will not end 20 ticks from now [10 delay, 10 duration]");
            Assert.That(compStatusC!.Applied, Is.False, "Status effect C was applied on the player, despite delay not being over. [10 delay, 10 duration]");

            Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "Status effect D start time was not 10 ticks ago! [-10 delay, 30 duration]");
            Assert.That(timeD.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Status effect D will not end twenty ticks from now [-10 delay, 30 duration]"); 
            Assert.That(compStatusD!.Applied, Is.True, "Status effect D was not applied on the player, despite having a negative delay! [-10 delay, 30 duration]");
        }

        // wait 10 ticks (C delay should be over)
        await Pair.RunUntilSynced();
        await Pair.RunTicksSync(Ten);

        // taking a look after 10 ticks - ABD should still be applied, C should now be applied as delay is over.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was no longer on the player after 10 ticks (should have remained until 20 ticks) [null delay, 20 duration]");
            Assert.That(compStatusA!.Applied, Is.True, "Status effect A ceased to be applied to the player earlier than expected (gone at 10 ticks, should have remained until 20) [null delay, 20 duration]");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.True, "Status effect B was no longer on the player after 10 ticks (should have remained until 20 ticks) [zero delay, 20 duration]");
            Assert.That(compStatusB!.Applied, Is.True, "Status effect B ceased to be applied to the player earlier than expected (gone at 10 ticks, should have remained until 20) [zero delay, 20 duration]");

            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.True, "Status effect C was not on the player after 10 ticks (should have remained until 20 ticks) [10 delay, 10 duration]");
            _sStatusSystem.TryGetTime(SPlayer, StatusC, out var latterTimeC);
            Assert.That(compStatusC!.Applied, Is.True, $"Status effect C had not been applied to the player after 10 ticks, despite its 10-tick delay being over. (cur time: {STiming.CurTime}, c delay: {latterTimeC.StartEffectTime?.ToString() ?? "unknown" }) [10 delay, 10 duration]");

            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusD), Is.True, "Status effect D was no longer on the player after 10 ticks (should have remained until 20 ticks) [-10 delay, 30 duration]");
            Assert.That(compStatusD!.Applied, Is.True, "Status effect D ceased to be applied to the player earlier than expected (gone at 10 ticks, should have remained until 20) [-10 delay, 30 duration]");
        }

        // wait 10 more ticks (effects should all be over)
        await Pair.RunUntilSynced();
        await Pair.RunTicksSync(Ten);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.False, "Status effect A [null delay, 20 duration] was still on the player after expected end time!");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.False, "Status effect B [zero delay, 20 duration] was still on the player after expected end time!");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusC), Is.False, "Status effect C [10 delay, 10 duration] was still on the player after expected end time!");
            Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusD), Is.False, "Status effect D [-10 delay, 30 duration] was still on the player after expected end time!");
        }
    }

    [Test, Description("Test that valid status effects with delays can be added with TrySetStatusEffectDuration")]
    public async Task TrySetStatusEffectDuration_AddNewEffects_WithDelay()
    {
        await TestAddNewStatusEffects_WithDelay(_sStatusSystem.TrySetStatusEffectDuration, "TrySetStatusEffectDuration");
    }

    [Test, Description("Test that valid status effects with delays can be added with TryAddStatusEffectDuration")]
    public async Task TryAddStatusEffectDuration_AddNewEffects_WithDelay()
    {
        await TestAddNewStatusEffects_WithDelay(_sStatusSystem.TryAddStatusEffectDuration, "TryAddStatusEffectDuration");
    }

    [Test, Description("Test that valid status effects with delays can be added with TryUpdateStatusEffectDuration")]
    public async Task TryUpdateStatusEffectDuration_AddNewEffects_WithDelay()
    {
        await TestAddNewStatusEffects_WithDelay(_sStatusSystem.TryUpdateStatusEffectDuration, "TryUpdateStatusEffectDuration");
    }

    #endregion Add new status effects (with delay)

    [Test, Description("Test that status effects can manually be removed with TryRemoveStatusEffect")]
    public async Task TryRemoveStatusEffect()
    {
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA), Is.True, "Status effect A could not be applied, unable to perform these tests :(");

                Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.True, "Status effect A was not found on the player");
                Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.False, "Status effect B was found on the player despite never being given");

                Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusA), Is.True, "TryRemoveStatusEffect for Status A failed! (returned false, expected true)");
                Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusB), Is.False, "TryRemoveStatusEffect for Status B (not on the player) somehow succeeded despite this effect not being on the player!? (returned true, expected false)");
            }
        });
        await Pair.RunUntilSynced(); // have to wait for queued deletion of status effects
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.False, "Status effect A was still on the player after being removed!");
    }
    
    /// <summary>
    /// helper method to give SPlayer effects A,B,C,D with identical delay (zero or 20 ticks) and duration (indefinite or 20 ticks)
    /// </summary>
    /// <param name="delay20">If true, will give effects a delay of 20 ticks. If false, gives delay of zero ticks (instant).</param>
    /// <param name="duration20">If true, will give effects a duration of 20 ticks. If false, gives indefinite duration.</param>
    /// <returns>STiming.CurTime as of when the status effects were applied (useful for other assertions later on)</returns>
    private async Task<TimeSpan> SetupStatusesSameLength(bool delay20 = false, bool duration20 = false)
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                curTime = STiming.CurTime;
                TimeSpan? delay = delay20 ? TwentyTicks : null;
                var delayText = delay20 ? "20 ticks" : "instant";
                TimeSpan? duration = duration20 ? TwentyTicks : null;
                var durationText = duration20 ? "20 ticks" : "indefinite";
                var endTimeText = duration20 ? (delay20 ? "40 ticks from now" : "20 ticks from now") : "indefinite";
                IResolveConstraint delayConstraint = Is.EqualTo(curTime + (delay20 ? delay : TimeSpan.Zero)).Within(STiming.TickPeriod);
                IResolveConstraint durationConstraint = duration20 ? Is.EqualTo(curTime + (delay20 ? TwentyTicks : TimeSpan.Zero) + duration).Within(STiming.TickPeriod) : Is.Null;

                SetupStatus(StatusA, delay, delayText, duration, durationText, endTimeText, delayConstraint, durationConstraint);
                SetupStatus(StatusB, delay, delayText, duration, durationText, endTimeText, delayConstraint, durationConstraint);
                SetupStatus(StatusC, delay, delayText, duration, durationText, endTimeText, delayConstraint, durationConstraint);
                SetupStatus(StatusD, delay, delayText, duration, durationText, endTimeText, delayConstraint, durationConstraint);
            }
        });
        await Pair.RunUntilSynced();
        return curTime;
    }

    /// <summary>
    /// Helper method used by <see cref="SetupStatusesSameLength"/> to set up an individual status with a preset delay, duration, and appropriate assertions
    /// </summary>
    private void SetupStatus(EntProtoId statusProto, TimeSpan? delay, string delayText, TimeSpan? duration, string durationText, string endTimeText, IResolveConstraint delayConstraint, IResolveConstraint durationConstraint)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, statusProto, duration, delay), Is.True, $"{statusProto.Id} ({delayText} delay, {durationText} duration) could not be added to player");
            Assert.That(_sStatusSystem.TryGetTime(SPlayer, statusProto, out var statusTime), Is.True, $"Could not get time info for {statusProto.Id}");
            Assert.That(statusTime.StartEffectTime, delayConstraint, $"Delay of {statusProto.Id} is not {delayText}");
            Assert.That(statusTime.EndEffectTime, durationConstraint, $"End time of {statusProto.Id} is not {endTimeText}");
        }
    }
    
    #region TryAddTime and TryRemoveTime

    [Test, Description("Testing TryAddTime to add status effect duration")]
    public async Task TryAddTime()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, TenTicks), Is.True, "Using use TryAddTime to add 10 ticks to finite duration of StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusB, -TenTicks), Is.True, "Using use TryAddTime to add -10 ticks to finite duration of StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Using use TryAddTime to add zero to finite duration of StatusC unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusD, null), Is.True, "Using use TryAddTime to add null to finite duration of StatusD (to make it indefinite) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Adding 10 ticks to 20-tick status effect did not give duration 30");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Adding minus 10 ticks to 20-tick status effect did not give duration 10");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Adding 0 ticks to 20-tick status effect did not give duration 20");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Adding null to 20-tick status effect did not make the effect indefinite");
            }
        });
    }
    
    [Test, Description("Testing TryAddTime to add status effect duration to indefinite status effects")]
    public async Task TryAddTime_Null()
    {
        await SetupStatusesSameLength(delay20:false,duration20:false);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, TenTicks), Is.True, "Using TryAddTime to add 10 ticks to indefinite duration of StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusB, -TenTicks), Is.True, "Using TryAddTime to add -10 ticks (subtract 10) to indefinite duration of StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Using TryAddTime to add zero to indefinite duration of StatusC unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusD, null), Is.True, "Using TryAddTime to add null to indefinite duration of StatusD (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.Null, "Adding 10 ticks to indefinite status effect made the duration non-indefinite");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.Null, "Adding minus 10 ticks to indefinite status effect made the duration non-indefinite");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.Null, "Adding 0 ticks to indefinite status effect made the duration non-indefinite");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Adding null to indefinite status effect made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TryAddTime to add status effect duration to effects with delays")]
    public async Task TryAddTime_Delays()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, TenTicks), Is.True, "Using use TryAddTime to add 10 ticks to 20-delay 20-duration StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusB, -TenTicks), Is.True, "Using use TryAddTime to add -10 ticks to 20-delay 20-duration StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Using use TryAddTime to add zero to 20-delay 20-duration StatusC unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusD, null), Is.True, "Using use TryAddTime to add null to 20-delay 20-duration StatusD (to make it indefinite) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Adding positive duration to status effect A somehow overwrote the delay!");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + FiftyTicks).Within(WiggleRoom), "Adding 10 ticks to 20-delay 20-duration status effect did not give end time 50");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Adding negative duration to status effect B somehow overwrote the delay!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Adding minus 10 ticks to 20-delay 20-duration status effect did not give end time 30");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Adding zero duration to status effect C somehow overwrote the delay!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "Adding 0 ticks to 20-delay 20-duration status effect did not give end time 40");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Adding null duration to status effect D somehow overwrote the delay!");
                Assert.That(timeD.EndEffectTime, Is.Null, "Adding null to 20-delay 20-duration status effect did not make the effect indefinite");
            }
        });
    }

    [Test, Description("Testing TryRemoveTime to remove status effect duration")]
    public async Task TryRemoveTime()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusA, TenTicks), Is.True, "Using TryRemoveTime to subtract 10 ticks from finite duration of StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusB, -TenTicks), Is.True, "Using TryRemoveTime to subtract -10 ticks (add 10) from finite duration of StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Using TryRemoveTime to subtract zero from finite duration of StatusC unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusD, null), Is.True, "Using TryRemoveTime to subtract null from finite StatusD duration (to remove status) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Subtracting 10 ticks from 20-tick status effect did not give duration 10");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Subtracting minus 10 ticks from 20-tick status effect did not give duration 30");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Subtracting 0 ticks from 20-tick status effect did not give duration 20");
            }
        });
        // give the system time to handle the queued removal of status D
        await Pair.RunUntilSynced();
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusD, out _), Is.False, "TryRemoveTime with null was supposed to remove the status effect; it was not removed.");
    }
    
    [Test, Description("Testing TryRemoveTime to remove status effect duration from indefinite durations")]
    public async Task TryRemoveTime_Null()
    {
        await SetupStatusesSameLength(delay20:false,duration20:false);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusA, TenTicks), Is.True, "Using TryRemoveTime to subtract 10 ticks from indefinite duration of StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusB, -TenTicks), Is.True, "Using TryRemoveTime to subtract -10 ticks (add 10) from indefinite duration of StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Using TryRemoveTime to subtract zero from indefinite duration of StatusC unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusD, null), Is.True, "Using TryRemoveTime to subtract null from indefinite StatusD (to remove status) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.Null, "Subtracting 10 ticks from indefinite status effect made the duration non-indefinite");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.Null, "Subtracting minus 10 ticks from indefinite status effect made the duration non-indefinite");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.Null, "Subtracting 0 ticks from indefinite status effect made the duration non-indefinite");
            }
        });
        // give the system time to handle the queued removal of status D
        await Pair.RunUntilSynced();
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusD, out _), Is.False, "TryRemoveTime with null was supposed to remove the indefinite status effect; it was not removed.");
    }

    [Test, Description("Testing TryRemoveTime to remove status effect duration from effects with delays")]
    public async Task TryRemoveTime_Delays()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusA, TenTicks), Is.True, "Using TryRemoveTime to subtract 10 ticks from 20-delay 20-duration StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusB, -TenTicks), Is.True, "Using TryRemoveTime to subtract -10 ticks (add 10) from 20-delay 20-duration StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Using TryRemoveTime to subtract zero from 20-delay 20-duration StatusC unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusD, null), Is.True, "Using TryRemoveTime to subtract null from 20-delay 20-duration StatusD (to remove status) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Subtracting positive duration from status effect A somehow overwrote the delay!");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Subtracting 10 ticks from 20-delay 20-duration status effect did not give end time 30");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Subtracting negative duration from status effect B somehow overwrote the delay!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FiftyTicks).Within(WiggleRoom), "Subtracting minus 10 ticks from 20-delay 20-duration status effect did not give end time 50");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Subtracting zero duration from status effect C somehow overwrote the delay!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "Subtracting 0 ticks from 20-delay 20-duration status effect did not give end time 40");
            }
        });
        // give the system time to handle the queued removal of status D
        await Pair.RunUntilSynced();
        Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusD, out _), Is.False, "TryRemoveTime with null was supposed to remove the status effect; it was not removed.");
    }

    [Test, Description("This is what happens if you use TryAddTime and TryRemoveTime to edit status effects which aren't on the player")]
    public async Task TryAddTime_TryRemoveTime_NonexistentStatuses()
    {
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, TenTicks), Is.False, "Using TryAddTime to add ticks to nonexistent status effect somehow returned true");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusB, null), Is.False, "Using TryAddTime to make nonexistent status effect last indefinitely (by passing null) somehow returned true");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusC, TenTicks), Is.False, "Using TryRemoveTime to remove ticks from nonexistent status effect somehow returned true");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusD, null), Is.False, "Using TryRemoveTime to remove nonexistent status effect (by passing null) somehow returned true");

                Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out _), Is.False, "TryAddTime somehow added a new status effect (by adding 10 ticks)");
                Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusB, out _), Is.False, "TryAddTime somehow added a new status effect (by making it indefinite via passing null)");
                Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusC, out _), Is.False, "TryRemoveTime somehow added a new status effect (by removing 10 ticks)");
                Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusD, out _), Is.False, "TryRemoveTime somehow added a new status effect (by passing null in an attempt to remove it)");
            }
        });
    }

    [Test, Description("This is what happens if you use TryAddTime and TryRemoveTime to remove all duration of a status effect/make it negative (setting end time in the past)")]
    public async Task TryAddTime_TryRemoveTime_RemoveAllDuration()
    {
        await SetupStatusesSameLength(delay20:false,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                // using TryAddTime and TryRemoveTime to reduce remaining effect durations to zero/negative
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, -TwentyTicks), Is.True, "Using TryAddTime to add -20 ticks (subtract 20) to 20-tick duration of StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusB, -ThirtyTicks), Is.True, "Using TryAddTime to add -30 ticks (subtract 30) to 20-tick duration of StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusC, TwentyTicks), Is.True, "Using TryRemoveTime to subtract 20 ticks from 20-tick duration of StatusC unexpectedly returned false");
                Assert.That(_sStatusSystem.TryRemoveTime(SPlayer, StatusD, ThirtyTicks), Is.True, "Using TryRemoveTime to subtract 30 ticks from 20-tick duration of StatusD unexpectedly returned false");
            }
        });
        // give the system time to handle the queued removal of statuses
        await Pair.RunUntilSynced();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusA, out _), Is.False, "TryAddTime to set remaining duration of StatusA to zero did not result in status getting removed.");
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusB, out _), Is.False, "TryAddTime to set remaining duration of StatusB to negative did not result in status getting removed.");
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusC, out _), Is.False, "TryRemoveTime to set remaining duration of StatusC to zero did not result in status getting removed.");
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusD, out _), Is.False, "TryRemoveTime to set remaining duration of StatusD to zero did not result in status getting removed.");
        }
    }

    #endregion TryAddTime and TryRemoveTime
    
    #region TrySetDuration

    [Test, Description("Attempting to use TrySetDuration to add a new status effect (it should fail)")]
    public async Task TrySetDuration_AttemptAddingStatus()
    {
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, TenTicks), Is.False, "Trying to add a new status effect with finite length somehow returned true (it should fail)");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusB, null), Is.False, "Trying to add a new status effect with indefinite length somehow returned true (it should fail)");

                Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.False, "Status effect A was found on the player (it should not have been added)");
                Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusB), Is.False, "Status effect B was found on the player (it should not have been added)");
            }
        });
    }
    
    [Test, Description("Using TrySetDuration to adjust the duration of status effects without any delays attached to them")]
    public async Task TrySetDuration_NoDelay()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                // using TrySetDuration to set durations
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, TenTicks), Is.True, "Using TrySetDuration to set duration to 10 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusB, TimeSpan.Zero), Is.True, "Using TrySetDuration to set duration to 0 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusC, -TenTicks), Is.True, "Using TrySetDuration to set duration to -10 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusD, null), Is.True, "Using TrySetDuration to set duration to null (indefinite) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Using TrySetDuration to set duration to 10 ticks did not set duration to 10 ticks.");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Using TrySetDuration to set duration to null (indefinite) did not set duration to null.");
            }
        });
        // give the system time to handle the expected queued removal of statuses
        await Pair.RunUntilSynced();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusB, out _), Is.False, "TrySetDuration to set remaining duration of StatusB to negative did not result in status getting removed.");
            Assert.That(_sStatusSystem.TryGetStatusEffect(SPlayer, StatusC, out _), Is.False, "TrySetDuration to set remaining duration of StatusC to zero did not result in status getting removed.");
        }
    }

    [Test, Description("Testing TrySetDuration to adjust status effect duration to indefinite status effects")]
    public async Task TrySetDuration_Null()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:false);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, TenTicks), Is.True, "Using TrySetDuration to set duration of indefinite length StatusA to 10 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusB, -TenTicks), Is.True, "Using TrySetDuration to set duration of indefinite length StatusB to -10 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Using TrySetDuration to set duration of indefinite length StatusC to zero unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusD, null), Is.True, "Using TrySetDuration to to set duration of indefinite length StatusB to null (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Could not overwrite duration of indefinite status effect to a positive length (10 ticks)");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "Could not overwrite duration of indefinite status effect to a negative length (-10 ticks)");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "Could not overwrite duration of indefinite status effect to zero");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of indefinite status effect to null (indefinite) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TrySetDuration to adjust status effect duration of effects with delays")]
    public async Task TrySetDuration_WithDelays()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, TenTicks), Is.True, "Using TrySetDuration to set duration of 20 length 20 delay StatusA to 10 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusB, -TenTicks), Is.True, "Using TrySetDuration to set duration of 20 length 20 delay StatusB to -10 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Using TrySetDuration to set duration of 20 length 20 delay StatusC to zero unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusD, null), Is.True, "Using TrySetDuration to to set duration of 20 length 20 delay StatusB to null (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Overwriting duration of status effect A with positive value (10) somehow overwrote the delay!");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Overwriting duration of 20-delay status effect to a positive amount (10) did not result in 30-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Overwriting duration of status effect B with negative value (-10) somehow overwrote the delay!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Overwriting duration of 20-delay status effect to a negative amount (-10) did not result in 10-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Overwriting duration of status effect C with zero somehow overwrote the delay!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Overwriting duration of 20-delay status effect to zero did not result in 20-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Overwriting duration of status effect D with null somehow overwrote the delay!");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to null (indefinite) somehow made the duration non-indefinite");
            }
        });
    }

    #endregion TrySetDuration

    #region TrySetStatusEffectDuration
    
    [Test, Description("Using TrySetStatusEffectDuration to adjust the duration of status effects")]
    public async Task TrySetStatusEffectDuration_NoDelay()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                // using TrySetDuration to set durations
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks), Is.True, "Using TrySetStatusEffectDuration to set duration to 10 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, TimeSpan.Zero), Is.False, "Using TrySetStatusEffectDuration to set duration to 0 ticks unexpectedly returned true");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, -TenTicks), Is.False, "Using TrySetStatusEffectDuration to set duration to -10 ticks unexpectedly returned true");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusD, null), Is.True, "Using TrySetStatusEffectDuration to set duration to null (indefinite) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Using TrySetStatusEffectDuration to set duration to 10 ticks did not set duration to 10 ticks.");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect B duration despite being given an invalid (negative) length");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (zero) length");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Using TrySetStatusEffectDuration to set duration to null (indefinite) did not set duration to null.");
            }
        });
    }

    [Test, Description("Testing TrySetStatusEffectDuration to adjust status effect duration to indefinite status effects")]
    public async Task TrySetStatusEffectDuration_Null()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:false);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks), Is.True, "Using TrySetStatusEffectDuration to set duration of indefinite length StatusA to 10 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, -TenTicks), Is.False, "Using TrySetStatusEffectDuration to set duration of indefinite length StatusB to -10 ticks unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero), Is.False, "Using TrySetStatusEffectDuration to set duration of indefinite length StatusC to zero unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusD, null), Is.True, "Using TrySetStatusEffectDuration to to set duration of indefinite length StatusB to null (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime,Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "TrySetStatusEffectDuration could not overwrite indefinite effect A duration with a finite length of 10 ticks");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.Null, "TrySetStatusEffectDuration unexpectedly overwrote effect B duration despite being given an invalid (negative) length");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.Null, "TrySetStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (zero) length");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of indefinite status effect to (null delay, null duration) (indefinite) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TrySetStatusEffectDuration to adjust status effect duration of effects with delays, whilst leaving the delay argument unspecified")]
    public async Task TrySetStatusEffectDuration_Delays_DefaultBehaviour()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks), Is.True, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusA to 10 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, -TenTicks), Is.False, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusB to -10 ticks unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero), Is.False, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusC to zero unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusD, null), Is.True, "Using TrySetStatusEffectDuration to to set duration of 20 length 20 delay StatusB to null (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TrySetStatusEffectDuration default behaviour with valid positive duration (null delay, 10 duration) somehow didn't overwrite the delay of Status A to zero.");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Overwriting duration of status effect A to a positive amount (null delay, 10 duration) did not result in 10-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect B delay despite the method failing due to being given an invalid (negative) length!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect B duration despite being given an invalid (negative) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect C delay despite the method failing due to being given an invalid (zero) length!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (zero) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TrySetStatusEffectDuration default behaviour with valid (null delay, null duration) duration somehow didn't overwrite the delay of Status D to zero.");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (null delay, null duration) (indefinite) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TrySetStatusEffectDuration to adjust status effect duration of effects with delays, whilst explicitly passing a delay of null")]
    public async Task TrySetStatusEffectDuration_Delays_PassingDelay_Null()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks, null), Is.True, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusA to 10 ticks with null delay unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, -TenTicks, null), Is.False, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusB to -10 ticks with null delay unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero, null), Is.False, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusC to zero with null delay unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusD, null, null), Is.True, "Using TrySetStatusEffectDuration to to set duration of 20 length 20 delay StatusB to null (making it indefinite again) with null delay unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TrySetStatusEffectDuration passing (null delay, 10 duration) somehow didn't overwrite the delay of Status A to zero.");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Overwriting duration of status effect A to a positive amount (null delay, 10 duration) did not result in 10-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect B delay, method should've done nothing due to being given invalid (negative) duration!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect B duration despite being given an invalid (negative) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect C delay, method should've done nothing due to being given invalid (zero) duration!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (zero) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TrySetStatusEffectDuration default behaviour with valid (null delay, null duration) somehow didn't overwrite the delay of Status D to zero.");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (null delay, null duration) (indefinite) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TrySetStatusEffectDuration to adjust durations & decreasing the delays of effects which already have delays")]
    public async Task TrySetStatusEffectDuration_Delays_DecreaseDelays()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks, TenTicks), Is.True, "Using TrySetStatusEffectDuration to set duration of (20 delay, 20 duration) StatusA to (10 delay, 10 duration) unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, -TenTicks, TenTicks), Is.False, "Using TrySetStatusEffectDuration to set duration of (20 delay, 20 duration) StatusB to (10 delay, -10 duration) unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero, TenTicks), Is.False, "Using TrySetStatusEffectDuration to set duration of (20 delay, 20 duration) StatusC to (10 delay, 0 duration) unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusD, null, TenTicks), Is.True, "Using TrySetStatusEffectDuration to to set duration of (20 delay, 20 duration) StatusB to (10 delay, null duration) (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "TrySetStatusEffectDuration passing (10 delay, 10 duration) somehow didn't overwrite the delay of Status A to 10.");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Overwriting duration of status effect A to a positive amount (10 delay, 10 duration) did not result in 20-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect B delay, method should've done nothing due to being given invalid (negative) duration!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect B duration despite being given an invalid (negative) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect C delay, method should've done nothing due to being given invalid (zero) duration!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (zero) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "TrySetStatusEffectDuration passing (10 delay, null duration) somehow didn't overwrite the delay of Status D to 10.");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (10 delay, null duration) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TrySetStatusEffectDuration to adjust durations & decreasing the delays of effects which already have delays")]
    public async Task TrySetStatusEffectDuration_Delays_DecreaseDelays_NegativeDelay_Weird()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, FortyTicks, -TenTicks), Is.True, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusA to -10 delay 40 duration unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, TenTicks, -TenTicks), Is.True, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusA to -10 delay 10 duration unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, TwentyTicks, -TenTicks), Is.True, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusC to -10 delay 20 duration unexpectedly returned false");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusD, null, -TenTicks), Is.True, "Using TrySetStatusEffectDuration to to set duration of 20 length 20 delay StatusB to -10 delay null duration (making it indefinite again) with 10 delay unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "TrySetStatusEffectDuration passing (-10 delay, 40 duration) somehow didn't overwrite the delay of Status A to -10.");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Overwriting duration of status effect A to a positive amount with negative delay (40 duration, -10 delay) did not result in 30-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "TrySetStatusEffectDuration passing (-10 delay, 10 duration) somehow didn't overwrite the delay of Status B to -10.");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "Overwriting status effect B to (-10 delay, 10 duration) somehow didn't set end time to 0");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime  - TenTicks).Within(WiggleRoom), "TrySetStatusEffectDuration passing (-10 delay, 20 duration) somehow didn't overwrite the delay of Status C to -10");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Overwriting status effect C to (-10 delay, 20 duration) somehow didn't set end time to 10");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "TrySetStatusEffectDuration passing (-10 delay, null duration) somehow didn't overwrite the delay of Status D to -10.");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (-10 delay, null duration) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TrySetStatusEffectDuration to increase the delays of status effects - unsupported(?) operation, see remarks in comments.")]
    public async Task TrySetStatusEffectDuration_Delays_IncreaseDelays_WeirdEdgeCase()
    {
        // TODO: do we still want this method to return true when a longer (invalid) delay has been given?
        //  * if delay is given, current implementation calculates new end time as (current time + delay + duration)
        //  * underlying `UpdateStatusEffectDelay` method rejects any delays longer than existing delay
        //  * however, this isn't factored in when calculating new effect end time - diff between current delay and desired delay is added to duration.
        //  is this intended behaviour?
        //  this test has been written based on current implementation (in case any systems rely on this weird behaviour), please adjust if implementation changes 
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, ThirtyTicks, ThirtyTicks), Is.True, "Weird TrySetStatusEffectDuration increase 20 length 20 delay StatusA to 30 delay 30 duration unexpectedly(?) returned false - should(?) return true, weird edge case, see TODO in method");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, -TenTicks, ThirtyTicks), Is.False, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusB to 30 delay -10 duration unexpectedly returned true (invalid duration input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero, ThirtyTicks), Is.False, "Using TrySetStatusEffectDuration to set duration of 20 length 20 delay StatusC to 30 delay 0 duration with 10 delay unexpectedly returned true (invalid duration input, it should do nothing)");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusD, null, ThirtyTicks), Is.True, "Using TrySetStatusEffectDuration to to set duration of 20 length 20 delay StatusB to 30 delay null duration (making it indefinite again) unexpectedly(?) returned false - should(?) return true, weird edge case, see TODO in method");

                // b and c are simple, should've done nothing.
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect B delay, method should've done nothing due to being given invalid (negative) duration!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect B duration despite being given an invalid (negative) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect C delay, method should've done nothing due to being given invalid (zero) duration!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (zero) length!");

                // A. Calculated new end time should be 60 (30delay + 30length = 60). However, as 30delay > 20delay, current implementation is expected to leave delay as 20.
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration passing 30 delay and 30 duration somehow overwrote the delay of 20 delay 20 duration Status A (current implementation should preserve the current shorter delay)");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + (ThirtyTicks * 2)).Within(WiggleRoom), "Overwriting duration of status effect A to a positive amount (30 duration, 30 delay -> end time = 30+30) did not result in 60-tick end time");

                // D. Calculated new end time should be null (given null duration). End time should still be 20 (30delay > 20delay, preserve 20 delay).
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TrySetStatusEffectDuration passing 30 delay and null duration somehow overwrote the delay of 20 delay 20 duration Status D (current implementation should preserve the current shorter delay)");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (delay 30(?) duration null) somehow made the duration non-indefinite");
            }
        });
    }

    #endregion
    
    #region TryUpdateStatusEffectDuration
    
    [Test, Description("Using TryUpdateStatusEffectDuration to decrease the duration of status effects (shouldn't do anything)")]
    public async Task TryUpdateStatusEffectDuration_NoDelay_Shorter()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusA, TenTicks), Is.True, "Using TryUpdateStatusEffectDuration to decrease duration from 20 ticks to 10 ticks unexpectedly returned false (should return true despite doing nothing, duration still >0)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusB, TimeSpan.Zero), Is.False, "Using TryUpdateStatusEffectDuration to set duration from 20 ticks to 0 ticks unexpectedly returned true (should return false, given duration ≤ 0)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusC, -TenTicks), Is.False, "Using TryUpdateStatusEffectDuration to set duration to -10 ticks unexpectedly returned true (should return false, given duration ≤ 0)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusD, TwentyTicks), Is.True, "Using TryUpdateStatusEffectDuration to set duration to twenty ticks (same as current) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect A duration from 20 ticks, despite being given an invalid (shorter) length of 10 ticks");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect B duration from 20 ticks, despite being given an invalid (shorter) length of 0 ticks");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect C duration from 20 ticks, despite being given an invalid (zero) length");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Using TryUpdateStatusEffectDuration to set duration from 20 ticks to 20 ticks somehow gave it a duration which wasn't 20");
            }
        });
    }

    [Test, Description("Using TryUpdateStatusEffectDuration to adjust the duration of status effects")]
    public async Task TryUpdateStatusEffectDuration_NoDelay_Longer()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusA, ThirtyTicks), Is.True, "Using TryUpdateStatusEffectDuration to increase duration to 30 ticks unexpectedly returned false");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusB, null), Is.True, "Using TryUpdateStatusEffectDuration to set duration to null (indefinite) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Using TryUpdateStatusEffectDuration to increase duration to 30 ticks did not set duration to 30 ticks.");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.Null, "Using TryUpdateStatusEffectDuration to set duration to null (indefinite) did not set duration to null.");
            }
        });
    }

    [Test, Description("Testing TryUpdateStatusEffectDuration to adjust status effect duration to indefinite status effects")]
    public async Task TryUpdateStatusEffectDuration_NoDelay_Indefinite()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:false);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusA, TenTicks), Is.True, "Using TryUpdateStatusEffectDuration to set duration of indefinite length StatusA to 10 ticks unexpectedly returned false (valid duration parameter, albeit no effect)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusB, -TenTicks), Is.False, "Using TryUpdateStatusEffectDuration to set duration of indefinite length StatusB to -10 ticks unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero), Is.False, "Using TryUpdateStatusEffectDuration to set duration of indefinite length StatusC to zero unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusD, null), Is.True, "Using TryUpdateStatusEffectDuration to to set duration of indefinite length StatusB to null (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime,Is.Null, "TryUpdateStatusEffectDuration unexpectedly overwrote indefinite effect A duration with a finite length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.Null, "TryUpdateStatusEffectDuration unexpectedly overwrote effect B duration despite being given an invalid (negative) length");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.Null, "TryUpdateStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (zero) length");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of indefinite status effect to (null delay, null duration) (indefinite) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TryUpdateStatusEffectDuration to adjust status effect duration of effects with delays, whilst leaving the delay argument unspecified")]
    public async Task TryUpdateStatusEffectDuration_Delays_DefaultBehaviour()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusA, FiftyTicks), Is.True, "Using TrySetStatusEffectDuration to set duration of (20 delay, 20 duration) StatusA to (null delay, 50 duration) unexpectedly returned false");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusB, TenTicks), Is.True, "Using TrySetStatusEffectDuration to set duration of (20 delay, 20 duration) StatusB to (null delay, 10 duration) unexpectedly returned false (shorter but valid duration arg, should return true)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusC, -TenTicks), Is.False, "Using TrySetStatusEffectDuration to set duration of (20 delay, 20 duration) StatusC to (null delay, -10 duration) unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusD, null), Is.True, "Using TrySetStatusEffectDuration to to set duration of (20 delay, 20 duration) StatusB to null (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TryUpdateStatusEffectDuration default behaviour with valid positive duration (null delay, 50 duration) somehow didn't overwrite the delay of Status A to zero.");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + FiftyTicks).Within(WiggleRoom), "Overwriting duration of status effect A to a positive amount (null delay, 50 duration) did not result in 50-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TryUpdateStatusEffectDuration overwriting (20 delay, 20 duration) with (null delay, 10 duration) didn't overwrite delay to zero!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect B duration. Initial (20 delay, 20 duration) end time was 40, attempted overwrite with (null delay, 10 duration) should have preserved longer initial end time of 40");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect C delay despite the method failing due to being given an invalid (negative) length!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (negative) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TryUpdateStatusEffectDuration default behaviour with valid (null delay, null duration) duration somehow didn't overwrite the delay of Status D to zero.");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (null delay, null duration) (indefinite) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TryUpdateStatusEffectDuration to adjust status effect duration of effects with delays, whilst explicitly passing a delay of null")]
    public async Task TryUpdateStatusEffectDuration_Delays_PassingDelay_Null()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusA, FiftyTicks, null), Is.True, "Using TrySetStatusEffectDuration to set duration of (20 delay, 20 duration) StatusA to (null delay, 50 duration) unexpectedly returned false");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusB, TenTicks, null), Is.True, "Using TrySetStatusEffectDuration to set duration of (20 delay, 20 duration) StatusB to (null delay, 10 duration) unexpectedly returned false (shorter but valid duration arg, should return true)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusC, -TenTicks, null), Is.False, "Using TrySetStatusEffectDuration to set duration of (20 delay, 20 duration) StatusC to (null delay, -10 duration) unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusD, null, null), Is.True, "Using TrySetStatusEffectDuration to to set duration of (20 delay, 20 duration) StatusB to null (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TryUpdateStatusEffectDuration default behaviour with valid positive duration (null delay, 50 duration) somehow didn't overwrite the delay of Status A to zero.");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + FiftyTicks).Within(WiggleRoom), "Overwriting duration of status effect A to a positive amount (null delay, 50 duration) did not result in 50-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TryUpdateStatusEffectDuration overwriting (20 delay, 20 duration) with (null delay, 10 duration) didn't overwrite delay to zero!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect B duration. Initial (20 delay, 20 duration) end time was 40, attempted overwrite with (null delay, 10 duration) should have preserved longer initial end time of 40");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect C delay despite the method failing due to being given an invalid (negative) length!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (negative) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "TryUpdateStatusEffectDuration default behaviour with valid (null delay, null duration) duration somehow didn't overwrite the delay of Status D to zero.");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (null delay, null duration) (indefinite) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TryUpdateStatusEffectDuration to adjust durations & decreasing the delays of effects which already have delays")]
    public async Task TryUpdateStatusEffectDuration_Delays_DecreaseDelays()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusA, FiftyTicks, TenTicks), Is.True, "Using TryUpdateStatusEffectDuration to set duration of (20 delay, 20 duration) StatusA to (10 delay, 50 duration) unexpectedly returned false");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusB, TenTicks, TenTicks), Is.True, "Using TryUpdateStatusEffectDuration to set duration of (20 delay, 20 duration) StatusB to (10 delay, 10 duration) unexpectedly returned false (shorter but valid duration arg, should return true)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusC, -TenTicks, TenTicks), Is.False, "Using TryUpdateStatusEffectDuration to set duration of (20 delay, 20 duration) StatusC to (10 delay, -10 duration) unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusD, null, TenTicks), Is.True, "Using TryUpdateStatusEffectDuration to to set duration of (20 delay, 20 duration) StatusB to (10 delay, null duration) (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration default behaviour with valid positive duration from (20 delay, 20 duration) to (10 delay, 50 duration) somehow didn't reduce the delay of Status A to 10.");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + (ThirtyTicks * 2)).Within(WiggleRoom), "Overwriting duration of status effect A from (20 delay, 20 duration) to a positive amount (10 delay, 50 duration) did not result in 60-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration overwriting (20 delay, 20 duration) with (10 delay, 10 duration) didn't overwrite delay to 10!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect B duration. Initial (20 delay, 20 duration) end time was 40, attempted overwrite with (null delay, 10 duration) should have preserved longer initial end time of 40");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect C delay despite the method failing due to being given an invalid (negative) length!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (negative) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration default behaviour with valid (10 delay, null duration) duration somehow didn't overwrite the delay of Status D to 10.");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (null delay, null duration) (indefinite) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TryUpdateStatusEffectDuration to adjust durations & decreasing the delays of effects which already have delays")]
    public async Task TryUpdateStatusEffectDuration_Delays_DecreaseDelays_NegativeDelay_Weird()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusA, (ThirtyTicks * 2), -TenTicks), Is.True, "Using TryUpdateStatusEffectDuration to set duration of (20 delay, 20 duration) StatusA to (-10 delay, 60 duration) unexpectedly returned false");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusB, TenTicks, -TenTicks), Is.True, "Using TryUpdateStatusEffectDuration to set duration of (20 delay, 20 duration) StatusB to (-10 delay, 10 duration) unexpectedly returned false (shorter but valid duration arg, should return true)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusC, -TenTicks, -TenTicks), Is.False, "Using TryUpdateStatusEffectDuration to set duration of (20 delay, 20 duration) StatusC to (-10 delay, -10 duration) unexpectedly returned true (invalid input, it should do nothing)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusD, null, -TenTicks), Is.True, "Using TryUpdateStatusEffectDuration to to set duration of (20 delay, 20 duration) StatusB to (-10 delay, null duration) (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration passing (-10 delay, 60 duration) somehow didn't overwrite the delay of Status A to -10.");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + FiftyTicks).Within(WiggleRoom), "Overwriting duration of status effect A to a positive amount with negative delay (-10 delay, 60 duration) did not result in 50-tick end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration passing (-10 delay, 10 duration) somehow didn't overwrite the delay of Status B to -10.");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "Overwriting status effect B to (-10 delay, 10 duration) somehow overwrote end time of 40 a different (presumably shorter) end time");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration passing (-10 delay, -10 duration) somehow overwrote delay (should've done nothing due to invalid duration argument)");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "Overwriting status effect C to (-10 delay, -10 duration) somehow overwrote end time of 40 (should've done nothing due to invalid duration argument)");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration passing (-10 delay, null duration) somehow didn't overwrite the delay of Status D to -10.");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (-10 delay, null duration) somehow made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TryUpdateStatusEffectDuration to increase the delays of status effects - unsupported(?) operation, see remarks in comments.")]
    public async Task TryUpdateStatusEffectDuration_Delays_IncreaseDelays_WeirdEdgeCase()
    {
        // TODO: do we still want this method to return true when a longer (invalid) delay has been given?
        //  * if delay is given, current implementation calculates new end time as (current time + delay + duration)
        //  * underlying `UpdateStatusEffectDelay` method rejects any delays longer than existing delay
        //  * however, this isn't factored in when calculating new effect end time - diff between current delay and desired delay is added to duration.
        //  is this intended behaviour?
        //  this test has been written based on current implementation (in case any systems rely on this weird behaviour), please adjust if implementation changes 
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusA, ThirtyTicks, ThirtyTicks), Is.True, "Weird TryUpdateStatusEffectDuration increase 20 length 20 delay StatusA to 30 delay 30 duration unexpectedly(?) returned false - should(?) return true, weird edge case, see TODO in method");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusB, -TenTicks, ThirtyTicks), Is.False, "Using TryUpdateStatusEffectDuration to set duration of 20 length 20 delay StatusB to 30 delay -10 duration unexpectedly returned true (invalid duration input, it should do nothing)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero, ThirtyTicks), Is.False, "Using TryUpdateStatusEffectDuration to set duration of 20 length 20 delay StatusC to 30 delay 0 duration with 10 delay unexpectedly returned true (invalid duration input, it should do nothing)");
                Assert.That(_sStatusSystem.TryUpdateStatusEffectDuration(SPlayer, StatusD, null, ThirtyTicks), Is.True, "Using TryUpdateStatusEffectDuration to to set duration of 20 length 20 delay StatusB to 30 delay null duration (making it indefinite again) unexpectedly(?) returned false - should(?) return true, weird edge case, see TODO in method");

                // b and c are simple, should've done nothing.
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect B delay, method should've done nothing due to being given invalid (negative) duration!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect B duration despite being given an invalid (negative) length!");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect C delay, method should've done nothing due to being given invalid (zero) duration!");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration unexpectedly overwrote effect C duration despite being given an invalid (zero) length!");

                // A. Calculated new end time should be 60 (30delay + 30length = 60). However, as 30delay > 20delay, current implementation is expected to leave delay as 20.
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration passing 30 delay and 30 duration somehow overwrote the delay of 20 delay 20 duration Status A (current implementation should preserve the current shorter delay)");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + (ThirtyTicks * 2)).Within(WiggleRoom), "Overwriting duration of status effect A to a positive amount (30 duration, 30 delay -> end time = 30+30) did not result in 60-tick end time");

                // D. Calculated new end time should be null (given null duration). End time should still be 20 (30delay > 20delay, preserve 20 delay).
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryUpdateStatusEffectDuration passing 30 delay and null duration somehow overwrote the delay of 20 delay 20 duration Status D (current implementation should preserve the current shorter delay)");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to (delay 30(?) duration null) somehow made the duration non-indefinite");
            }
        });
    }
    
    #endregion
    
    #region TryAddStatusEffectDuration

    [Test, Description("Testing TryAddStatusEffectDuration to add status effect duration")]
    public async Task TryAddStatusEffectDuration_AddTime()
    {
        var curTime = await SetupStatusesSameLength(delay20:false,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, TenTicks), Is.True, "Using use TryAddStatusEffectDuration to add 10 ticks to finite duration of StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusB, -TenTicks), Is.True, "Using use TryAddStatusEffectDuration to add -10 ticks to finite duration of StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero), Is.False, "Using use TryAddStatusEffectDuration to add zero to finite duration of StatusC unexpectedly returned true (should return false if given duration of zero)");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusD, null), Is.True, "Using use TryAddStatusEffectDuration to add null to finite duration of StatusD (to make it indefinite) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Adding 10 ticks to 20-tick status effect did not give duration 30");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Adding minus 10 ticks to 20-tick status effect did not give duration 10");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Adding 0 ticks to 20-tick status effect did not give duration 20");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Adding null to 20-tick status effect did not make the effect indefinite");
            }
        });
    }
    
    [Test, Description("Testing TryAddStatusEffectDuration to add status effect duration to indefinite status effects")]
    public async Task TryAddStatusEffectDuration_Null()
    {
        await SetupStatusesSameLength(delay20:false,duration20:false);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, TenTicks), Is.True, "Using TryAddStatusEffectDuration to add 10 ticks to indefinite duration of StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusB, -TenTicks), Is.True, "Using TryAddStatusEffectDuration to add -10 ticks (subtract 10) to indefinite duration of StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero), Is.False, "Using TryAddStatusEffectDuration to add zero to indefinite duration of StatusC unexpectedly returned true (should return false if given duration of zero)");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusD, null), Is.True, "Using TryAddStatusEffectDuration to add null to indefinite duration of StatusD (making it indefinite again) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.Null, "Adding 10 ticks to indefinite status effect made the duration non-indefinite");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.Null, "Adding minus 10 ticks to indefinite status effect made the duration non-indefinite");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.Null, "Adding 0 ticks to indefinite status effect made the duration non-indefinite");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Adding null to indefinite status effect made the duration non-indefinite");
            }
        });
    }

    [Test, Description("Testing TryAddStatusEffectDuration to add status effect duration to effects with delays, passing null")]
    public async Task TryAddStatusEffectDuration_Delays_PassingNull()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, TenTicks, null), Is.True, "Using use TryAddStatusEffectDuration to add 10 ticks to 20-delay 20-duration StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusB, -TenTicks, null), Is.True, "Using use TryAddStatusEffectDuration to add -10 ticks to 20-delay 20-duration StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero, null), Is.False, "Using use TryAddStatusEffectDuration to add zero to 20-delay 20-duration StatusC unexpectedly returned true (should return false if given duration of zero)");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusD, null, null), Is.True, "Using use TryAddStatusEffectDuration to add null to 20-delay 20-duration StatusD (to make it indefinite) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "Passing null for TryAddStatusEffectDuration delay argument somehow did not overwrite the delay with current time!");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + FiftyTicks).Within(WiggleRoom), "Adding 10 ticks to 20-delay 20-duration status effect did not give end time 50");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "Passing null for TryAddStatusEffectDuration delay argument somehow did not overwrite the delay with current time!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Adding minus 10 ticks to 20-delay 20-duration status effect did not give end time 30");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Adding zero duration to status effect C somehow overwrote the delay! (should have done nothing, zero duration should have been rejected, preventing method from being executed)");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "Adding 0 ticks to 20-delay 20-duration status effect did not give end time 40 (prior end time of 40 should have been preserved, invalid duration argument of 0 should have prevented method execution)");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "Passing null for TryAddStatusEffectDuration delay argument somehow did not overwrite the delay with current time!");
                Assert.That(timeD.EndEffectTime, Is.Null, "Adding null to 20-delay 20-duration status effect did not make the effect indefinite");
            }
        });
    }

    [Test, Description("Testing TrySetStatusEffectDuration to adjust status effect duration of effects with delays, whilst leaving the delay argument unspecified")]
    public async Task TryAddStatusEffectDuration_Delays_DecreaseDelays()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, TenTicks, TenTicks), Is.True, "Using use TryAddStatusEffectDuration to set 10 delay and add 10 duration to 20-delay 20-duration StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusB, -TenTicks, TenTicks), Is.True, "Using use TryAddStatusEffectDuration to set 10 delay and add -10 duration ticks to 20-delay 20-duration StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero, TenTicks), Is.False, "Using use TryAddStatusEffectDuration to set 10 delay and add zero length to 20-delay 20-duration StatusC unexpectedly returned true (should return false if given duration of zero)");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusD, null, TenTicks), Is.True, "Using use TryAddStatusEffectDuration to set 10 delay and add null length to 20-delay 20-duration StatusD (to make it indefinite) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Adding 10 duration and setting 10 delay for TryAddStatusEffectDuration delay argument somehow did not overwrite the delay with current time!");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + FiftyTicks).Within(WiggleRoom), "Adding 10 duration and setting 10 delay to 20-delay 20-duration status effect did not give end time 50");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Passing null for TryAddStatusEffectDuration delay argument somehow did not overwrite the delay with current time!");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Adding minus 10 duration and setting 10 delay to 20-delay 20-duration status effect did not give end time 30");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Adding zero duration and setting 10 delay to status effect C somehow overwrote the delay! (should have done nothing, zero duration should have been rejected, preventing method from being executed)");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "Adding 0 duration and setting 10 delay to 20-delay 20-duration status effect did not give end time 40 (prior end time of 40 should have been preserved, invalid duration argument of 0 should have prevented method execution)");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Passing null duration and setting 10 delay for TryAddStatusEffectDuration delay argument somehow did not overwrite the delay with current time!");
                Assert.That(timeD.EndEffectTime, Is.Null, "Adding null to 20-delay 20-duration status effect did not make the effect indefinite");
            }
        });
    }

    [Test, Description("Testing TryAddStatusEffectDuration to adjust durations & decreasing the delays of effects which already have delays")]
    public async Task TryAddStatusEffectDuration_Delays_DecreaseDelays_NegativeDelay_Weird()
    {
        var curTime = await SetupStatusesSameLength(delay20:true,duration20:true);
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, TenTicks, -TenTicks), Is.True, "Using use TryAddStatusEffectDuration to set -10 delay and add 10 duration to 20-delay 20-duration StatusA unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusB, -TenTicks, -TenTicks), Is.True, "Using use TryAddStatusEffectDuration to set -10 delay and add -10 duration ticks to 20-delay 20-duration StatusB unexpectedly returned false");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusC, TimeSpan.Zero, -TenTicks), Is.False, "Using use TryAddStatusEffectDuration to set -10 delay and add zero length to 20-delay 20-duration StatusC unexpectedly returned true (should return false if given duration of zero)");
                Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusD, null, -TenTicks), Is.True, "Using use TryAddStatusEffectDuration to set -10 delay and add null length to 20-delay 20-duration StatusD (to make it indefinite) unexpectedly returned false");

                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "TryAddStatusEffectDuration passing -10 delay and adding -10 duration somehow didn't overwrite the delay of Status A to -10.");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + FiftyTicks).Within(WiggleRoom), "passing -10 delay and adding -10 duration to effect A did not result in 50-tick end time (20 delay 20 duration -> add 10 duration moves end time to 50 -> setting delay to -10 doesn't change end time)");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "TryAddStatusEffectDuration passing -10 delay and adding -10 duration somehow didn't overwrite the delay of Status B to -10.");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "passing -10 delay and adding -10 duration to status effect B (from 20 delay 20 duration) didn't update end time to 30 (20 delay 20 duration -> add -10 duration moves end time to 30 -> setting delay to -10 doesn't change end time)");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "TryAddStatusEffectDuration passing -10 delay and adding -10 duration somehow overwrote delay (should've done nothing due to invalid duration argument)");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + FortyTicks).Within(WiggleRoom), "passing -10 delay and adding 0 duration for Effect C somehow overwrote end time of 40 (should've done nothing due to invalid duration argument)");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(WiggleRoom), "TryAddStatusEffectDuration passing -10 delay and setting null duration somehow didn't overwrite the delay of Status D to -10.");
                Assert.That(timeD.EndEffectTime, Is.Null, "Overwriting duration of finite status effect with 20 tick delay to indefinite via passing -10 delay and setting null duration somehow made the duration non-indefinite");
            }
        });
    }
    #endregion

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
        Assume.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Status effect A did not have expected end time of 10 ticks after current time");

        Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, TwentyTicks), Is.True, "TryAddStatusEffectDuration adding 20 ticks to duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTenPlusTwenty);
        Assert.That(timePlusTenPlusTwenty.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Status effect A did not have expected updated end time of 30 ticks after current time (was 10, 20 added)");

        Assert.That(_sStatusSystem.TryAddStatusEffectDuration(SPlayer, StatusA, -TenTicks), Is.True, "TryAddStatusEffectDuration removing ten ticks from duration did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTenPlusTwentyMinusTen);
        Assert.That(timePlusTenPlusTwentyMinusTen.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Status effect A did not have expected updated end time of 20 ticks after current time (was 30, 10 removed)");
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
        Assume.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Status effect A did not have expected end time of 10 ticks after current time");

        Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, ThirtyTicks), Is.True, "TrySetDuration setting to 30 ticks did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusThirty);
        Assert.That(timePlusThirty.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Status effect A did not have expected updated end time of 30 ticks after current time");

        Assert.That(_sStatusSystem.TrySetDuration(SPlayer, StatusA, TwentyTicks), Is.True, "TrySetDuration setting to 20 ticks did not return true");
        _sStatusSystem.TryGetTime(SPlayer, StatusA, out var timePlusTwenty);
        Assert.That(timePlusTwenty.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Status effect A did not have expected updated end time of 20 ticks after current time");
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
        Assume.That(time.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Status effect A did not have expected end time of 10 ticks after current time");

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
            Assert.That(time1.StartEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Status effect A did not have expected start time 20 ticks after current time");
            Assert.That(time1.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(WiggleRoom), "Status effect A did not have expected end time of 30 ticks after current time");
            Assert.That(compStatusA!.Applied, Is.False, "Status effect A was applied to the player, despite delay not being over");
        }

        Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks, TenTicks), Is.True, "TrySetStatusEffectDuration reducing to 10 tick delay did not return true");
        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time2), Is.True, "Could not get time info for effect A (delay 10, duration 10, end time should be 20)");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(time2.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Status effect A did not have expected start time 10 ticks after current time (delay 10, duration 10, end time should be 20)");
            Assert.That(time2.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(WiggleRoom), "Status effect A did not have expected end time of 20 ticks after current time  (delay 10, duration 10, end time should be 20)");
            Assert.That(compStatusA!.Applied, Is.False, "Status effect A was applied to the player, despite reduced delay not being over");
        }

        Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, TenTicks, null), Is.True, "TrySetStatusEffectDuration setting to null delay did not return true");
        Assume.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var time3), Is.True, "Could not get time info for effect A (delay null, duration 10, end time should be 10)");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(time3.StartEffectTime, Is.EqualTo(curTime).Within(WiggleRoom), "Status effect A did not have expected start time of immediately (delay null, duration 10, end time should be 10)");
            Assert.That(time3.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(WiggleRoom), "Status effect A did not have expected end time of 10 ticks after current time  (delay null, duration 10, end time should be 10)");
        }
        await Server.WaitRunTicks(1); // need to wait for callbacks and such to resolve themselves in order for the "applied" status to update 
        Assert.That(compStatusA!.Applied, Is.True, "Status effect A was not applied to the player after delay was reduced to zero");
    }

    
}