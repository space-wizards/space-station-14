using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Mobs.Systems;

public sealed class MobThresholdSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem= default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MobThresholdsComponent, ComponentStartup>(MobThresholdStartup);
        SubscribeLocalEvent<MobThresholdsComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<MobThresholdsComponent, ComponentGetState>(OnGetComponentState);
        SubscribeLocalEvent<MobThresholdsComponent, ComponentHandleState>(OnHandleComponentState);
    }
    private void OnHandleComponentState(EntityUid uid, MobThresholdsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MobThresholdComponentState state)
            return;
        component.Thresholds = new SortedDictionary<FixedPoint2, MobState>(state.Thresholds);
        component.CurrentThresholdState = state.CurrentThresholdState;
    }

    private void OnGetComponentState(EntityUid uid, MobThresholdsComponent component, ref ComponentGetState args)
    {
        args.State = new MobThresholdComponentState(component.CurrentThresholdState, new Dictionary<FixedPoint2, MobState>(component.Thresholds));
    }

    private void MobThresholdStartup(EntityUid uid, MobThresholdsComponent component, ComponentStartup args)
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
        TriggerThreshold(uid, mobStateComp, component, MobState.Invalid, component.Thresholds.First().Value);
    }

    public FixedPoint2 GetThresholdForState(EntityUid uid, MobState mobState, MobThresholdsComponent? thresholdComponent)
    {
        if (!Resolve(uid, ref thresholdComponent) || !thresholdComponent.ThresholdReverseLookup.TryGetValue(mobState, out var threshold))
        {
            return FixedPoint2.Zero;
        }
        return threshold;
    }

    public bool TryGetThresholdForState(EntityUid uid, MobState mobState,  [NotNullWhen(true)] out FixedPoint2? threshold,
        MobThresholdsComponent? thresholdComponent = null)
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

        if (!TryComp<MobThresholdsComponent>(ent1, out var threshold1) ||
            !TryComp<MobThresholdsComponent>(ent2, out var threshold2))
            return false;

        if (!TryGetThresholdForState(ent1, MobState.Dead, out var ent1DeadThreshold, threshold1))
        {
            ent1DeadThreshold = 0;
        }
        if (!TryGetThresholdForState(ent2, MobState.Dead, out var ent2DeadThreshold, threshold2))
        {
            ent2DeadThreshold = 0;
        }

        damage = (oldDamage.Damage / ent1DeadThreshold.Value) * ent2DeadThreshold.Value;
        return true;
    }

    public void SetMobStateThreshold(EntityUid uid, FixedPoint2 damage, MobState mobState,
        MobThresholdsComponent? thresholdComponent = null)
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

    private void OnDamaged(EntityUid uid, MobThresholdsComponent mobThresholdsComponent, DamageChangedEvent args)
    {
        var mobStateComp = EnsureComp<MobStateComponent>(uid);
        CheckThresholds(uid, mobStateComp, mobThresholdsComponent, args.Damageable);

    }

    //Call this if you are somehow change the amount of damage on damageable without triggering a damageChangedEvent
    public void VerifyThresholds(EntityUid target, MobThresholdsComponent? thresholdComponent = null,
        MobStateComponent? mobStateComponent = null,DamageableComponent? damageableComponent = null)
    {
        if (!EnsureComps(target, mobStateComponent, thresholdComponent, damageableComponent))
            return;
        CheckThresholds(target, mobStateComponent, thresholdComponent, damageableComponent);
    }

    private void CheckThresholds(EntityUid target, MobStateComponent mobStateComponent, MobThresholdsComponent thresholdsComponent, DamageableComponent damageableComponent)
    {
        foreach (var (threshold,mobState) in thresholdsComponent.Thresholds)
        {
            if (damageableComponent.TotalDamage > threshold)
                continue;
            TriggerThreshold(target, mobStateComponent, thresholdsComponent,thresholdsComponent.CurrentThresholdState, mobState);
            var ev = new MobThresholdUpdatedEvent(mobState, damageableComponent.TotalDamage, threshold);
            RaiseLocalEvent(ref ev);
        }
    }

    private void TriggerThreshold(EntityUid uid, MobStateComponent mobStateComponent, MobThresholdsComponent thresholdsComponent,
        MobState oldMobState, MobState newMobState)
    {
        if (oldMobState == newMobState)
            return;
        _mobStateSystem.ReturnMobStateTicket(uid, oldMobState, mobStateComponent);
        thresholdsComponent.CurrentThresholdState = newMobState;
        Dirty(thresholdsComponent);
        _mobStateSystem.TakeMobStateTicket(uid, newMobState, mobStateComponent);

    }

    private bool EnsureComps(EntityUid target, [NotNullWhen(true)] MobStateComponent? mobStateComponent, [NotNullWhen(true)] MobThresholdsComponent? thresholdComponent,
        [NotNullWhen(true)] DamageableComponent? damageableComponent)
    {
        if (Resolve(target, ref thresholdComponent)&& Resolve(target, ref mobStateComponent) && Resolve(target, ref damageableComponent))
            return true;
        Logger.Error("Target entity does not have damageable or mobThreshold components!");
        return false;
    }
}

public record struct MobThresholdUpdatedEvent(MobState MobState, FixedPoint2 Damage, FixedPoint2 Threshold);
