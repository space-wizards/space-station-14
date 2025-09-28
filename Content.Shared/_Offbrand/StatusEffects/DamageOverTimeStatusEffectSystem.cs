using Content.Shared.Damage;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class DamageOverTimeStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<StatusEffectComponent, DamageOverTimeStatusEffectComponent>();
        var damagesToDeal = new List<(EntityUid, DamageSpecifier, EntityUid)>();

        while (enumerator.MoveNext(out var uid, out var effect, out var damageOverTime))
        {
            if (_timing.CurTime < damageOverTime.NextUpdate || effect.AppliedTo is not { } target)
                continue;

            damageOverTime.NextUpdate = _timing.CurTime + damageOverTime.UpdateInterval;
            Dirty(uid, damageOverTime);

            damagesToDeal.Add((target, damageOverTime.Damages, uid));
        }

        // work around a concurrent modification exception
        foreach (var (target, damage, source) in damagesToDeal)
        {
            _damageable.TryChangeDamage(target, damage, ignoreResistances: true, interruptsDoAfters: false, origin: source);
        }
    }
}
