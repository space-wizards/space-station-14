using Content.Shared.Damage;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public sealed class DamageOnLandComponent : Component
    {
        [DataField("resistancePenetration")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float? ResistancePenetration;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
    }
}
