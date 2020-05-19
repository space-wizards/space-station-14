using System;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using Content.Shared.Maths;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Handles changing temperature,
    /// informing others of the current temperature,
    /// and taking fire damage from high temperature.
    /// </summary>
    [RegisterComponent]
    public class TemperatureComponent : Component, ITemperatureComponent
    {
        /// <inheritdoc />
        public override string Name => "Temperature";

        //TODO: should be programmatic instead of how it currently is
        [ViewVariables]
        public float CurrentTemperature { get; private set; } = PhysicalConstants.ZERO_CELCIUS;

        float _fireDamageThreshold = 0;
        float _fireDamageCoefficient = 1;

        float _secondsSinceLastDamageUpdate = 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fireDamageThreshold, "firedamagethreshold", 0);
            serializer.DataField(ref _fireDamageCoefficient, "firedamagecoefficient", 1);
        }

        /// <inheritdoc />
        public void OnUpdate(float frameTime)
        {
            int fireDamage = (int)Math.Floor(Math.Max(0, CurrentTemperature - _fireDamageThreshold) / _fireDamageCoefficient);

            _secondsSinceLastDamageUpdate += frameTime;

            Owner.TryGetComponent(out DamageableComponent component);

            while (_secondsSinceLastDamageUpdate >= 1)
            {
                component?.TakeDamage(DamageType.Heat, fireDamage);
                _secondsSinceLastDamageUpdate -= 1;
            }
        }
    }
}
