using Content.Shared.Damage;
using Content.Shared.Sound;

namespace Content.Server.Mousetrap;

[RegisterComponent]
public sealed class MousetrapComponent : Component
{
    [ViewVariables]
    public bool IsActive;

    [DataField("ignoreDamageIfInventorySlotsFilled")]
    public List<string> IgnoreDamageIfSlotFilled = new();
}
