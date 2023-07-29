using Content.Shared.Damage;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public sealed class DamageOnLandComponent : Component
    {
        [DataField("resistanceReductionValue")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float? ResistanceReductionValue;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
    }
}
