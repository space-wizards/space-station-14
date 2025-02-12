using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Atmos;

namespace Content.Server.Nutrition.Components // Vapes are very nutritious.
{
    [RegisterComponent, Access(typeof(SmokingSystem))]
    public sealed partial class VapeComponent : Component
    {
        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Delay { get; set; } = 5;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float UserDelay { get; set; } = 2;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ExplosionIntensity { get; set; } = 2.5f;

        // TODO use RiggableComponent.
        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ExplodeOnUse { get; set; } = false;

        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public Gas GasType { get; set; } = Gas.WaterVapor;

        /// <summary>
        /// Solution volume will be divided by this number and converted to the gas
        /// </summary>
        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ReductionFactor { get; set; } = 300f;

        // TODO when this gets fixed, use prototype serializers
        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public string SolutionNeeded = "Water";
    }
}
