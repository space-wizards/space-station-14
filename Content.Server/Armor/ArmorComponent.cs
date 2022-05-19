using Content.Shared.Damage;

namespace Content.Server.Armor
{
    [RegisterComponent]
    public sealed class ArmorComponent : Component
    {
        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;
    }
}
