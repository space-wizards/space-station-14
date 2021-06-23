using System;
using Content.Server.Alert;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Temperature.Components
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
                if (Owner.TryGetComponent<IPhysBody>(out var physics))
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

            if (Owner.TryGetComponent(out ServerAlertsComponent? status))
            {
                switch (CurrentTemperature)
                {
                    // Cold strong.
                    case <= 260:
                        status.ShowAlert(AlertType.Cold, 3);
                        break;

                    // Cold mild.
                    case <= 280 and > 260:
                        status.ShowAlert(AlertType.Cold, 2);
                        break;

                    // Cold weak.
                    case <= 292 and > 280:
                        status.ShowAlert(AlertType.Cold, 1);
                        break;

                    // Safe.
                    case <= 327 and > 292:
                        status.ClearAlertCategory(AlertCategory.Temperature);
                        break;

                    // Heat weak.
                    case <= 335 and > 327:
                        status.ShowAlert(AlertType.Hot, 1);
                        break;

                    // Heat mild.
                    case <= 345 and > 335:
                        status.ShowAlert(AlertType.Hot, 2);
                        break;

                    // Heat strong.
                    case > 345:
                        status.ShowAlert(AlertType.Hot, 3);
                        break;
                }
            }

            if (!damageType.HasValue) return;

            if (!Owner.TryGetComponent(out IDamageableComponent? component)) return;
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
