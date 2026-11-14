using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This system implements the behavior of <see cref="SatiationDamageComponent"/>
/// </summary>
public sealed partial class SatiationDamageSystem :
    BaseSatiationEffectSystem<SatiationDamageComponent, DamageSpecifier?>
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var satiationDamageQuery = EntityQueryEnumerator<SatiationDamageComponent>();
        while (satiationDamageQuery.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime < comp.NextDamageTime ||
                _mobState.IsDead(ent))
                continue;

            comp.NextDamageTime = _timing.CurTime + comp.Frequency;
            DirtyField(ent, comp, nameof(SatiationDamageComponent.NextDamageTime));

            foreach (var (_, thresholds) in comp.Satiations)
            {
                if (thresholds.Current is not { } damage)
                    continue;

                _damageable.TryChangeDamage(ent, damage, interruptsDoAfters: false);
            }
        }
    }

    protected override DamageSpecifier? DefaultValue() => null;

    protected override Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<DamageSpecifier?>> GetThresholds(
        SatiationDamageComponent comp) => comp.Satiations;
}
