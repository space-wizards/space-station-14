using System;
using System.Collections.Generic;
using Content.Server.Administration.Logs;
using Content.Server.Alert;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Database;
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
    public sealed class HungerComponent : SharedHungerComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
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

        // Hunger
        [ViewVariables(VVAccess.ReadOnly)]
        public override HungerThreshold CurrentHungerThreshold => _currentHungerThreshold;
        private HungerThreshold _currentHungerThreshold;

        private HungerThreshold _lastHungerThreshold;

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentHunger
        {
            get => _currentHunger;
            set => _currentHunger = value;
        }
        private float _currentHunger;

        [ViewVariables(VVAccess.ReadOnly)]
        public Dictionary<HungerThreshold, float> HungerThresholds => _hungerThresholds;
        private readonly Dictionary<HungerThreshold, float> _hungerThresholds = new()
        {
            { HungerThreshold.Overfed, 600.0f },
            { HungerThreshold.Okay, 450.0f },
            { HungerThreshold.Peckish, 300.0f },
            { HungerThreshold.Starving, 150.0f },
            { HungerThreshold.Dead, 0.0f },
        };

        public static readonly Dictionary<HungerThreshold, AlertType> HungerThresholdAlertTypes = new()
        {
            { HungerThreshold.Overfed, AlertType.Overfed },
            { HungerThreshold.Peckish, AlertType.Peckish },
            { HungerThreshold.Starving, AlertType.Starving },
        };

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        public void HungerThresholdEffect(bool force = false)
        {
            if (_currentHungerThreshold != _lastHungerThreshold || force)
            {
                // Revert slow speed if required
                if (_lastHungerThreshold == HungerThreshold.Starving && _currentHungerThreshold != HungerThreshold.Dead &&
                    _entMan.TryGetComponent(Owner, out MovementSpeedModifierComponent? movementSlowdownComponent))
                {
                    EntitySystem.Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(Owner);
                }

                // Update UI
                _entMan.TryGetComponent(Owner, out ServerAlertsComponent? alertsComponent);

                if (HungerThresholdAlertTypes.TryGetValue(_currentHungerThreshold, out var alertId))
                {
                    alertsComponent?.ShowAlert(alertId);
                }
                else
                {
                    alertsComponent?.ClearAlertCategory(AlertCategory.Hunger);
                }

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
                        EntitySystem.Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(Owner);
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
            Dirty();
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
            UpdateCurrentThreshold();

            if (_currentHungerThreshold != HungerThreshold.Dead)
                return;
            // --> Current Hunger is below dead threshold

            if (!_entMan.TryGetComponent(Owner, out MobStateComponent? mobState))
                return;

            if (!mobState.IsDead())
            {
                // --> But they are not dead yet.
                _accumulatedFrameTime += frametime;
                if (_accumulatedFrameTime >= 1)
                {
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner, Damage * (int) _accumulatedFrameTime, true);
                    _accumulatedFrameTime -= (int) _accumulatedFrameTime;
                }
            }
        }

        private void UpdateCurrentThreshold()
        {
            var calculatedHungerThreshold = GetHungerThreshold(_currentHunger);
            // _trySound(calculatedThreshold);
            if (calculatedHungerThreshold != _currentHungerThreshold)
            {
                if (_currentHungerThreshold == HungerThreshold.Dead)
                    EntitySystem.Get<AdminLogSystem>().Add(LogType.Hunger, $"{_entMan.ToPrettyString(Owner):entity} has stopped starving");
                else if (calculatedHungerThreshold == HungerThreshold.Dead)
                    EntitySystem.Get<AdminLogSystem>().Add(LogType.Hunger, $"{_entMan.ToPrettyString(Owner):entity} has started starving");

                _currentHungerThreshold = calculatedHungerThreshold;
                HungerThresholdEffect();
                Dirty();
            }
        }

        public void ResetFood()
        {
            _currentHunger = HungerThresholds[HungerThreshold.Okay];
            UpdateCurrentThreshold();
        }

        public override ComponentState GetComponentState()
        {
            return new HungerComponentState(_currentHungerThreshold);
        }
    }
}
