using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This system implements the behavior of <see cref="SatiationDamageComponent"/>
/// </summary>
public sealed partial class SatiationDamageSystem : BaseContinuousSatiationEffectSystem<SatiationDamageComponent>
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    protected override TimeSpan GetContinuousEffectFrequency(SatiationDamageComponent comp) => comp.Frequency;
    protected override ref TimeSpan GetContinuousEffectTime(SatiationDamageComponent comp) => ref comp.NextDamageTime;

    /// <summary>
    /// Caches the damage values for the current thresholds.
    /// </summary>
    protected override void OnThresholdChanged(
        Entity<SatiationDamageComponent, SatiationComponent> entity,
        ref SatiationThresholdChangedEvent args
    )
    {
        entity.Comp1.CachedDamageValues.Clear();
        var satiation = new Entity<SatiationComponent>(entity, entity);
        foreach (var (type, thresholds) in entity.Comp1.Satiations)
        {
            if (!SatiationSystem.TryGetValueByThreshold(satiation, type, thresholds, out var res) || res == null)
                continue;

            entity.Comp1.CachedDamageValues.Add(type, res);
        }
    }

    protected override void OnContinuousEffect(Entity<SatiationDamageComponent, SatiationComponent> entity)
    {
        if (_mobState.IsDead(entity))
            return;

        foreach (var (_, damage) in entity.Comp1.CachedDamageValues)
        {
            _damageable.TryChangeDamage(entity.Owner, damage, interruptsDoAfters: false);
        }
    }
}
