using System.Diagnostics.CodeAnalysis;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Damage.Systems;

public sealed class SoftCritSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
    }

    /// <summary>
    ///     Attempts to get the actual damage we should be using considering softcrit
    /// </summary>
    public FixedPoint2 GetEffectiveDamage(EntityUid uid,
        DamageableComponent damageableComponent,
        SoftCritComponent? softCritComponent = null)
    {
        Resolve(uid, ref softCritComponent, logMissing:false);
        return softCritComponent?.TotalDamageEffective ?? damageableComponent.TotalDamage;
    }

    /// <summary>
    ///     Calculates the amount of time it will take for this entity's DamageEffective to reach its Damage
    /// </summary>
    private TimeSpan CalculateTimeToEffective(Entity<DamageableComponent> uid, SoftCritComponent? softCrit = null)
    {
        if (!Resolve(uid, ref softCrit))
            return TimeSpan.Zero;

        var DeathThreshold = _mobThreshold.GetThresholdForState(uid, MobState.Dead);
        var TotalDamage = uid.Comp.TotalDamage;

        if (TotalDamage > DeathThreshold)
            return TimeSpan.Zero;

        var Distance = Math.Abs((float)(TotalDamage - softCrit.TotalDamageEffective)) / (float)DeathThreshold;

        return softCrit.DamageLerpTimeZeroDamage * (float)(1 - TotalDamage / DeathThreshold) * Distance;
    }

    private void OnDamageChanged(Entity<DamageableComponent> uid, ref DamageChangedEvent args)
    {
        if (CalculateTimeToEffective(uid) <= TimeSpan.Zero)
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
        ResetDamageEffective(uid, damageable);
        RemComp<ActiveDamageComponent>(uid);
    }

    /// <summary>
    ///     Updates the DamageEffective of a SoftCritComponent
    /// </summary>
    public void UpdateDamageEffective(EntityUid uid,
        float Factor,
        DamageableComponent? damageableComponent = null,
        SoftCritComponent? softCrit = null)
    {
        if (!Resolve(uid, ref damageableComponent) ||
            !Resolve(uid, ref softCrit))
            return;
        foreach (var (DamageType, DamageValue) in damageableComponent.Damage.DamageDict)
        {
            if (softCrit.DamageEffective.DamageDict.TryGetValue(DamageType, out var ThisValue))
                softCrit.DamageEffective.DamageDict[DamageType] = ThisValue + (DamageValue - ThisValue) * Factor;
        }

        DamageEffectiveChanged(uid, softCrit);
    }

    /// <summary>
    ///     Reset the DamageEffective of a SoftCritComponent
    /// </summary>
    public void ResetDamageEffective(EntityUid uid, DamageableComponent? damageableComponent = null, SoftCritComponent? softCritComponent = null)
    {
        if (!Resolve(uid, ref damageableComponent, ref softCritComponent))
            return;

        foreach (var (DamageType, Damage) in damageableComponent.Damage.DamageDict)
        {
            softCritComponent.DamageEffective.DamageDict[DamageType] = Damage;
        }
        DamageEffectiveChanged(uid, softCritComponent);
    }

    /// <summary>
    ///     If the effectivedamage in a DamageableComponent was changed, this function should be called.
    /// </summary>
    public void DamageEffectiveChanged(EntityUid uid, SoftCritComponent softCrit)
    {
        softCrit.TotalDamageEffective = softCrit.DamageEffective.GetTotal();
        Dirty(uid, softCrit);
        RaiseLocalEvent(uid, new DamageEffectiveChangedEvent(softCrit));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveDamageComponent>();

        while (query.MoveNext(out var uid, out var _))
        {
            if (!TryComp(uid, out DamageableComponent? damageableComponent))
                continue;

            if (!TryComp(uid, out SoftCritComponent? softCritComponent))
            {
                RemoveActiveDamage(uid, damageableComponent);
                continue;
            }

            if (softCritComponent.DamageEffective.Empty)
                ResetDamageEffective(uid, damageableComponent, softCritComponent);

            var TimeTo = CalculateTimeToEffective((uid, damageableComponent));

            if (TimeTo <= TimeSpan.FromSeconds(frameTime))
            {
                RemoveActiveDamage(uid, damageableComponent);
                continue;
            }

            UpdateDamageEffective(uid, frameTime / (float)TimeTo.TotalSeconds, damageableComponent, softCritComponent);
        }
    }
}

public sealed class DamageEffectiveChangedEvent : EntityEventArgs
{
    public readonly SoftCritComponent Damageable;

    public DamageEffectiveChangedEvent(SoftCritComponent damageable)
    {
        Damageable = damageable;
    }
}

