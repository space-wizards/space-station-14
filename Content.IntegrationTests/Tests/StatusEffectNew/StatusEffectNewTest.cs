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
            Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect B did not have an end time of 10 game ticks after start time");

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
    public async Task TestAddNewStatusEffects_NoDelay_TrySetStatusEffectDuration()
    {
        await TestAddNewStatusEffects_NoDelay(_sStatusSystem.TrySetStatusEffectDuration, "TrySetStatusEffectDuration");
    }

    [Test, Description("Test that valid status effects can be added with TryAddStatusEffectDuration (and that invalid ones are rejected) - non-delay variant")]
    public async Task TestAddNewStatusEffects_NoDelay_TryAddStatusEffectDuration()
    {
        await TestAddNewStatusEffects_NoDelay(_sStatusSystem.TryAddStatusEffectDuration, "TryAddStatusEffectDuration");
    }

    [Test, Description("Test that valid status effects can be added with TryUpdateStatusEffectDuration (and that invalid ones are rejected) - non-delay variant")]
    public async Task TestAddNewStatusEffects_NoDelay_TryUpdateStatusEffectDuration()
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
            Assert.That(timeA.StartEffectTime, Is.EqualTo(curTime).Within(SGameTiming.TickPeriod), "Status effect A start time is not currentTime! [null delay, 20 duration]");
            Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect A [null delay, 20 duration] will not end twenty ticks from now"); 
            Assert.That(compStatusA!.Applied, Is.True, "Status effect A was not applied on the player, despite not having a delay [null delay, 20 duration]");

            Assert.That(timeB.StartEffectTime, Is.EqualTo(curTime).Within(SGameTiming.TickPeriod), "Status effect B start time is not currentTime! [zero delay, 20 duration]");
            Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect B [zero delay, 20 duration] will not end twenty ticks from now"); 
            Assert.That(compStatusB!.Applied, Is.True, "Status effect B was not applied on the player, despite having a delay of zero! [zero delay, 20 duration]");

            Assert.That(timeC.StartEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Status effect C is not going to start after a delay of ten ticks [10 delay, 10 duration]");
            Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect C (10 tick delay, 10 tick duration) will not end 20 ticks from now [10 delay, 10 duration]");
            Assert.That(compStatusC!.Applied, Is.False, "Status effect C was applied on the player, despite delay not being over. [10 delay, 10 duration]");

            Assert.That(timeD.StartEffectTime, Is.EqualTo(curTime - TenTicks).Within(SGameTiming.TickPeriod), "Status effect D start time was not 10 ticks ago! [-10 delay, 30 duration]");
            Assert.That(timeD.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Status effect D will not end twenty ticks from now [-10 delay, 30 duration]"); 
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
    public async Task TestAddNewStatusEffects_WithDelay_TrySetStatusEffectDuration()
    {
        await TestAddNewStatusEffects_WithDelay(_sStatusSystem.TrySetStatusEffectDuration, "TrySetStatusEffectDuration");
    }

    [Test, Description("Test that valid status effects with delays can be added with TryAddStatusEffectDuration")]
    public async Task TestAddNewStatusEffects_WithDelay_TryAddStatusEffectDuration()
    {
        await TestAddNewStatusEffects_WithDelay(_sStatusSystem.TryAddStatusEffectDuration, "TryAddStatusEffectDuration");
    }

    [Test, Description("Test that valid status effects with delays can be added with TryUpdateStatusEffectDuration")]
    public async Task TestAddNewStatusEffects_WithDelay_TryUpdateStatusEffectDuration()
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

                Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusA), Is.True, "TryRemoveStatusEffect for Status A failed!");
                Assert.That(_sStatusSystem.TryRemoveStatusEffect(SPlayer, StatusB), Is.False, "TryRemoveStatusEffect for Status B (not on the player) somehow succeeded despite this effect not being on the player!?");
            }
        });

        await Pair.RunUntilSynced(); // have to wait for queued deletion of status effects
        Assert.That(_sStatusSystem.HasStatusEffect(SPlayer, StatusA), Is.False, "Status effect A was still on the player after being removed!");
    }


    /// <summary>
    /// helper method to give SPlayer effects A,B,C,D with duration of null (indefinite) or 20 ticks, and no delay.
    /// </summary>
    /// <param name="duration20">If true, will give effects a duration of 20 ticks. If false, gives duration of null.</param>
    /// <returns>STiming.CurTime as of when the status effects were applied.</returns>
    private async Task<TimeSpan> TryAddOrRemoveTime_Setup(bool duration20)
    {
        var curTime = TimeSpan.Zero;
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                curTime = STiming.CurTime;
                TimeSpan? duration = duration20 ? TwentyTicks : null;
                var durationText = duration20 ? "20 ticks" : "indefinite";
                Constraint durationConstraint = duration20 ? Is.EqualTo(curTime + duration).Within(STiming.TickPeriod) : Is.Null;
                
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusA, duration), Is.True, $"Status effect A ({durationText} duration) could not be added to player");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusB, duration), Is.True, $"Status effect B ({durationText} duration) could not be added to player");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusC, duration), Is.True, $"Status effect C ({durationText} duration) could not be added to player");
                Assert.That(_sStatusSystem.TrySetStatusEffectDuration(SPlayer, StatusD, duration), Is.True, $"Status effect D ({durationText} duration) could not be added to player");
                
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var initialTimeA), Is.True, "Could not get time info for effect A");
                Assert.That(initialTimeA.EndEffectTime, durationConstraint, $"Initial duration of effect A was not {durationText}");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var initialTimeB), Is.True, "Could not get time info for effect B");
                Assert.That(initialTimeB.EndEffectTime, durationConstraint, $"Initial duration of effect B was not {durationText}");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var initialTimeC), Is.True, "Could not get time info for effect C");
                Assert.That(initialTimeC.EndEffectTime, durationConstraint, $"Initial duration of effect C was not {durationText}");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var initialTimeD), Is.True, "Could not get time info for effect D");
                Assert.That(initialTimeD.EndEffectTime, durationConstraint, $"Initial duration of effect D was not {durationText}");
            }
        });
        await Pair.RunUntilSynced();
        return curTime;
    }
    
    [Test, Description("Testing TryAddTime to add status effect duration")]
    public async Task TryAddTime()
    {
        var curTime = await TryAddOrRemoveTime_Setup(true);

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, TenTicks), Is.True, "Unable to use TryAddTime to add 10 ticks to duration of StatusA");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusB, -TenTicks), Is.True, "Unable to use TryAddTime to add -10 ticks to duration of StatusB");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Unable to use TryAddTime to add zero to duration of StatusC");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusD, null), Is.True, "Unable to use TryAddTime to add null to duration of StatusD");
                
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.EqualTo(curTime + ThirtyTicks).Within(SGameTiming.TickPeriod), "Adding 10 ticks to 20-tick status effect did not give duration 30");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.EqualTo(curTime + TenTicks).Within(SGameTiming.TickPeriod), "Adding minus 10 ticks to 20-tick status effect did not give duration 10");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.EqualTo(curTime + TwentyTicks).Within(SGameTiming.TickPeriod), "Adding 0 ticks to 20-tick status effect did not give duration 20");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Adding null to 20-tick status effect did not make the effect indefinite");
            }
        });
    }
    
    [Test, Description("Testing TryAddTime to add status effect duration")]
    public async Task TryAddTime_Null()
    {
        var curTime = await TryAddOrRemoveTime_Setup(false);

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusA, TenTicks), Is.True, "Unable to use TryAddTime to add 10 ticks to duration of StatusA");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusB, -TenTicks), Is.True, "Unable to use TryAddTime to add -10 ticks to duration of StatusB");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusC, TimeSpan.Zero), Is.True, "Unable to use TryAddTime to add zero to duration of StatusC");
                Assert.That(_sStatusSystem.TryAddTime(SPlayer, StatusD, null), Is.True, "Unable to use TryAddTime to add null to duration of StatusD");
                
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusA, out var timeA), Is.True, "Could not get time info for effect A");
                Assert.That(timeA.EndEffectTime, Is.Null, "Adding 10 ticks to 20-tick status effect did not give duration 30");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusB, out var timeB), Is.True, "Could not get time info for effect B");
                Assert.That(timeB.EndEffectTime, Is.Null, "Adding minus 10 ticks to 20-tick status effect did not give duration 10");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusC, out var timeC), Is.True, "Could not get time info for effect C");
                Assert.That(timeC.EndEffectTime, Is.Null, "Adding 0 ticks to 20-tick status effect did not give duration 20");
                Assert.That(_sStatusSystem.TryGetTime(SPlayer, StatusD, out var timeD), Is.True, "Could not get time info for effect D");
                Assert.That(timeD.EndEffectTime, Is.Null, "Adding null to 20-tick status effect did not make the effect indefinite");
            }
        });
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