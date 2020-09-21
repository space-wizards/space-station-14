using System;
using System.Diagnostics;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
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

            if (Owner.TryGetComponent(out ServerStatusEffectsComponent status))
            {
                switch(CurrentTemperature)
                {
                    // Cold strong.
                    case var t when t <= 260:
                        status.ChangeStatusEffect(StatusEffect.Temperature, "/Textures/Interface/StatusEffects/Temperature/cold3.png", null);
                        break;

                    // Cold mild.
                    case var t when t <= 280 && t > 260:
                        status.ChangeStatusEffect(StatusEffect.Temperature, "/Textures/Interface/StatusEffects/Temperature/cold2.png", null);
                        break;

                    // Cold weak.
                    case var t when t <= 292 && t > 280:
                        status.ChangeStatusEffect(StatusEffect.Temperature, "/Textures/Interface/StatusEffects/Temperature/cold1.png", null);
                        break;

                    // Safe.
                    case var t when t <= 327 && t > 292:
                        status.RemoveStatusEffect(StatusEffect.Temperature);
                        break;

                    // Heat weak.
                    case var t when t <= 335 && t > 327:
                        status.ChangeStatusEffect(StatusEffect.Temperature, "/Textures/Interface/StatusEffects/Temperature/hot1.png", null);
                        break;

                    // Heat mild.
                    case var t when t <= 345 && t > 335:
                        status.ChangeStatusEffect(StatusEffect.Temperature, "/Textures/Interface/StatusEffects/Temperature/hot2.png", null);
                        break;

                    // Heat strong.
                    case var t when t > 345:
                        status.ChangeStatusEffect(StatusEffect.Temperature, "/Textures/Interface/StatusEffects/Temperature/hot3.png", null);
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
