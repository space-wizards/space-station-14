using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.EntitySystems;
using Content.Shared.MobThresholds.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.MobThresholds.Systems;

public sealed class MobThresholdSystem : EntitySystem
{
    [Dependency] private readonly SharedMobStateSystem _mobStateSystem= default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MobThresholdComponent, ComponentStartup>(MobThresholdStartup);
        SubscribeLocalEvent<MobThresholdComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<MobThresholdComponent, ComponentGetState>(OnGetComponentState);
        SubscribeLocalEvent<MobThresholdComponent, ComponentHandleState>(OnHandleComponentState);
    }

    private void OnHandleComponentState(EntityUid uid, MobThresholdComponent component, ComponentHandleState args)
    {
        if (args.Current is not MobThresholdComponentState state)
            return;
        component.Thresholds = state.Thresholds;
        component.CurrentThresholdState = state.CurrentThresholdState;
    }

    private void OnGetComponentState(EntityUid uid, MobThresholdComponent component, ComponentGetState args)
    {
        args.State = new MobThresholdComponentState(component.CurrentThresholdState, component.Thresholds);
    }

    private void MobThresholdStartup(EntityUid uid, MobThresholdComponent component, ref ComponentStartup args)
    {
        if (!HasComp<DamageableComponent>(uid))
        {
            Logger.Warning("Entity: "+uid+" "+EnsureComp<MetaDataComponent>(uid).EntityName+ "Does not have a damageableComponent. Adding one!");
            EnsureComp<DamageableComponent>(uid);
        }
        var mobStateComp = EnsureComp<MobStateComponent>(uid);
        foreach (var (damage,mobState) in component.Thresholds)
        {
            mobStateComp.AllowedStates.Add(mobState);
            component.ThresholdReverseLookup.Add(mobState, damage);
        }
        TriggerThreshold(uid, mobStateComp, component, MobState.MobState.Invalid, component.Thresholds.First().Value);
    }

    public FixedPoint2 GetThresholdForState(EntityUid uid, MobState.MobState mobState, MobThresholdComponent? thresholdComponent)
    {
        if (!Resolve(uid, ref thresholdComponent) || !thresholdComponent.ThresholdReverseLookup.TryGetValue(mobState, out var threshold))
        {
            return FixedPoint2.Zero;
        }
        return threshold;
    }

    public bool TryGetThresholdForState(EntityUid uid, MobState.MobState mobState,  [NotNullWhen(true)] out FixedPoint2? threshold,
        MobThresholdComponent? thresholdComponent = null)
    {
        if (!Resolve(uid, ref thresholdComponent) ||
            !thresholdComponent.ThresholdReverseLookup.TryGetValue(mobState, out var foundThreshold))
        {
            threshold = null;
            return false;
        }

        threshold = foundThreshold;
        return true;
    }

    /// <summary>
    /// Takes the damage from one entity and scales it relative to the health of another
    /// </summary>
    /// <param name="ent1">The entity whose damage will be scaled</param>
    /// <param name="ent2">The entity whose health the damage will scale to</param>
    /// <param name="damage">The newly scaled damage. Can be null</param>
    public bool GetScaledDamage(EntityUid ent1, EntityUid ent2, out DamageSpecifier? damage)
    {
        damage = null;

        if (!TryComp<DamageableComponent>(ent1, out var oldDamage))
            return false;

        if (!TryComp<MobThresholdComponent>(ent1, out var threshold1) ||
            !TryComp<MobThresholdComponent>(ent2, out var threshold2))
            return false;

        if (!TryGetThresholdForState(ent1, MobState.MobState.Dead, out var ent1DeadThreshold, threshold1))
        {
            ent1DeadThreshold = 0;
        }
        if (!TryGetThresholdForState(ent2, MobState.MobState.Dead, out var ent2DeadThreshold, threshold2))
        {
            ent2DeadThreshold = 0;
        }

        damage = (oldDamage.Damage / ent1DeadThreshold.Value) * ent2DeadThreshold.Value;
        return true;
    }

    public void SetMobStateThreshold(EntityUid uid, FixedPoint2 damage, MobState.MobState mobState,
        MobThresholdComponent? thresholdComponent = null)
    {
        if (!Resolve(uid, ref thresholdComponent))
            return;
        if (thresholdComponent.ThresholdReverseLookup.TryGetValue(mobState, out var oldDamage))
        {
            thresholdComponent.Thresholds.Remove(oldDamage);
        }
        thresholdComponent.Thresholds[damage] = mobState;
        thresholdComponent.ThresholdReverseLookup[mobState] = damage;
        VerifyThresholds(uid, thresholdComponent);
    }

    private void OnDamaged(EntityUid uid, MobThresholdComponent mobThresholdComponent, ref DamageChangedEvent args)
    {
        var mobStateComp = EnsureComp<MobStateComponent>(uid);
        CheckThresholds(uid, mobStateComp, mobThresholdComponent, args.Damageable);
    }

    //Call this if you are somehow change the amount of damage on damageable without triggering a damageChangedEvent
    public void VerifyThresholds(EntityUid target, MobThresholdComponent? thresholdComponent = null,
        MobStateComponent? mobStateComponent = null,DamageableComponent? damageableComponent = null)
    {
        if (!EnsureComps(target, mobStateComponent, thresholdComponent, damageableComponent))
            return;
        CheckThresholds(target, mobStateComponent, thresholdComponent, damageableComponent);
    }

    private void CheckThresholds(EntityUid target, MobStateComponent mobStateComponent, MobThresholdComponent thresholdComponent, DamageableComponent damageableComponent)
    {
        foreach (var (threshold,mobState) in thresholdComponent.Thresholds)
        {
            if (damageableComponent.TotalDamage > threshold)
                continue;
            TriggerThreshold(target, mobStateComponent, thresholdComponent,thresholdComponent.CurrentThresholdState, mobState);
        }
    }

    private void TriggerThreshold(EntityUid uid, MobStateComponent mobStateComponent, MobThresholdComponent thresholdComponent,
        MobState.MobState oldMobState, MobState.MobState newMobState)
    {
        if (oldMobState == newMobState)
            return;
        _mobStateSystem.ReturnMobStateTicket(uid, oldMobState, mobStateComponent);
        thresholdComponent.CurrentThresholdState = newMobState;
        Dirty(thresholdComponent);
        _mobStateSystem.TakeMobStateTicket(uid, newMobState, mobStateComponent);

    }

    private bool EnsureComps(EntityUid target, [NotNullWhen(true)] MobStateComponent? mobStateComponent, [NotNullWhen(true)] MobThresholdComponent? thresholdComponent,
        [NotNullWhen(true)] DamageableComponent? damageableComponent)
    {
        if (Resolve(target, ref thresholdComponent)&& Resolve(target, ref mobStateComponent) && Resolve(target, ref damageableComponent))
            return true;
        Logger.Error("Target entity does not have damageable or mobThreshold components!");
        return false;
    }
}
