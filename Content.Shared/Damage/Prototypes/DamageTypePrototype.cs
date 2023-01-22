using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     A single damage type. These types are grouped together in <see cref="DamageGroupPrototype"/>s.
    /// </summary>
    [Prototype("damageType")]
    public sealed class DamageTypePrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        /// The price for each 1% damage reduction in armors
        /// </summary>
        [DataField("armorCoefficientPrice")]
        public double ArmorPriceCoefficient { get; set; }

        /// <summary>
        /// The price for each flat damage reduction in armors
        /// </summary>
        [DataField("armorFlatPrice")]
        public double ArmorPriceFlat { get; set; }

        /// <summary>
        /// The trauma to apply for wounds when this damage is applied
        /// </summary>
        /// <returns></returns>
        [DataField("trauma", customTypeSerializer: typeof(PrototypeIdSerializer<TraumaPrototype>))]
        public string? Trauma;
    }
}
