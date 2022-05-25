using Content.Shared.Damage;
using Content.Shared.Sound;

namespace Content.Server.Mousetrap;

[RegisterComponent]
public sealed class MousetrapComponent : Component
{
    [ViewVariables]
    public bool IsActive;

    // Entity prototypes that have other types of
    // damage applied when interacting with this.
    // It would be more fun to just do this
    // based on scale/height of the entity,
    // but this might result in false positives.
    [DataField("specialDamage")]
    public DamageSpecifier SpecialDamage = new();

    [DataField("ignoreDamageIfInventorySlotsFilled")]
    public List<string> IgnoreDamageIfSlotFilled = new();
}
