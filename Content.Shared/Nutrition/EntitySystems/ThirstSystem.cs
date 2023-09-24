using Content.Shared.Alert;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Nutrition.EntitySystems;

[UsedImplicitly]
public sealed class ThirstSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("thirst");

        SubscribeLocalEvent<ThirstComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<ThirstComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ThirstComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ThirstComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnComponentStartup(EntityUid uid, ThirstComponent component, ComponentStartup args)
    {
        // Do not change behavior unless starting value is explicitly defined
        if (component.CurrentThirst < 0)
        {
            component.CurrentThirst = _random.Next(
                (int) component.ThirstThresholds[ThirstThreshold.Thirsty] + 10,
                (int) component.ThirstThresholds[ThirstThreshold.Okay] - 1);
        }
        component.CurrentThirstThreshold = GetThirstThreshold(component, component.CurrentThirst);
        component.LastThirstThreshold = ThirstThreshold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
        // TODO: Check all thresholds make sense and throw if they don't.
        UpdateEffects(uid, component);
    }

    private void OnRefreshMovespeed(EntityUid uid, ThirstComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        // TODO: This should really be taken care of somewhere else
        if (_jetpack.IsUserFlying(uid))
            return;

        var mod = component.CurrentThirstThreshold <= ThirstThreshold.Parched ? 0.75f : 1.0f;
        args.ModifySpeed(mod, mod);
    }

    private void OnRejuvenate(EntityUid uid, ThirstComponent component, RejuvenateEvent args)
    {
        ResetThirst(component);
    }

    private ThirstThreshold GetThirstThreshold(ThirstComponent component, float amount)
    {
        ThirstThreshold result = ThirstThreshold.Dead;
        var value = component.ThirstThresholds[ThirstThreshold.OverHydrated];
        foreach (var threshold in component.ThirstThresholds)
        {
            if (threshold.Value <= value && threshold.Value >= amount)
            {
                result = threshold.Key;
                value = threshold.Value;
            }
        }

        return result;
    }

    public void UpdateThirst(ThirstComponent component, float amount)
    {
        component.CurrentThirst = Math.Clamp(component.CurrentThirst + amount, component.ThirstThresholds[ThirstThreshold.Dead], component.ThirstThresholds[ThirstThreshold.OverHydrated]);
    }

    public void ResetThirst(ThirstComponent component)
    {
        component.CurrentThirst = component.ThirstThresholds[ThirstThreshold.Okay];
    }

    private bool IsMovementThreshold(ThirstThreshold threshold)
    {
        switch (threshold)
        {
            case ThirstThreshold.Dead:
            case ThirstThreshold.Parched:
                return true;
            case ThirstThreshold.Thirsty:
            case ThirstThreshold.Okay:
            case ThirstThreshold.OverHydrated:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(threshold), threshold, null);
        }
    }

    private void UpdateEffects(EntityUid uid, ThirstComponent component)
    {
        if (IsMovementThreshold(component.LastThirstThreshold) != IsMovementThreshold(component.CurrentThirstThreshold) &&
                TryComp(uid, out MovementSpeedModifierComponent? movementSlowdownComponent))
        {
            _movement.RefreshMovementSpeedModifiers(uid, movementSlowdownComponent);
        }

        // Update UI
        if (ThirstComponent.ThirstThresholdAlertTypes.TryGetValue(component.CurrentThirstThreshold, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, AlertCategory.Thirst);
        }

        switch (component.CurrentThirstThreshold)
        {
            case ThirstThreshold.OverHydrated:
                component.LastThirstThreshold = component.CurrentThirstThreshold;
                component.ActualDecayRate = component.BaseDecayRate * 1.2f;
                return;

            case ThirstThreshold.Okay:
                component.LastThirstThreshold = component.CurrentThirstThreshold;
                component.ActualDecayRate = component.BaseDecayRate;
                return;

            case ThirstThreshold.Thirsty:
                // Same as okay except with UI icon saying drink soon.
                component.LastThirstThreshold = component.CurrentThirstThreshold;
                component.ActualDecayRate = component.BaseDecayRate * 0.8f;
                return;
            case ThirstThreshold.Parched:
                _movement.RefreshMovementSpeedModifiers(uid);
                component.LastThirstThreshold = component.CurrentThirstThreshold;
                component.ActualDecayRate = component.BaseDecayRate * 0.6f;
                return;

            case ThirstThreshold.Dead:
                return;

            default:
                _sawmill.Error($"No thirst threshold found for {component.CurrentThirstThreshold}");
                throw new ArgumentOutOfRangeException($"No thirst threshold found for {component.CurrentThirstThreshold}");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ThirstComponent>();
        while (query.MoveNext(out var uid, out var thirst))
        {
            if (_timing.CurTime < thirst.NextUpdateTime)
                continue;

            thirst.NextUpdateTime += thirst.UpdateRate;

            UpdateThirst(thirst, -thirst.ActualDecayRate);
            var calculatedThirstThreshold = GetThirstThreshold(thirst, thirst.CurrentThirst);

            if (calculatedThirstThreshold == thirst.CurrentThirstThreshold)
                continue;

            thirst.CurrentThirstThreshold = calculatedThirstThreshold;
            UpdateEffects(uid, thirst);
            Dirty(uid, thirst);
        }
    }

    private void OnUnpaused(EntityUid uid, ThirstComponent component, ref EntityUnpausedEvent args)
    {
        component.NextUpdateTime += args.PausedTime;
    }
}
