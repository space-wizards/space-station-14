using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
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
    public sealed class HungerComponent : Component
    {
        #pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
        #pragma warning restore 649

        public override string Name => "Hunger";

        // Base stuff
        public float BaseDecayRate => _baseDecayRate;
        [ViewVariables] private float _baseDecayRate;
        public float ActualDecayRate => _actualDecayRate;
        [ViewVariables] private float _actualDecayRate;

        // Hunger
        public HungerThreshold CurrentHungerThreshold => _currentHungerThreshold;
        private HungerThreshold _currentHungerThreshold;
        private HungerThreshold _lastHungerThreshold;
        public float CurrentHunger => _currentHunger;
        [ViewVariables] private float _currentHunger;

        public Dictionary<HungerThreshold, float> HungerThresholds => _hungerThresholds;
        private Dictionary<HungerThreshold, float> _hungerThresholds = new Dictionary<HungerThreshold, float>
        {
            {HungerThreshold.Overfed, 600.0f},
            {HungerThreshold.Okay, 450.0f},
            {HungerThreshold.Peckish, 300.0f},
            {HungerThreshold.Starving, 150.0f},
            {HungerThreshold.Dead, 0.0f},
        };

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _baseDecayRate, "base_decay_rate", 0.5f);
        }

        public void HungerThresholdEffect(bool force = false)
        {
            if (_currentHungerThreshold != _lastHungerThreshold || force) {
                Logger.InfoS("hunger", $"Updating hunger state for {Owner.Name}");

                // Revert slow speed if required
                if (_lastHungerThreshold == HungerThreshold.Starving && _currentHungerThreshold != HungerThreshold.Dead &&
                    Owner.TryGetComponent(out PlayerInputMoverComponent playerSpeedupComponent))
                {
                    // TODO shitcode: Come up something better
                    playerSpeedupComponent.WalkMoveSpeed = playerSpeedupComponent.WalkMoveSpeed * 2;
                    playerSpeedupComponent.SprintMoveSpeed = playerSpeedupComponent.SprintMoveSpeed * 4;
                }

                // Update UI
                Owner.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent);
                statusEffectsComponent?.ChangeStatus(StatusEffect.Hunger, "/Textures/Mob/UI/Hunger/" +
                                                                          _currentHungerThreshold + ".png");

                switch (_currentHungerThreshold)
                {
                    case HungerThreshold.Overfed:
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate * 1.2f;
                        return;

                    case HungerThreshold.Okay:
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate;
                        return;

                    case HungerThreshold.Peckish:
                        // Same as okay except with UI icon saying eat soon.
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate * 0.8f;
                        return;

                    case HungerThreshold.Starving:
                        // TODO: If something else bumps this could cause mega-speed.
                        // If some form of speed update system if multiple things are touching it use that.
                        if (Owner.TryGetComponent(out PlayerInputMoverComponent playerInputMoverComponent)) {
                            playerInputMoverComponent.WalkMoveSpeed = playerInputMoverComponent.WalkMoveSpeed / 2;
                            playerInputMoverComponent.SprintMoveSpeed = playerInputMoverComponent.SprintMoveSpeed / 4;
                        }
                        _lastHungerThreshold = _currentHungerThreshold;
                        _actualDecayRate = _baseDecayRate * 0.6f;
                        return;

                    case HungerThreshold.Dead:
                        return;
                    default:
                        Logger.ErrorS("hunger", $"No hunger threshold found for {_currentHungerThreshold}");
                        throw new ArgumentOutOfRangeException($"No hunger threshold found for {_currentHungerThreshold}");
                }
            }
        }

        protected override void Startup()
        {
            base.Startup();
            // Similar functionality to SS13. Should also stagger people going to the chef.
            _currentHunger = _random.Next(
                (int)_hungerThresholds[HungerThreshold.Peckish] + 10,
                (int)_hungerThresholds[HungerThreshold.Okay] - 1);
            _currentHungerThreshold = GetHungerThreshold(_currentHunger);
            _lastHungerThreshold = HungerThreshold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
            HungerThresholdEffect(true);
        }

        public HungerThreshold GetHungerThreshold(float food)
        {
            HungerThreshold result = HungerThreshold.Dead;
            var value = HungerThresholds[HungerThreshold.Overfed];
            foreach (var threshold in _hungerThresholds)
            {
                if (threshold.Value <= value && threshold.Value >= food)
                {
                    result = threshold.Key;
                    value = threshold.Value;
                }
            }

            return result;
        }

        public void UpdateFood(float amount)
        {
            _currentHunger = Math.Min(_currentHunger + amount, HungerThresholds[HungerThreshold.Overfed]);
        }

        // TODO: If mob is moving increase rate of consumption?
        //  Should use a multiplier as something like a disease would overwrite decay rate.
        public void OnUpdate(float frametime)
        {
            _currentHunger -= frametime * ActualDecayRate;
            var calculatedHungerThreshold = GetHungerThreshold(_currentHunger);
            // _trySound(calculatedThreshold);
            if (calculatedHungerThreshold != _currentHungerThreshold)
            {
                _currentHungerThreshold = calculatedHungerThreshold;
                HungerThresholdEffect();
            }
            if (_currentHungerThreshold == HungerThreshold.Dead)
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
    }

    public enum HungerThreshold
    {
        Overfed,
        Okay,
        Peckish,
        Starving,
        Dead,
    }
}
