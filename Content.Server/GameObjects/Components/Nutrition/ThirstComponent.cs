using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public sealed class ThirstComponent : Component, IMoveSpeedModifier
    {
#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        public override string Name => "Thirst";

        // Base stuff
        public float BaseDecayRate => _baseDecayRate;
        [ViewVariables] private float _baseDecayRate;
        public float ActualDecayRate => _actualDecayRate;
        [ViewVariables] private float _actualDecayRate;

        // Thirst
        public ThirstThreshold CurrentThirstThreshold => _currentThirstThreshold;
        private ThirstThreshold _currentThirstThreshold;
        private ThirstThreshold _lastThirstThreshold;
        public float CurrentThirst => _currentThirst;
        [ViewVariables] private float _currentThirst;

        public Dictionary<ThirstThreshold, float> ThirstThresholds => _thirstThresholds;
        private Dictionary<ThirstThreshold, float> _thirstThresholds = new Dictionary<ThirstThreshold, float>
        {
            {ThirstThreshold.OverHydrated, 600.0f},
            {ThirstThreshold.Okay, 450.0f},
            {ThirstThreshold.Thirsty, 300.0f},
            {ThirstThreshold.Parched, 150.0f},
            {ThirstThreshold.Dead, 0.0f},
        };

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _baseDecayRate, "base_decay_rate", 0.1f);
        }

        public void ThirstThresholdEffect(bool force = false)
        {
            if (_currentThirstThreshold != _lastThirstThreshold || force)
            {
                Logger.InfoS("thirst", $"Updating Thirst state for {Owner.Name}");

                // Revert slow speed if required
                if (_lastThirstThreshold == ThirstThreshold.Parched && _currentThirstThreshold != ThirstThreshold.Dead &&
                    Owner.TryGetComponent(out MovementSpeedModifierComponent movementSlowdownComponent))
                {
                    movementSlowdownComponent.RefreshMovementSpeedModifiers();
                }

                // Update UI
                Owner.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent);
                statusEffectsComponent?.ChangeStatus(StatusEffect.Thirst, "/Textures/Mob/UI/Thirst/" +
                                                                          _currentThirstThreshold + ".png");

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
                        if (Owner.TryGetComponent(out MovementSpeedModifierComponent movementSlowdownComponent1))
                        {
                            movementSlowdownComponent1.RefreshMovementSpeedModifiers();
                        }
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
                (int)_thirstThresholds[ThirstThreshold.Thirsty] + 10,
                (int)_thirstThresholds[ThirstThreshold.Okay] - 1);
            _currentThirstThreshold = GetThirstThreshold(_currentThirst);
            _lastThirstThreshold = ThirstThreshold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
            // TODO: Check all thresholds make sense and throw if they don't.
            ThirstThresholdEffect(true);
        }

        public ThirstThreshold GetThirstThreshold(float drink)
        {
            ThirstThreshold result = ThirstThreshold.Dead;
            var value = ThirstThresholds[ThirstThreshold.OverHydrated];
            foreach (var threshold in _thirstThresholds)
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
            var calculatedThirstThreshold = GetThirstThreshold(_currentThirst);
            // _trySound(calculatedThreshold);
            if (calculatedThirstThreshold != _currentThirstThreshold)
            {
                _currentThirstThreshold = calculatedThirstThreshold;
                ThirstThresholdEffect();
            }

            if (_currentThirstThreshold == ThirstThreshold.Dead)
            {
                // TODO: Remove from dead people
                if (Owner.TryGetComponent(out DamageableComponent damage))
                {
                    damage.TakeDamage(DamageType.Brute, 2);
                    return;
                }
                return;
            }
        }

        float IMoveSpeedModifier.SprintSpeedModifier
        {
            get
            {
                if (_currentThirstThreshold == ThirstThreshold.Parched)
                {
                    return 0.25f;
                }
                return 1.0f;
            }
        }
        float IMoveSpeedModifier.WalkSpeedModifier
        {
            get
            {
                if (_currentThirstThreshold == ThirstThreshold.Parched)
                {
                    return 0.5f;
                }
                return 1.0f;
            }
        }

        public void ResetThirst()
        {
            _currentThirst = ThirstThresholds[ThirstThreshold.Okay];
        }
    }

    public enum ThirstThreshold
    {
        // Hydrohomies
        OverHydrated,
        Okay,
        Thirsty,
        Parched,
        Dead,
    }
}
