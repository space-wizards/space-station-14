using Content.Shared.Atmos;
using Content.Shared.Damage;
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

        [DataField("heatDamageThreshold")]
        private float _heatDamageThreshold = default;
        [DataField("coldDamageThreshold")]
        private float _coldDamageThreshold = default;
        [DataField("tempDamageCoefficient")]
        private float _tempDamageCoefficient = 1;
        [DataField("currentTemperature")]
        public float CurrentTemperature { get; set; } = Atmospherics.T20C;
        [DataField("specificHeat")]
        private float _specificHeat = Atmospherics.MinimumHeatCapacity;

        [ViewVariables] public float HeatDamageThreshold => _heatDamageThreshold;
        [ViewVariables] public float ColdDamageThreshold => _coldDamageThreshold;
        [ViewVariables] public float TempDamageCoefficient => _tempDamageCoefficient;
        [ViewVariables] public float SpecificHeat => _specificHeat;
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

        [DataField("coldDamage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier ColdDamage = default!;

        [DataField("heatDamage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier HeatDamage = default!;
    }
}
