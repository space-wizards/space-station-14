using Content.Server.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Server.Damage.Components
{
    [Access(typeof(DamageOtherOnHitSystem))]
    [RegisterComponent]
    public sealed class DamageOtherOnHitComponent : Component
    {
        [DataField("resistancePenetration")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float? ResistancePenetration;
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

    }
}
