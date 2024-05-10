using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     A single damage type. These types are grouped together in <see cref="DamageGroupPrototype"/>s.
    /// </summary>
    [Prototype("damageType")]
    public sealed partial class DamageTypePrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

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
    }
}
