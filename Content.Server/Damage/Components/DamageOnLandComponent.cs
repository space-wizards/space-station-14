using Content.Shared.Damage;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public sealed partial class DamageOnLandComponent : Component
    {
        /// <summary>
        /// Should this entity be damaged when it lands regardless of its resistances?
        /// </summary>
        [DataField("ignoreResistances")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IgnoreResistances = false;

        /// <summary>
        /// How much damage.
        /// </summary>
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
    }
}
