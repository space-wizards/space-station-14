using System;
using System.Collections.Generic;
using Content.Server.Alert;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    public sealed class ThirstComponent : SharedThirstComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private float _accumulatedFrameTime;

        // Base stuff
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseDecayRate
        {
            get => _baseDecayRate;
            set => _baseDecayRate = value;
        }
        [DataField("baseDecayRate")]
        private float _baseDecayRate = 0.1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ActualDecayRate
        {
            get => _actualDecayRate;
            set => _actualDecayRate = value;
        }
        private float _actualDecayRate;

        // Thirst
        [ViewVariables(VVAccess.ReadOnly)]
        public override ThirstThreshold CurrentThirstThreshold => _currentThirstThreshold;
        private ThirstThreshold _currentThirstThreshold;

        private ThirstThreshold _lastThirstThreshold;

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentThirst
        {
            get => _currentThirst;
            set => _currentThirst = value;
        }
        private float _currentThirst;

        [ViewVariables(VVAccess.ReadOnly)]
        public Dictionary<ThirstThreshold, float> ThirstThresholds { get; } = new()
        {
            {ThirstThreshold.OverHydrated, 600.0f},
            {ThirstThreshold.Okay, 450.0f},
            {ThirstThreshold.Thirsty, 300.0f},
            {ThirstThreshold.Parched, 150.0f},
            {ThirstThreshold.Dead, 0.0f},
        };

        public static readonly Dictionary<ThirstThreshold, AlertType> ThirstThresholdAlertTypes = new()
        {
            {ThirstThreshold.OverHydrated, AlertType.Overhydrated},
            {ThirstThreshold.Thirsty, AlertType.Thirsty},
            {ThirstThreshold.Parched, AlertType.Parched},
        };

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        public void ThirstThresholdEffect(bool force = false)
        {
            if (_currentThirstThreshold != _lastThirstThreshold || force)
            {
                // Revert slow speed if required
                if (_lastThirstThreshold == ThirstThreshold.Parched && _currentThirstThreshold != ThirstThreshold.Dead &&
                    Owner.TryGetComponent(out MovementSpeedModifierComponent? movementSlowdownComponent))
                {
                    EntitySystem.Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(OwnerUid);
                }

                // Update UI
                Owner.TryGetComponent(out ServerAlertsComponent? alertsComponent);

                if (ThirstThresholdAlertTypes.TryGetValue(_currentThirstThreshold, out var alertId))
                {
                    alertsComponent?.ShowAlert(alertId);
                }
                else
                {
                    alertsComponent?.ClearAlertCategory(AlertCategory.Thirst);
                }

                switch (_currentThirstThreshold)
                {
                    case ThirstThreshold.OverHydrated:
                        _lastThirstThreshold = _currentThirstThreshold;
                        _actualDecayRate = _baseDecayRate * 1.2f;
                        return;

                    case ThirstThreshold.Okay:
                        _lastThirstThreshold = _currentThirstThreshold;
                        _actualDecayRate = _baseDecayRate;
                        return;

                    case ThirstThreshold.Thirsty:
                        // Same as okay except with UI icon saying drink soon.
                        _lastThirstThreshold = _currentThirstThreshold;
                        _actualDecayRate = _baseDecayRate * 0.8f;
                        return;

                    case ThirstThreshold.Parched:
                        EntitySystem.Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(OwnerUid);
                        _lastThirstThreshold = _currentThirstThreshold;
                        _actualDecayRate = _baseDecayRate * 0.6f;
                        return;

                    case ThirstThreshold.Dead:
                        return;
                    default:
                        Logger.ErrorS("thirst", $"No thirst threshold found for {_currentThirstThreshold}");
                        throw new ArgumentOutOfRangeException($"No thirst threshold found for {_currentThirstThreshold}");
                }
            }
        }

        protected override void Startup()
        {
            base.Startup();
            _currentThirst = _random.Next(
                (int)ThirstThresholds[ThirstThreshold.Thirsty] + 10,
                (int)ThirstThresholds[ThirstThreshold.Okay] - 1);
            _currentThirstThreshold = GetThirstThreshold(_currentThirst);
            _lastThirstThreshold = ThirstThreshold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
            // TODO: Check all thresholds make sense and throw if they don't.
            ThirstThresholdEffect(true);
            Dirty();
        }

        public ThirstThreshold GetThirstThreshold(float drink)
        {
            ThirstThreshold result = ThirstThreshold.Dead;
            var value = ThirstThresholds[ThirstThreshold.OverHydrated];
            foreach (var threshold in ThirstThresholds)
            {
                if (threshold.Value <= value && threshold.Value >= drink)
                {
                    result = threshold.Key;
                    value = threshold.Value;
                }
            }

            return result;
        }

        public void UpdateThirst(float amount)
        {
            _currentThirst = Math.Min(_currentThirst + amount, ThirstThresholds[ThirstThreshold.OverHydrated]);
        }

        // TODO: If mob is moving increase rate of consumption.
        //  Should use a multiplier as something like a disease would overwrite decay rate.
        public void OnUpdate(float frametime)
        {
            _currentThirst -= frametime * ActualDecayRate;
            UpdateCurrentThreshold();

            if (_currentThirstThreshold != ThirstThreshold.Dead)
                return;
            // --> Current Hunger is below dead threshold

            if (!Owner.TryGetComponent(out MobStateComponent? mobState))
                return;

            if (!mobState.IsDead())
            {
                // --> But they are not dead yet.
                _accumulatedFrameTime += frametime;
                if (_accumulatedFrameTime >= 1)
                {
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner.Uid, Damage * (int) _accumulatedFrameTime, true);
                    _accumulatedFrameTime -= (int) _accumulatedFrameTime;
                }
            }
        }

        private void UpdateCurrentThreshold()
        {
            var calculatedThirstThreshold = GetThirstThreshold(_currentThirst);
            // _trySound(calculatedThreshold);
            if (calculatedThirstThreshold != _currentThirstThreshold)
            {
                _currentThirstThreshold = calculatedThirstThreshold;
                ThirstThresholdEffect();
                Dirty();
            }
        }

        public void ResetThirst()
        {
            _currentThirst = ThirstThresholds[ThirstThreshold.Okay];
            UpdateCurrentThreshold();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ThirstComponentState(_currentThirstThreshold);
        }
    }
}
