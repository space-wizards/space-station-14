namespace Content.Shared.Defects.Components;

/// <summary>
/// Reduces melee weapon damage at spawn by multiplying all damage values on
/// MeleeWeaponComponent by DamageMultiplier. Represents a degraded or
/// partially-drained power cell that can't sustain full output.
/// </summary>
[RegisterComponent]
public sealed partial class ReducedDamageDefectComponent : DefectComponent
{
    public ReducedDamageDefectComponent()
    {
        Prob = 0.55f;
        DefectLabel = "weak power cell";
    }

    // All MeleeWeaponComponent damage values are multiplied by this at MapInit.
    // 0.75 = 25% damage reduction.
    [DataField]
    public float DamageMultiplier = 0.75f;
}
