using Content.Shared.Damage;

namespace Content.Shared.Defects.Components;

/// <summary>
/// Burns the wielder for a small amount of heat damage on every melee swing,
/// regardless of whether the attack connects. Represents a weapon with a
/// missing or damaged handle exposing the user to the power emitter.
/// Configure the damage amount per item in YAML via the Damage field.
/// </summary>
[RegisterComponent]
public sealed partial class DamageUserDefectComponent : DefectComponent
{
    public DamageUserDefectComponent()
    {
        Prob = 0.40f;
        DefectLabel = "missing handle";
    }

    // Damage applied to the wielder on each swing. Set in YAML per item.
    // If null, no damage is applied (effectively a no-op).
    [DataField]
    public DamageSpecifier? Damage;
}
