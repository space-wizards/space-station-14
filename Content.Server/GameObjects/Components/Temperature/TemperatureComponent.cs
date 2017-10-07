using Content.Server.Interfaces.GameObjects;
using Content.Shared.Maths;
using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Handles changing temperature,
    /// informing others of the current temperature,
    /// and taking fire damage from high temperature.
    /// </summary>
    public class TemperatureComponent : Component, ITemperatureComponent
    {
        /// <inheritdoc />
        public override string Name => "Temperature";

        /// <inheritdoc />
        public override uint? NetID => ContentNetIDs.TEMPERATURE;

        //TODO: should be programmatic instead of how it currently is
        public float CurrentTemperature { get; private set; } = PhysicalConstants.ZERO_CELCIUS;

        float _fireDamageThreshold = 0;
        float _fireDamageCoefficient = 1;

        float _secondsSinceLastDamageUpdate = 0;

        /// <inheritdoc />
        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;

            if (mapping.TryGetNode("firedamagethreshold", out node))
            {
                _fireDamageThreshold = node.AsFloat();
            }
            if (mapping.TryGetNode("firedamagecoefficient", out node))
            {
                _fireDamageCoefficient = node.AsFloat();
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            int fireDamage = (int)Math.Floor(Math.Max(0, CurrentTemperature - _fireDamageThreshold) / _fireDamageCoefficient);

            _secondsSinceLastDamageUpdate += frameTime;

            Owner.TryGetComponent<DamageableComponent>(out DamageableComponent component);

            while (_secondsSinceLastDamageUpdate >= 1)
            {
                component?.TakeDamage(DamageType.Heat, fireDamage);
                _secondsSinceLastDamageUpdate -= 1;
            }
        }
    }
}
