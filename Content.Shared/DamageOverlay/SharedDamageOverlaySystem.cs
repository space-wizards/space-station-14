using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;

namespace Content.Shared.DamageOverlay;

public abstract partial class SharedDamageOverlaySystem : EntitySystem
{
    [Dependency] private MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<DamageOverlayComponent> entity, ref ComponentInit args)
    {
        UpdateOverlays(entity);
        RefreshOverlay(entity);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<DamageOverlayComponent> entity, ref MobStateChangedEvent args)
    {
        if (entity.Comp.Locked)
            return;

        UpdateOverlays(entity, args.Component);
    }

    [SubscribeLocalEvent]
    private void OnThresholdCheck(Entity<DamageOverlayComponent> entity, ref MobThresholdChecked args)
    {
        if (entity.Comp.Locked)
            return;

        UpdateOverlays(entity, args.MobState, args.Damageable, args.Threshold);
    }

    protected void ClearOverlay(Entity<DamageOverlayComponent> entity)
    {
        entity.Comp.State = MobState.Alive;
        entity.Comp.DeadLevel = 0f;
        entity.Comp.CritLevel = 0f;
        entity.Comp.PainLevel = 0f;
        entity.Comp.OxygenLevel = 0f;
        Dirty(entity);
    }

    //TODO: Jezi: adjust oxygen and hp overlays to use appropriate systems once bodysim is implemented
    protected void UpdateOverlays(Entity<DamageOverlayComponent> entity,
        MobStateComponent? mobState = null,
        DamageableComponent? damageable = null,
        MobThresholdsComponent? thresholds = null,
        InjurableComponent? injurable = null)
    {
        if (mobState == null && !TryComp(entity, out mobState) ||
            thresholds == null && !TryComp(entity, out thresholds) ||
            damageable == null && !TryComp(entity, out damageable) ||
            injurable == null && !TryComp(entity, out injurable))
            return;

        if (!_mobThresholdSystem.TryGetIncapThreshold(entity, out var foundThreshold, thresholds))
            return; //this entity cannot die or crit!!

        if (!thresholds.ShowOverlays)
        {
            ClearOverlay(entity);
            return; //this entity intentionally has no overlays
        }

        var damagePerGroup = _damageable.GetDamagePerGroup((entity, damageable));
        var critThreshold = foundThreshold.Value;
        entity.Comp.State = mobState.CurrentState;

        switch (mobState.CurrentState)
        {
            case MobState.Alive:
            {
                FixedPoint2 painLevel = 0;
                entity.Comp.PainLevel = 0;

                if (!_statusEffects.TryEffectsWithComp<PainNumbnessStatusEffectComponent>(entity, out _))
                {
                    foreach (var painDamageType in injurable.PainDamageGroups)
                    {

                        damagePerGroup.TryGetValue(painDamageType, out var painDamage);
                        painLevel += painDamage;
                    }

                    entity.Comp.PainLevel = FixedPoint2.Min(1f, painLevel / critThreshold).Float();

                    if (entity.Comp.PainLevel < 0.05f) // Don't show damage overlay if they're near enough to max.
                    {
                        entity.Comp.PainLevel = 0;
                    }
                }

                if (damagePerGroup.TryGetValue("Airloss", out var oxyDamage))
                {
                    entity.Comp.OxygenLevel = FixedPoint2.Min(1f, oxyDamage / critThreshold).Float();
                }

                entity.Comp.CritLevel = 0;
                entity.Comp.DeadLevel = 0;
                break;
            }
            case MobState.Critical:
            {
                if (!_mobThresholdSystem.TryGetDeadPercentage(entity,
                        FixedPoint2.Max(0.0, _damageable.GetTotalDamage((entity, damageable))),
                        out var critLevel))
                    return;
                entity.Comp.CritLevel = critLevel.Value.Float();

                entity.Comp.PainLevel = 0;
                entity.Comp.DeadLevel = 0;
                break;
            }
            case MobState.Dead:
            {
                entity.Comp.PainLevel = 0;
                entity.Comp.CritLevel = 0;
                break;
            }
        }

        if (!entity.Comp.Locked)
        {
            Dirty(entity);
            RefreshOverlay(entity);
        }
    }

    /// <summary>
    /// Refreshes the overlay, updating it to the new values stored in the component.
    /// </summary>
    /// <param name="entity">The affected entity.</param>
    protected virtual void RefreshOverlay(Entity<DamageOverlayComponent> entity) { }
}
