using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Temperature
{
    /// <summary>
    /// Handles changing temperature,
    /// informing others of the current temperature,
    /// and taking fire damage from high temperature.
    /// </summary>
    [RegisterComponent]
    public class TemperatureComponent : Component
    {
        /// <inheritdoc />
        public override string Name => "Temperature";

        [ViewVariables] public float CurrentTemperature { get => _currentTemperature; set => _currentTemperature = value; }

        [ViewVariables] public float HeatDamageThreshold => _heatDamageThreshold;
        [ViewVariables] public float ColdDamageThreshold => _coldDamageThreshold;
        [ViewVariables] public float TempDamageCoefficient => _tempDamageCoefficient;
        [ViewVariables] public float HeatCapacity {
            get
            {
                if (Owner.TryGetComponent<IPhysicsComponent>(out var physics))
                {
                    return SpecificHeat * physics.Mass;
                }

                return Atmospherics.MinimumHeatCapacity;
            }
        }

        [ViewVariables] public float SpecificHeat => _specificHeat;

        [DataField("heatDamageThreshold")]
        private float _heatDamageThreshold = default;
        [DataField("coldDamageThreshold")]
        private float _coldDamageThreshold = default;
        [DataField("tempDamageCoefficient")]
        private float _tempDamageCoefficient = 1;
        [DataField("currentTemperature")]
        private float _currentTemperature = Atmospherics.T20C;
        [DataField("specificHeat")]
        private float _specificHeat = Atmospherics.MinimumHeatCapacity;

        public void Update()
        {
            var tempDamage = 0;
            DamageType? damageType = null;
            if (CurrentTemperature >= _heatDamageThreshold)
            {
                tempDamage = (int) Math.Floor((CurrentTemperature - _heatDamageThreshold) * _tempDamageCoefficient);
                damageType = DamageType.Heat;
            }
            else if (CurrentTemperature <= _coldDamageThreshold)
            {
                tempDamage = (int) Math.Floor((_coldDamageThreshold - CurrentTemperature) * _tempDamageCoefficient);
                damageType = DamageType.Cold;
            }

            if (Owner.TryGetComponent(out ServerAlertsComponent status))
            {
                switch(CurrentTemperature)
                {
                    // Cold strong.
                    case var t when t <= 260:
                        status.ShowAlert(AlertType.Cold, 3);
                        break;

                    // Cold mild.
                    case var t when t <= 280 && t > 260:
                        status.ShowAlert(AlertType.Cold, 2);
                        break;

                    // Cold weak.
                    case var t when t <= 292 && t > 280:
                        status.ShowAlert(AlertType.Cold, 1);
                        break;

                    // Safe.
                    case var t when t <= 327 && t > 292:
                        status.ClearAlertCategory(AlertCategory.Temperature);
                        break;

                    // Heat weak.
                    case var t when t <= 335 && t > 327:
                        status.ShowAlert(AlertType.Hot, 1);
                        break;

                    // Heat mild.
                    case var t when t <= 345 && t > 335:
                        status.ShowAlert(AlertType.Hot, 2);
                        break;

                    // Heat strong.
                    case var t when t > 345:
                        status.ShowAlert(AlertType.Hot, 3);
                        break;
                }
            }

            if (!damageType.HasValue) return;

            if (!Owner.TryGetComponent(out IDamageableComponent component)) return;
            component.ChangeDamage(damageType.Value, tempDamage, false);
        }

        /// <summary>
        /// Forcefully give heat to this component
        /// </summary>
        /// <param name="heatAmount"></param>
        public void ReceiveHeat(float heatAmount)
        {
            CurrentTemperature += heatAmount / HeatCapacity;
        }

        /// <summary>
        /// Forcefully remove heat from this component
        /// </summary>
        /// <param name="heatAmount"></param>
        public void RemoveHeat(float heatAmount)
        {
            CurrentTemperature -= heatAmount / HeatCapacity;
        }

    }
}
