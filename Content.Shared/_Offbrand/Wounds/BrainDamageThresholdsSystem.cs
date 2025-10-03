using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class BrainDamageThresholdsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainDamageThresholdsComponent, AfterBrainDamageChanged>(OnAfterBrainDamageChanged);
        SubscribeLocalEvent<BrainDamageThresholdsComponent, AfterBrainOxygenChanged>(OnAfterBrainOxygenChanged);
        SubscribeLocalEvent<BrainDamageThresholdsComponent, UpdateMobStateEvent>(OnUpdateMobState);
        SubscribeLocalEvent<BrainDamageThresholdsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<BrainDamageThresholdsComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.CurrentDamageEffect is { } dEffect)
            _statusEffects.TryRemoveStatusEffect(ent, dEffect);

        if (ent.Comp.CurrentOxygenEffect is { } oEffect)
            _statusEffects.TryRemoveStatusEffect(ent, oEffect);
    }

    private void UpdateState(Entity<BrainDamageThresholdsComponent> ent)
    {
        var brain = Comp<BrainDamageComponent>(ent);

        var damageState = ent.Comp.DamageStateThresholds.HighestMatch(brain.Damage) ?? MobState.Alive;
        var oxygenState = ent.Comp.OxygenStateThresholds.LowestMatch(brain.Oxygen) ?? MobState.Alive;

        var state = ThresholdHelpers.Max(damageState, oxygenState);

        if (state == ent.Comp.CurrentState)
            return;

        ent.Comp.CurrentState = state;
        Dirty(ent);
        _mobState.UpdateMobState(ent);
    }

    private void OnAfterBrainDamageChanged(Entity<BrainDamageThresholdsComponent> ent, ref AfterBrainDamageChanged args)
    {
        var brain = Comp<BrainDamageComponent>(ent);

        UpdateState(ent);
        UpdateAlert((ent.Owner, ent.Comp, brain));

        var damageEffect = ent.Comp.DamageEffectThresholds.HighestMatch(brain.Damage);
        if (damageEffect == ent.Comp.CurrentDamageEffect)
            return;

        if (ent.Comp.CurrentDamageEffect is { } oldEffect)
            _statusEffects.TryRemoveStatusEffect(ent, oldEffect);

        if (damageEffect is { } newEffect)
            _statusEffects.TryUpdateStatusEffectDuration(ent, newEffect, out _);

        ent.Comp.CurrentDamageEffect = damageEffect;
        Dirty(ent);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void OnAfterBrainOxygenChanged(Entity<BrainDamageThresholdsComponent> ent, ref AfterBrainOxygenChanged args)
    {
        var brain = Comp<BrainDamageComponent>(ent);

        UpdateState(ent);

        var oxygenEffect = ent.Comp.OxygenEffectThresholds.LowestMatch(brain.Oxygen);
        if (oxygenEffect == ent.Comp.CurrentOxygenEffect)
            return;

        if (ent.Comp.CurrentOxygenEffect is { } oldEffect)
            _statusEffects.TryRemoveStatusEffect(ent, oldEffect);

        if (oxygenEffect is { } newEffect)
            _statusEffects.TryUpdateStatusEffectDuration(ent, newEffect, out _);

        ent.Comp.CurrentOxygenEffect = oxygenEffect;
        Dirty(ent);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void UpdateAlert(Entity<BrainDamageThresholdsComponent, BrainDamageComponent> ent)
    {
        var targetEffect = ent.Comp1.DamageAlertThresholds.HighestMatch(ent.Comp2.Damage);
        if (targetEffect == ent.Comp1.CurrentDamageAlertThresholdState)
            return;

        ent.Comp1.CurrentDamageAlertThresholdState = targetEffect;
        Dirty(ent);

        if (targetEffect is { } effect)
        {
            _alerts.ShowAlert(ent.Owner, effect);
        }
        else
        {
            _alerts.ClearAlertCategory(ent.Owner, ent.Comp1.DamageAlertCategory);
        }
    }

    private void OnUpdateMobState(Entity<BrainDamageThresholdsComponent> ent, ref UpdateMobStateEvent args)
    {
        args.State = ThresholdHelpers.Max(ent.Comp.CurrentState, args.State);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }
}
