using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Drunk;
using Content.Shared.Mobs.Systems;
using Content.Shared.Speech.EntitySystems;

namespace Content.Server.Body.Systems;

public sealed class BloodstreamSystem : SharedBloodstreamSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedDrunkSystem _drunkSystem = default!;
    [Dependency] private readonly SharedStutteringSystem _stutteringSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bloodstream))
        {
            if (GameTiming.CurTime < bloodstream.NextUpdate)
                continue;

            bloodstream.NextUpdate += bloodstream.UpdateInterval;

            if (!SolutionContainerSystem.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
                continue;

            // Adds blood to their blood level if it is below the maximum; Blood regeneration. Must be alive.
            if (bloodSolution.Volume < bloodSolution.MaxVolume && !_mobStateSystem.IsDead(uid))
            {
                TryModifyBloodLevel(uid, bloodstream.BloodRefreshAmount, bloodstream);
            }

            // Removes blood from the bloodstream based on bleed amount (bleed rate)
            // as well as stop their bleeding to a certain extent.
            if (bloodstream.BleedAmount > 0)
            {
                // Blood is removed from the bloodstream at a 1-1 rate with the bleed amount
                TryModifyBloodLevel(uid, (-bloodstream.BleedAmount), bloodstream);
                // Bleed rate is reduced by the bleed reduction amount in the bloodstream component.
                TryModifyBleedAmount(uid, -bloodstream.BleedReductionAmount);
            }

            // deal bloodloss damage if their blood level is below a threshold.
            var bloodPercentage = GetBloodLevelPercentage(uid, bloodstream);
            if (bloodPercentage < bloodstream.BloodlossThreshold && !_mobStateSystem.IsDead(uid))
            {
                // bloodloss damage is based on the base value, and modified by how low your blood level is.
                var amt = bloodstream.BloodlossDamage / (0.1f + bloodPercentage);

                _damageableSystem.TryChangeDamage(uid, amt, ignoreResistances: false, interruptsDoAfters: false);

                // Apply dizziness as a symptom of bloodloss.
                // The effect is applied in a way that it will never be cleared without being healthy.
                // Multiplying by 2 is arbitrary but works for this case, it just prevents the time from running out
                _drunkSystem.TryApplyDrunkenness(
                    uid,
                    (float) bloodstream.UpdateInterval.TotalSeconds * 2,
                    applySlur: false);
                _stutteringSystem.DoStutter(uid, bloodstream.UpdateInterval * 2, refresh: false);

                // storing the drunk and stutter time so we can remove it independently from other effects additions
                bloodstream.StatusTime += bloodstream.UpdateInterval * 2;
            }
            else if (!_mobStateSystem.IsDead(uid))
            {
                // If they're healthy, we'll try and heal some bloodloss instead.
                _damageableSystem.TryChangeDamage(uid, bloodstream.BloodlossHealDamage * bloodPercentage, ignoreResistances: true, interruptsDoAfters: false);

                // Remove the drunk effect when healthy. Should only remove the amount of drunk and stutter added by low blood level
                _drunkSystem.TryRemoveDrunkenessTime(uid, bloodstream.StatusTime.TotalSeconds);
                _stutteringSystem.DoRemoveStutterTime(uid, bloodstream.StatusTime.TotalSeconds);
                // Reset the drunk and stutter time to zero
                bloodstream.StatusTime = TimeSpan.Zero;
            }
        }
    }

    private void OnBeingGibbed(Entity<BloodstreamComponent> ent, ref BeingGibbedEvent args)
    {
        SpillAllSolutions(ent, ent);
    }

}
