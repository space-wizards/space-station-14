using System;
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

        //TODO: should be programmatic instead of how it currently is
        [ViewVariables] public float CurrentTemperature => _currentTemperature;
        [ViewVariables] public float FireDamageThreshold => _fireDamageThreshold;
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

        private float _fireDamageThreshold;
        private float _coldDamageThreshold;
        private float _tempDamageCoefficient;
        private float _currentTemperature;
        private float _specificHeat;

        private float _secondsSinceLastDamageUpdate = 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fireDamageThreshold, "fireDamageThreshold", 0);
            serializer.DataField(ref _coldDamageThreshold, "coldDamageThreshold", 0);
            serializer.DataField(ref _tempDamageCoefficient, "tempDamageCoefficient", 1);
            serializer.DataField(ref _currentTemperature, "currentTemperature", Atmospherics.T20C);
            serializer.DataField(ref _specificHeat, "specificHeat", Atmospherics.MinimumHeatCapacity);
        }

        public void Update(float frameTime)
        {
            var tempDamage = 0;
            DamageType? damageType = null;
            if (CurrentTemperature >= _fireDamageThreshold)
            {
                tempDamage = (int) Math.Floor((CurrentTemperature - _fireDamageThreshold) * _tempDamageCoefficient);
                damageType = DamageType.Heat;
            }
            else if (CurrentTemperature <= _coldDamageThreshold)
            {
                tempDamage = (int) Math.Floor((_coldDamageThreshold - CurrentTemperature) * _tempDamageCoefficient);
                damageType = DamageType.Cold;
            }

            if (damageType.HasValue)
            {
                _secondsSinceLastDamageUpdate += frameTime;
                HandleTemperatureDamage(damageType.Value, tempDamage);
                return;
            }
        }

        // Set new temperature
        public void SetTemperature(float newTemperature)
        {
            // creadth: if we won't have anything useful here
            // we might probably replace it with public setter
            // on the actual property instead of separate method
            _currentTemperature = newTemperature;
        }

        private void HandleTemperatureDamage(DamageType type, int amount)
        {
            if (!Owner.TryGetComponent(out IDamageableComponent component)) return;

            while (_secondsSinceLastDamageUpdate >= 1)
            {
                component?.ChangeDamage(type, amount, false);
                _secondsSinceLastDamageUpdate -= 1;
            }

        }
    }
}
