using Content.Shared.Damage;

namespace Content.Shared.Defects.Components;

/// <summary>
/// Applies damage to the wielder on every melee swing, regardless of whether the
/// attack connects. Represents a weapon with a missing or damaged handle.
/// Damage type and amount are configured via YAML.
/// </summary>
[RegisterComponent]
public sealed partial class DamageUserDefectComponent : DefectComponent
{
    public DamageUserDefectComponent()
    {
        Prob = 0.40f;
        DefectLabel = "missing handle";
    }

    // Damage applied to the wielder on each swing.
    [DataField]
    public DamageSpecifier? Damage;
}
