using Content.Shared.Damage;

namespace Content.Server.Damage.Components
{
    [RegisterComponent]
    public sealed class DamageOnLandComponent : Component
    {
        [DataField("ignoreResistances")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IgnoreResistances = false;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
    }
}
