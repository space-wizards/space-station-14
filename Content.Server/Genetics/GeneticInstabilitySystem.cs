using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Genetics;

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

            PreserveGeneticDeathThreshold(uid, genetics);

            if (_mobState.IsDead(uid))
                continue;

            genetics.UnstableDamageTimer += frameTime;
            if (genetics.UnstableDamageTimer < DamageInterval)
                continue;

            genetics.UnstableDamageTimer -= DamageInterval;

            var cellularDmg = new DamageSpecifier();
            cellularDmg.DamageDict.Add("Cellular", FixedPoint2.New(DamagePerTick));
            _damageable.TryChangeDamage(uid, cellularDmg, ignoreResistances: true);

            var currentDamage = _damageable.GetAllDamage(uid);
            if (currentDamage.DamageDict.TryGetValue("Cellular", out var cellularTotal) &&
                cellularTotal >= FixedPoint2.New(CellularDeathThreshold))
            {
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
