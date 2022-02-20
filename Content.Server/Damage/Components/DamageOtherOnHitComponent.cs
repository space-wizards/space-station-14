using Content.Server.Damage.Systems;
using Content.Shared.Damage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Damage.Components
{
    [Friend(typeof(DamageOtherOnHitSystem))]
    [RegisterComponent]
    public sealed class DamageOtherOnHitComponent : Component
    {
        [DataField("ignoreResistances")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IgnoreResistances = false;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

    }
}
