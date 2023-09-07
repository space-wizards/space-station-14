using Content.Shared.Damage;

namespace Content.Server.Armor
{
    [RegisterComponent]
    public sealed partial class ArmorComponent : Component
    {
        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;
    }
}
