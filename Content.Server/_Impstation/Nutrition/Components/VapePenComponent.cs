using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Server._Impstation.Nutrition.Components
{
    [RegisterComponent, Access(typeof(SmokingSystem))]
    public sealed partial class VapePenComponent : Component
    {
        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Delay { get; set; } = 3;

        [DataField("userDelay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float UserDelay { get; set; } = 1;

        [DataField("explosionIntensity")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ExplosionIntensity { get; set; } = 2.5f;

        [DataField("explodeOnUse")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ExplodeOnUse { get; set; } = false;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("cartSlot")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string CartSlotId = "cart_slot";

        [DataField("fillLevel")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float FillLevel = 0.0f;

        /// <summary>
        /// Amounts of reagents that cause an explosion
        /// </summary>
        [DataField("unstableReagent")]
        [ViewVariables(VVAccess.ReadWrite)]
        public List<ReagentQuantity> UnstableReagent = [
            new("Plasma", FixedPoint2.New(5), null),
            new("Tritium", FixedPoint2.New(1), null)
        ];

        /// <summary>
        /// Reagent composition of the total solution that's needed draw
        /// </summary>
        [DataField("acceptableSolvents")]
        [ViewVariables(VVAccess.ReadWrite)]
        public List<ReagentQuantity> AcceptableSolvents = [
            new("Water", FixedPoint2.New(100.0f), null),
            new("Sugar", FixedPoint2.New(50.0f), null)
        ];

        /// <summary>
        /// How much charge a single use expends.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("chargeUse")]
        public float ChargeUse = 36f;
    }
}
