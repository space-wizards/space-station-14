using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public sealed class ThirstComponent : SharedThirstComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        // Base stuff
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseDecayRate
        {
            get => _baseDecayRate;
            set => _baseDecayRate = value;
        }
        private float _baseDecayRate;

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
        public Dictionary<ThirstThreshold, float> ThirstThresholds { get; } = new Dictionary<ThirstThreshold, float>
        {
            {ThirstThreshold.OverHydrated, 600.0f},
            {ThirstThreshold.Okay, 450.0f},
            {ThirstThreshold.Thirsty, 300.0f},
            {ThirstThreshold.Parched, 150.0f},
            {ThirstThreshold.Dead, 0.0f},
        };

        public static readonly Dictionary<ThirstThreshold, string> ThirstThresholdImages = new Dictionary<ThirstThreshold, string>
        {
            {ThirstThreshold.OverHydrated, "/Textures/Interface/StatusEffects/Thirst/OverHydrated.png"},
            {ThirstThreshold.Thirsty, "/Textures/Interface/StatusEffects/Thirst/Thirsty.png"},
            {ThirstThreshold.Parched, "/Textures/Interface/StatusEffects/Thirst/Parched.png"},
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
                // Revert slow speed if required
                if (_lastThirstThreshold == ThirstThreshold.Parched && _currentThirstThreshold != ThirstThreshold.Dead &&
                    Owner.TryGetComponent(out MovementSpeedModifierComponent movementSlowdownComponent))
                {
                    movementSlowdownComponent.RefreshMovementSpeedModifiers();
                }

                // Update UI
                Owner.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent);

                if (ThirstThresholdImages.TryGetValue(_currentThirstThreshold, out var statusTexture))
                {
                    statusEffectsComponent?.ChangeStatusEffectIcon(StatusEffect.Thirst, statusTexture);
                }
                else
                {
                    statusEffectsComponent?.RemoveStatusEffect(StatusEffect.Thirst);
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
            var calculatedThirstThreshold = GetThirstThreshold(_currentThirst);
            // _trySound(calculatedThreshold);
            if (calculatedThirstThreshold != _currentThirstThreshold)
            {
                _currentThirstThreshold = calculatedThirstThreshold;
                ThirstThresholdEffect();
                Dirty();
            }

            if (_currentThirstThreshold == ThirstThreshold.Dead)
            {
                if (Owner.TryGetComponent(out IDamageableComponent damageable))
                {
                    if (damageable.CurrentState != DamageState.Dead)
                    {
                        damageable.ChangeDamage(DamageType.Blunt, 2, true, null);
                    }
                }
            }
        }


        public void ResetThirst()
        {
            _currentThirstThreshold = ThirstThreshold.Okay;
            _currentThirst = ThirstThresholds[_currentThirstThreshold];
            ThirstThresholdEffect();
        }

        public override ComponentState GetComponentState()
        {
            return new ThirstComponentState(_currentThirstThreshold);
        }
    }

}
