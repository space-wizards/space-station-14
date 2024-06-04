using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class HungerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    [ValidatePrototypeId<SatiationIconPrototype>]
    private const string HungerIconOverfedId = "HungerIconOverfed";

    [ValidatePrototypeId<SatiationIconPrototype>]
    private const string HungerIconPeckishId = "HungerIconPeckish";

    [ValidatePrototypeId<SatiationIconPrototype>]
    private const string HungerIconStarvingId = "HungerIconStarving";

    private SatiationIconPrototype? _hungerIconOverfed;
    private SatiationIconPrototype? _hungerIconPeckish;
    private SatiationIconPrototype? _hungerIconStarving;

    public override void Initialize()
    {
        base.Initialize();

        DebugTools.Assert(_prototype.TryIndex(HungerIconOverfedId, out _hungerIconOverfed) &&
                          _prototype.TryIndex(HungerIconPeckishId, out _hungerIconPeckish) &&
                          _prototype.TryIndex(HungerIconStarvingId, out _hungerIconStarving));

        SubscribeLocalEvent<HungerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HungerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HungerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<HungerComponent, RejuvenateEvent>(OnRejuvenate);
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
        _alerts.ClearAlertCategory(uid, component.HungerAlertCategory);
    }

    private void OnRefreshMovespeed(EntityUid uid, HungerComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.CurrentThreshold > HungerThreshold.Starving)
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
        Dirty(uid, component);
    }

    private void UpdateCurrentThreshold(EntityUid uid, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var calculatedHungerThreshold = GetHungerThreshold(component);
        if (calculatedHungerThreshold == component.CurrentThreshold)
            return;
        component.CurrentThreshold = calculatedHungerThreshold;
        DoHungerThresholdEffects(uid, component);
        Dirty(uid, component);
    }

    private void DoHungerThresholdEffects(EntityUid uid, HungerComponent? component = null, bool force = false)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.CurrentThreshold == component.LastThreshold && !force)
            return;

        if (GetMovementThreshold(component.CurrentThreshold) != GetMovementThreshold(component.LastThreshold))
        {
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        }

        if (component.HungerThresholdAlerts.TryGetValue(component.CurrentThreshold, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, component.HungerAlertCategory);
        }

        if (component.HungerThresholdDecayModifiers.TryGetValue(component.CurrentThreshold, out var modifier))
        {
            component.ActualDecayRate = component.BaseDecayRate * modifier;
        }

        component.LastThreshold = component.CurrentThreshold;
    }

    private void DoContinuousHungerEffects(EntityUid uid, HungerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.CurrentThreshold <= HungerThreshold.Starving &&
            component.StarvationDamage is { } damage &&
            !_mobState.IsDead(uid))
        {
            _damageable.TryChangeDamage(uid, damage, true, false);
        }
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

    /// <summary>
    /// A check that returns if the entity is below a hunger threshold.
    /// </summary>
    public bool IsHungerBelowState(EntityUid uid, HungerThreshold threshold, float? food = null, HungerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false; // It's never going to go hungry, so it's probably fine to assume that it's not... you know, hungry.

        return GetHungerThreshold(comp, food) < threshold;
    }

    private bool GetMovementThreshold(HungerThreshold threshold)
    {
        switch (threshold)
        {
            case HungerThreshold.Overfed:
            case HungerThreshold.Okay:
                return true;
            case HungerThreshold.Peckish:
            case HungerThreshold.Starving:
            case HungerThreshold.Dead:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(threshold), threshold, null);
        }
    }

    public bool TryGetStatusIconPrototype(HungerComponent component, [NotNullWhen(true)] out SatiationIconPrototype? prototype)
    {
        switch (component.CurrentThreshold)
        {
            case HungerThreshold.Overfed:
                prototype = _hungerIconOverfed;
                break;
            case HungerThreshold.Peckish:
                prototype = _hungerIconPeckish;
                break;
            case HungerThreshold.Starving:
                prototype = _hungerIconStarving;
                break;
            default:
                prototype = null;
                break;
        }

        return prototype != null;
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
            DoContinuousHungerEffects(uid, hunger);
        }
    }
}

