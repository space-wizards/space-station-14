using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Temperature.Components
{
    /// <summary>
    /// Handles changing temperature,
    /// informing others of the current temperature,
    /// and taking fire damage from high temperature.
    /// </summary>
    [RegisterComponent]
    public sealed class TemperatureComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentTemperature { get; set; } = Atmospherics.T20C;

        [DataField("heatDamageThreshold")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float HeatDamageThreshold = 360f;

        [DataField("coldDamageThreshold")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ColdDamageThreshold = 260f;

        /// <summary>
        /// Overrides HeatDamageThreshold if the entity's within a parent with the TemperatureDamageThresholdsComponent component.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float? ParentHeatDamageThreshold;

        /// <summary>
        /// Overrides ColdDamageThreshold if the entity's within a parent with the TemperatureDamageThresholdsComponent component.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float? ParentColdDamageThreshold;

        [DataField("specificHeat")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpecificHeat = 50f;

        /// <summary>
        ///     How well does the air surrounding you merge into your body temperature?
        /// </summary>
        [DataField("atmosTemperatureTransferEfficiency")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float AtmosTemperatureTransferEfficiency = 0.1f;

        [ViewVariables] public float HeatCapacity
        {
            get
            {
                if (IoCManager.Resolve<IEntityManager>().TryGetComponent<PhysicsComponent>(Owner, out var physics) && physics.FixturesMass != 0)
                {
                    return SpecificHeat * physics.FixturesMass;
                }

                return Atmospherics.MinimumHeatCapacity;
            }
        }

        [DataField("coldDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier ColdDamage = new();

        [DataField("heatDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier HeatDamage = new();

        /// <summary>
        ///     Temperature won't do more than this amount of damage per second.
        ///
        ///     Okay it genuinely reaches this basically immediately for a plasma fire.
        /// </summary>
        [DataField("damageCap")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 DamageCap = FixedPoint2.New(8);

        /// <summary>
        ///     Used to keep track of when damage starts/stops. Useful for logs.
        /// </summary>
        public bool TakingDamage = false;
    }
}
