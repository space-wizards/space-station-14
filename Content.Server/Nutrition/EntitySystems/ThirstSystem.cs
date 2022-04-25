using Content.Server.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Random;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Alert;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Damage;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class ThirstSystem : EntitySystem
    {

        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        private float _accumulatedFrameTime;

        public override void Initialize()
        {
            base.Initialize();

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
            float mod = component.CurrentThirstThreshold == ThirstThreshold.Parched ? 0.75f : 1.0f;
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
                    _entMan.TryGetComponent(component.Owner, out MovementSpeedModifierComponent? movementSlowdownComponent))
            {
                EntitySystem.Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(component.Owner);
            }

            // Update UI
            if (ThirstComponent.ThirstThresholdAlertTypes.TryGetValue(component.CurrentThirstThreshold, out var alertId))
            {
                EntitySystem.Get<AlertsSystem>().ShowAlert(component.Owner, alertId);
            }
            else
            {
                EntitySystem.Get<AlertsSystem>().ClearAlertCategory(component.Owner, AlertCategory.Thirst);
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
                    EntitySystem.Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(component.Owner);
                    component.LastThirstThreshold = component.CurrentThirstThreshold;
                    component.ActualDecayRate = component.BaseDecayRate * 0.6f;
                    return;

                case ThirstThreshold.Dead:
                    return;
                default:
                    Logger.ErrorS("thirst", $"No thirst threshold found for {component.CurrentThirstThreshold}");
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
                        if (component.CurrentThirstThreshold == ThirstThreshold.Dead)
                            EntitySystem.Get<AdminLogSystem>().Add(LogType.Thirst, $"{_entMan.ToPrettyString(component.Owner):entity} has stopped taking dehydration damage");
                        else if (calculatedThirstThreshold == ThirstThreshold.Dead)
                            EntitySystem.Get<AdminLogSystem>().Add(LogType.Thirst, $"{_entMan.ToPrettyString(component.Owner):entity} has started taking dehydration damage");

                        component.CurrentThirstThreshold = calculatedThirstThreshold;
                        UpdateEffects(component);
                    }
                    if (component.CurrentThirstThreshold == ThirstThreshold.Dead)
                    {
                        if (!_entMan.TryGetComponent(component.Owner, out MobStateComponent? mobState))
                            return;

                        if (!mobState.IsDead())
                        {
                            EntitySystem.Get<DamageableSystem>().TryChangeDamage(component.Owner, component.Damage, true);
                        }
                    }
                }
                _accumulatedFrameTime -= 1;
            }
        }
    }
}
