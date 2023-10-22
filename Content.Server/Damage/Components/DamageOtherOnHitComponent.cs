using Content.Server.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Server.Damage.Components
{
    [Access(typeof(DamageOtherOnHitSystem))]
    [RegisterComponent]
    public sealed partial class DamageOtherOnHitComponent : Component
    {
        [DataField("ignoreResistances")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IgnoreResistances = false;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

    }
}
