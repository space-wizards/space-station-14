using Content.Shared.Alert;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHungerComponent))]
    public sealed class HungerComponent : SharedHungerComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        // Base stuff
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseDecayRate
        {
            get => _baseDecayRate;
            set => _baseDecayRate = value;
        }
        [DataField("baseDecayRate")]
        private float _baseDecayRate = 0.01666666666f;

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
        [DataField("startingHunger")]
        private float _currentHunger = -1f;

        [ViewVariables(VVAccess.ReadOnly)]
        public Dictionary<HungerThreshold, float> HungerThresholds => _hungerThresholds;

        [DataField("thresholds", customTypeSerializer: typeof(DictionarySerializer<HungerThreshold, float>))]
        private Dictionary<HungerThreshold, float> _hungerThresholds = new()
        {
            { HungerThreshold.Overfed, 200.0f },
            { HungerThreshold.Okay, 150.0f },
            { HungerThreshold.Peckish, 100.0f },
            { HungerThreshold.Starving, 50.0f },
            { HungerThreshold.Dead, 0.0f },
        };

        public static readonly Dictionary<HungerThreshold, AlertType> HungerThresholdAlertTypes = new()
        {
            { HungerThreshold.Peckish, AlertType.Peckish },
            { HungerThreshold.Starving, AlertType.Starving },
            { HungerThreshold.Dead, AlertType.Starving },
        };

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
                if (HungerThresholdAlertTypes.TryGetValue(_currentHungerThreshold, out var alertId))
                {
                    EntitySystem.Get<AlertsSystem>().ShowAlert(Owner, alertId);
                }
                else
                {
                    EntitySystem.Get<AlertsSystem>().ClearAlertCategory(Owner, AlertCategory.Hunger);
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
            // Do not change behavior unless starting hunger is explicitly defined
            if (_currentHunger < 0)
            {
                // Similar functionality to SS13. Should also stagger people going to the chef.
                _currentHunger = _random.Next(
                    (int) _hungerThresholds[HungerThreshold.Peckish] + 10,
                    (int) _hungerThresholds[HungerThreshold.Okay] - 1);
            }

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
            _currentHunger = Math.Clamp(_currentHunger + amount, HungerThresholds[HungerThreshold.Dead], HungerThresholds[HungerThreshold.Overfed]);
        }

        // TODO: If mob is moving increase rate of consumption?
        //  Should use a multiplier as something like a disease would overwrite decay rate.
        public void OnUpdate(float frametime)
        {
            UpdateFood(- frametime * ActualDecayRate);
            UpdateCurrentThreshold();
        }

        private void UpdateCurrentThreshold()
        {
            var calculatedHungerThreshold = GetHungerThreshold(_currentHunger);
            // _trySound(calculatedThreshold);
            if (calculatedHungerThreshold != _currentHungerThreshold)
            {
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
