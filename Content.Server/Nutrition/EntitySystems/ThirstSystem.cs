using Content.Server.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Alert;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Damage;
using Content.Shared.Movement.Systems;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class ThirstSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly DamageableSystem _damage = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

        private ISawmill _sawmill = default!;
        private float _accumulatedFrameTime;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("thirst");
            SubscribeLocalEvent<ThirstComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
            SubscribeLocalEvent<ThirstComponent, ComponentStartup>(OnComponentStartup);
        }
        private void OnComponentStartup(EntityUid uid, ThirstComponent component, ComponentStartup args)
        {
            component.CurrentThirst = _random.Next(
                (int) component.ThirstThresholds[ThirstThreshold.Thirsty] + 10,
                (int) component.ThirstThresholds[ThirstThreshold.Okay] - 1);
            component.CurrentThirstThreshold = GetThirstThreshold(component, component.CurrentThirst);
            component.LastThirstThreshold = ThirstThreshold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
            // TODO: Check all thresholds make sense and throw if they don't.
            UpdateEffects(component);
        }

        private void OnRefreshMovespeed(EntityUid uid, ThirstComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            var mod = (component.CurrentThirstThreshold & (ThirstThreshold.Parched | ThirstThreshold.Dead)) != 0x0 ? 0.75f : 1.0f;
            args.ModifySpeed(mod, mod);
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
            component.CurrentThirst = Math.Min(component.CurrentThirst + amount, component.ThirstThresholds[ThirstThreshold.OverHydrated]);
        }

        public void ResetThirst(ThirstComponent component)
        {
            component.CurrentThirst = component.ThirstThresholds[ThirstThreshold.Okay];
        }

        private void UpdateEffects(ThirstComponent component)
        {
            if (component.LastThirstThreshold == ThirstThreshold.Parched && component.CurrentThirstThreshold != ThirstThreshold.Dead &&
                    EntityManager.TryGetComponent(component.Owner, out MovementSpeedModifierComponent? movementSlowdownComponent))
            {
                _movement.RefreshMovementSpeedModifiers(component.Owner);
            }

            // Update UI
            if (ThirstComponent.ThirstThresholdAlertTypes.TryGetValue(component.CurrentThirstThreshold, out var alertId))
            {
                _alerts.ShowAlert(component.Owner, alertId);
            }
            else
            {
                _alerts.ClearAlertCategory(component.Owner, AlertCategory.Thirst);
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
                    _movement.RefreshMovementSpeedModifiers(component.Owner);
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
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime > 1)
            {
                foreach (var component in EntityManager.EntityQuery<ThirstComponent>())
                {
                    component.CurrentThirst -= component.ActualDecayRate;
                    var calculatedThirstThreshold = GetThirstThreshold(component, component.CurrentThirst);
                    if (calculatedThirstThreshold != component.CurrentThirstThreshold)
                    {
                        component.CurrentThirstThreshold = calculatedThirstThreshold;
                        UpdateEffects(component);
                    }
                }
                _accumulatedFrameTime -= 1;
            }
        }
    }
}
