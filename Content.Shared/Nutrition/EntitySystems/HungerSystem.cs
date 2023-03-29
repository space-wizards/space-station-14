using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class HungerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HungerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<HungerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<HungerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HungerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HungerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<HungerComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnGetState(EntityUid uid, HungerComponent component, ref ComponentGetState args)
    {
        args.State = new HungerComponentState(component.CurrentHunger,
            component.BaseDecayRate,
            component.ActualDecayRate,
            component.LastHungerThreshold,
            component.CurrentHungerThreshold,
            component.Thresholds,
            component.HungerThresholdAlerts,
            component.StarvingSlowdownModifier,
            component.NextUpdateTime,
            component.UpdateRate);
    }

    private void OnHandleState(EntityUid uid, HungerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not HungerComponentState state)
            return;
        component.CurrentHunger = state.CurrentHunger;
        component.BaseDecayRate = state.BaseDecayRate;
        component.ActualDecayRate = state.ActualDecayRate;
        component.LastHungerThreshold = state.LastHungerThreshold;
        component.CurrentHungerThreshold = state.CurrentThreshold;
        component.Thresholds = new(state.HungerThresholds);
        component.HungerThresholdAlerts = new(state.HungerAlertThresholds);
        component.StarvingSlowdownModifier = state.StarvingSlowdownModifier;
        component.NextUpdateTime = state.NextUpdateTime;
        component.UpdateRate = state.UpdateRate;
    }

    private void OnMapInit(EntityUid uid, HungerComponent component, MapInitEvent args)
    {
        var amount = _random.Next(
            (int) component.Thresholds[HungerThreshold.Peckish] + 10,
            (int) component.Thresholds[HungerThreshold.Okay]);
        SetHunger(uid, amount, component);
    }

    private void OnShutdown(EntityUid uid, HungerComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(uid, AlertCategory.Hunger);
    }

    private void OnRefreshMovespeed(EntityUid uid, HungerComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.CurrentHungerThreshold > HungerThreshold.Starving)
            return;

        if (_jetpack.IsUserFlying(uid))
            return;

        args.ModifySpeed(component.StarvingSlowdownModifier, component.StarvingSlowdownModifier);
    }

    private void OnRejuvenate(EntityUid uid, HungerComponent component, RejuvenateEvent args)
    {
        SetHunger(uid, component.Thresholds[HungerThreshold.Okay], component);
    }

    /// <summary>
    /// Adds to the current hunger of an entity by the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    public void ModifyHunger(EntityUid uid, float amount, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        SetHunger(uid, component.CurrentHunger + amount, component);
    }

    /// <summary>
    /// Sets the current hunger of an entity to the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    public void SetHunger(EntityUid uid, float amount, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        component.CurrentHunger = Math.Clamp(amount,
            component.Thresholds[HungerThreshold.Dead],
            component.Thresholds[HungerThreshold.Overfed]);
        UpdateCurrentThreshold(uid, component);
        Dirty(component);
    }

    private void UpdateCurrentThreshold(EntityUid uid, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var calculatedHungerThreshold = GetHungerThreshold(component);
        if (calculatedHungerThreshold == component.CurrentHungerThreshold)
            return;
        component.CurrentHungerThreshold = calculatedHungerThreshold;
        DoHungerThresholdEffects(uid, component);
        Dirty(component);
    }

    private void DoHungerThresholdEffects(EntityUid uid, HungerComponent? component = null, bool force = false)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.CurrentHungerThreshold == component.LastHungerThreshold && !force)
            return;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

        if (component.HungerThresholdAlerts.TryGetValue(component.CurrentHungerThreshold, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, AlertCategory.Hunger);
        }

        if (component.StarvationDamage is { } damage && !_mobState.IsDead(uid))
        {
            _damageable.TryChangeDamage(uid, damage, true, false);
        }

        component.LastHungerThreshold = component.CurrentHungerThreshold;
        if (component.HungerThresholdDecayModifiers.TryGetValue(component.CurrentHungerThreshold, out var modifier))
            component.ActualDecayRate = component.BaseDecayRate * modifier;
    }

    /// <summary>
    /// Gets the hunger threshold for an entity based on the amount of food specified.
    /// If a specific amount isn't specified, just uses the current hunger of the entity
    /// </summary>
    /// <param name="component"></param>
    /// <param name="food"></param>
    /// <returns></returns>
    public HungerThreshold GetHungerThreshold(HungerComponent component, float? food = null)
    {
        food ??= component.CurrentHunger;
        var result = HungerThreshold.Dead;
        var value = component.Thresholds[HungerThreshold.Overfed];
        foreach (var threshold in component.Thresholds)
        {
            if (threshold.Value <= value && threshold.Value >= food)
            {
                result = threshold.Key;
                value = threshold.Value;
            }
        }
        return result;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HungerComponent>();
        while (query.MoveNext(out var uid, out var hunger))
        {
            if (_timing.CurTime < hunger.NextUpdateTime)
                continue;
            hunger.NextUpdateTime = _timing.CurTime + hunger.UpdateRate;

            ModifyHunger(uid, -hunger.ActualDecayRate, hunger);
        }
    }
}

