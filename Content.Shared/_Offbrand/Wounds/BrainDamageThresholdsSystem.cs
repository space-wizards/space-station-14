using Content.Shared._Offbrand.Organs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class BrainDamageThresholdsSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

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

    public void UpdateState(Entity<BrainDamageThresholdsComponent?> ent, Entity<DamageableOrganComponent, OxygenatableOrganComponent>? organ)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (organ is { } brain)
        {
            ent.Comp.DisplayDamage = brain.Comp1.Damage;
            ent.Comp.DisplayMaxDamage = brain.Comp1.MaxDamage;
            ent.Comp.DisplayOxygen = brain.Comp2.Oxygen;
            Dirty(ent);
        }

        var damageState = organ?.Comp1.Damage is { } damage
            ? ent.Comp.DamageStateThresholds.HighestMatch(damage) ?? MobState.Alive
            : MobState.Dead;
        var oxygenState = organ?.Comp2.Oxygen is { } oxygen
            ? ent.Comp.OxygenStateThresholds.LowestMatch(oxygen) ?? MobState.Alive
            : MobState.Dead;

        var state = ThresholdHelpers.Max(damageState, oxygenState);

        if (state == ent.Comp.CurrentState)
            return;

        ent.Comp.CurrentState = state;
        Dirty(ent);
        _mobState.UpdateMobState(ent);
    }

    public void OnAfterBrainDamageChanged(Entity<BrainDamageThresholdsComponent?> ent, Entity<DamageableOrganComponent, OxygenatableOrganComponent> organ)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        UpdateState((ent, ent.Comp), organ);

        var damageEffect = ent.Comp.DamageEffectThresholds.HighestMatch(organ.Comp1.Damage);
        if (damageEffect == ent.Comp.CurrentDamageEffect)
            return;

        if (ent.Comp.CurrentDamageEffect is { } oldEffect)
            _statusEffects.TryRemoveStatusEffect(ent, oldEffect);

        if (damageEffect is { } newEffect)
            _statusEffects.TryUpdateStatusEffectDuration(ent, newEffect, out _);

        ent.Comp.CurrentDamageEffect = damageEffect;
        Dirty(ent);

        var overlays = new PotentiallyUpdateDamageOverlayEvent(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    public void OnAfterBrainOxygenChanged(Entity<BrainDamageThresholdsComponent?> ent, Entity<DamageableOrganComponent, OxygenatableOrganComponent> organ)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        UpdateState((ent, ent.Comp), organ);

        var oxygenEffect = ent.Comp.OxygenEffectThresholds.LowestMatch(organ.Comp2.Oxygen);
        if (oxygenEffect == ent.Comp.CurrentOxygenEffect)
            return;

        if (ent.Comp.CurrentOxygenEffect is { } oldEffect)
            _statusEffects.TryRemoveStatusEffect(ent, oldEffect);

        if (oxygenEffect is { } newEffect)
            _statusEffects.TryUpdateStatusEffectDuration(ent, newEffect, out _);

        ent.Comp.CurrentOxygenEffect = oxygenEffect;
        Dirty(ent);

        var overlays = new PotentiallyUpdateDamageOverlayEvent(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void OnUpdateMobState(Entity<BrainDamageThresholdsComponent> ent, ref UpdateMobStateEvent args)
    {
        args.State = ThresholdHelpers.Max(ent.Comp.CurrentState, args.State);

        var overlays = new PotentiallyUpdateDamageOverlayEvent(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }
}
