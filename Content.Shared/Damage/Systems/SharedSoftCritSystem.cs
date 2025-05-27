using System.Runtime.InteropServices.JavaScript;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public abstract partial class SharedSoftCritSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
    }

    /// <summary>
    ///     Calculates the amount of time it will take for this entity's DamageEffective to reach its Damage
    /// </summary>
    private TimeSpan CalculateTimeToEffective(Entity<DamageableComponent> uid)
    {
        if (!uid.Comp.SoftCritEligible)
            return TimeSpan.Zero;

        var DeathThreshold = _mobThreshold.GetThresholdForState(uid, MobState.Dead);
        var TotalDamage = uid.Comp.TotalDamage;

        if (TotalDamage > DeathThreshold)
            return TimeSpan.Zero;

        var Distance = Math.Abs((float)(TotalDamage - uid.Comp.TotalDamageEffective)) / (float)DeathThreshold;

        return uid.Comp.DamageLerpTimeZeroDamage * (float)(1 - TotalDamage / DeathThreshold) * Distance;
    }

    private void OnDamageChanged(Entity<DamageableComponent> uid, ref DamageChangedEvent args)
    {
        if (!uid.Comp.SoftCritEligible || CalculateTimeToEffective(uid) <= TimeSpan.Zero)
        {
            RemoveActiveDamage(uid, uid.Comp);
            return;
        }

        EnsureComp<ActiveDamageComponent>(uid);
    }

    public void RemoveActiveDamage(EntityUid uid, DamageableComponent? damageable)
    {
        if (!Resolve(uid, ref damageable))
            return;
        _damageable.ResetDamageEffective(uid, damageable);
        RemComp<ActiveDamageComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DamageableComponent, ActiveDamageComponent>();

        while (query.MoveNext(out var uid, out var damageableComponent, out var _))
        {
            if (!damageableComponent.SoftCritEligible) // How did you fucking GET here? (vv probably)
            {
                RemoveActiveDamage(uid, damageableComponent);
                continue;
            }

            if (damageableComponent.DamageEffective.Empty)
                _damageable.ResetDamageEffective(uid, damageableComponent);

            var TimeTo = CalculateTimeToEffective((uid, damageableComponent));

            if (TimeTo <= TimeSpan.FromSeconds(frameTime))
            {
                RemoveActiveDamage(uid, damageableComponent);
                continue;
            }

            _damageable.UpdateDamageEffective(uid, damageableComponent, frameTime / (float)TimeTo.TotalSeconds);
        }
    }
}
