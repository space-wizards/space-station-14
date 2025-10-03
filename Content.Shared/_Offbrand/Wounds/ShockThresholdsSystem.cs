using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class ShockThresholdsSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockThresholdsComponent, AfterShockChangeEvent>(OnAfterShockChange);
        SubscribeLocalEvent<ShockThresholdsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShockThresholdsComponent, UpdateMobStateEvent>(OnUpdateMobState);
    }

    private void OnShutdown(Entity<ShockThresholdsComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.CurrentThresholdState is { } effect)
            _statusEffects.TryRemoveStatusEffect(ent, effect);
    }

    private void UpdateEffects(Entity<ShockThresholdsComponent> ent, FixedPoint2 shock)
    {
        var targetEffect = ent.Comp.Thresholds.HighestMatch(shock);
        if (targetEffect == ent.Comp.CurrentThresholdState)
            return;

        var seenTarget = targetEffect is null;
        if (ent.Comp.CurrentThresholdState is { } oldEffect)
            _statusEffects.TryRemoveStatusEffect(ent, oldEffect);

        if (targetEffect is { } effect)
            _statusEffects.TryUpdateStatusEffectDuration(ent, effect, out _);

        ent.Comp.CurrentThresholdState = targetEffect;
        Dirty(ent);
    }

    private void UpdateState(Entity<ShockThresholdsComponent> ent, FixedPoint2 shock)
    {
        var state = ent.Comp.MobThresholds.HighestMatch(shock) ?? MobState.Alive;

        if (state == ent.Comp.CurrentMobState)
            return;

        ent.Comp.CurrentMobState = state;
        Dirty(ent);
        _mobState.UpdateMobState(ent);
    }

    private void OnAfterShockChange(Entity<ShockThresholdsComponent> ent, ref AfterShockChangeEvent args)
    {
        var shock = _pain.GetShock(ent.Owner);
        UpdateEffects(ent, shock);
        UpdateState(ent, shock);
    }

    public bool IsCritical(Entity<ShockThresholdsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.CurrentThresholdState == ent.Comp.Thresholds.Last().Value;
    }

    private void OnUpdateMobState(Entity<ShockThresholdsComponent> ent, ref UpdateMobStateEvent args)
    {
        args.State = ThresholdHelpers.Max(ent.Comp.CurrentMobState, args.State);

        var overlays = new bPotentiallyUpdateDamageOverlayEventb(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }
}
