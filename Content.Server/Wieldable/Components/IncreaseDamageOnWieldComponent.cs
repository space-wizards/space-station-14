using Content.Shared.Damage;

namespace Content.Server.Wieldable.Components
{
    [RegisterComponent, Access(typeof(WieldableSystem))]
    public sealed class IncreaseDamageOnWieldComponent : Component
    {
        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;
    }
}
