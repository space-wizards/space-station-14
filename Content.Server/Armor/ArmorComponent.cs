using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Armor
{
    [RegisterComponent]
    public class ArmorComponent : Component
    {
        public override string Name => "Armor";

        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;
    }
}
