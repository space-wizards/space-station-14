using System;
using System.Diagnostics;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Maths;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
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
                if (Owner.TryGetComponent<ICollidableComponent>(out var physics))
                {
                    return SpecificHeat * physics.Mass;
                }

                return Atmospherics.MinimumHeatCapacity;
            }
        }

        [ViewVariables] public float SpecificHeat => _specificHeat;

        private float _heatDamageThreshold;
        private float _coldDamageThreshold;
        private float _tempDamageCoefficient;
        private float _currentTemperature;
        private float _specificHeat;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _heatDamageThreshold, "heatDamageThreshold", 0);
            serializer.DataField(ref _coldDamageThreshold, "coldDamageThreshold", 0);
            serializer.DataField(ref _tempDamageCoefficient, "tempDamageCoefficient", 1);
            serializer.DataField(ref _currentTemperature, "currentTemperature", Atmospherics.T20C);
            serializer.DataField(ref _specificHeat, "specificHeat", Atmospherics.MinimumHeatCapacity);
        }

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

            if (!damageType.HasValue) return;

            if (!Owner.TryGetComponent(out IDamageableComponent component)) return;
            component.ChangeDamage(damageType.Value, tempDamage, false);
            Debug.Write($"Temp is: {CurrentTemperature}");
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
