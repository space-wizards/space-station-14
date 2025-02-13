using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Server._Impstation.Nutrition.Components
{
    [RegisterComponent, Access(typeof(SmokingSystem))]
    public sealed partial class VapeCartComponent : Component
    {
        /// <summary>
        /// Solution volume will be divided by this number and converted to the gas
        /// </summary>
        [DataField("gasReductionFactor")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float GasReductionFactor { get; set; } = 1.8f;

        /// <summary>
        /// How much solution volume added to the bloodstream will be multiplied by
        /// </summary>
        [DataField("flavorMultiplicationFactor")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float FlavorMultiplicationFactor { get; set; } = 2.0f;

        /// <summary>
        /// Ignores vape pen component damage when true
        /// </summary>
        [DataField("ignoreDamage")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IgnoreDamage { get; set; } = false;
    }
}
