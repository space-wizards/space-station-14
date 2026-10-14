using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Genetics;

/// <summary>
/// Deals cellular damage when a subject's genetic instability exceeds the threshold,
/// mirroring /tg/ genetics: >100 instability = 15 cellular damage every 10 s;
/// reaching 150 cellular damage triggers death via lethal damage, not a direct state change.
/// </summary>
public sealed class GeneticInstabilitySystem : EntitySystem
{
    private const int InstabilityDamageThreshold = 100;
    private const int CellularDeathThreshold = 150;
    private const float DamageInterval = 10f;
    private const int DamagePerTick = 15;

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private SharedGeneticsSystem _genetics = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GeneticMutationComponent, MobThresholdsComponent>();
        while (query.MoveNext(out var uid, out var genetics, out _))
        {
            var instability = _genetics.GetInstability(uid);

            if (instability <= InstabilityDamageThreshold)
            {
                RestoreDeadThreshold(uid, genetics);
                genetics.UnstableDamageTimer = 0f;
                continue;
            }

            // Ensure the mob won't be killed prematurely by normal damage before we get a chance
            // to deal genetic death — raise the dead threshold to accommodate CellularDeathThreshold.
            PreserveGeneticDeathThreshold(uid, genetics);

            if (_mobState.IsDead(uid))
                continue;

            genetics.UnstableDamageTimer += frameTime;
            if (genetics.UnstableDamageTimer < DamageInterval)
                continue;

            genetics.UnstableDamageTimer -= DamageInterval;

            // Deal cellular damage each tick
            var cellularDmg = new DamageSpecifier();
            cellularDmg.DamageDict.Add("Cellular", FixedPoint2.New(DamagePerTick));
            _damageable.TryChangeDamage(uid, cellularDmg, ignoreResistances: true);

            // Check if we've hit the death threshold via accumulated cellular damage
            var currentDamage = _damageable.GetAllDamage(uid);
            if (currentDamage.DamageDict.TryGetValue("Cellular", out var cellularTotal) &&
                cellularTotal >= FixedPoint2.New(CellularDeathThreshold))
            {
                // Kill by dealing enough damage to exceed the normal dead threshold
                var deathDmg = new DamageSpecifier();
                deathDmg.DamageDict.Add("Cellular", FixedPoint2.New(1000));
                _damageable.TryChangeDamage(uid, deathDmg, ignoreResistances: true, interruptsDoAfters: true);
            }
        }
    }

    private void PreserveGeneticDeathThreshold(EntityUid uid, GeneticMutationComponent genetics)
    {
        if (genetics.HasOriginalDeadThreshold)
            return;
        if (!_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold))
            return;

        genetics.OriginalDeadThreshold = threshold.Value;
        genetics.HasOriginalDeadThreshold = true;

        // Make sure the entity can accumulate enough cellular damage to trigger genetic death
        var required = FixedPoint2.New(CellularDeathThreshold + 50);
        if (threshold.Value < required)
            _mobThreshold.SetMobStateThreshold(uid, required, MobState.Dead);
    }

    private void RestoreDeadThreshold(EntityUid uid, GeneticMutationComponent genetics)
    {
        if (!genetics.HasOriginalDeadThreshold)
            return;

        _mobThreshold.SetMobStateThreshold(uid, genetics.OriginalDeadThreshold, MobState.Dead);
        genetics.HasOriginalDeadThreshold = false;
        genetics.OriginalDeadThreshold = FixedPoint2.Zero;
    }
}
