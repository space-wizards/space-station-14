using Content.Shared.Damage;

namespace Content.Shared.Wieldable.Components;

[RegisterComponent, Access(typeof(WieldableSystem))]
public sealed partial class IncreaseDamageOnWieldComponent : Component
{
    [DataField("damage", required: true)]
    public DamageSpecifier BonusDamage = default!;
}
