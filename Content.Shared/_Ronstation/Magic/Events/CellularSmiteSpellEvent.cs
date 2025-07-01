using Content.Shared.Actions;
using Content.Shared.Damage;

namespace Content.Shared.Magic.Events;

public sealed partial class CellularSmiteSpellEvent : EntityTargetActionEvent
{
    //<summary>
    // Damage that the smite spell will do.
    //</summary>
    [DataField]
    public DamageSpecifier smiteDamage = new();
}
