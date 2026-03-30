namespace Content.Shared.Defects.Components;

/// <summary>
/// Randomizes explosive yield and/or projectile count at MapInit.
/// When present (default 70% chance), the system sets absolute values
/// within the configured ranges on ExplosiveComponent and/or ProjectileGrenadeComponent.
/// When absent (rolled out), the entity's inherited prototype values apply -- typically full yield.
/// </summary>
[RegisterComponent]
public sealed partial class RandomExplosiveYieldDefectComponent : DefectComponent
{
    public RandomExplosiveYieldDefectComponent()
    {
        Prob = 0.7f;
        DefectLabel = "degraded filler";
    }

    // --- ExplosiveComponent fields (null = don't override) ---
    [DataField]
    public float? MaxIntensityMin;
    [DataField]
    public float? MaxIntensityMax;

    [DataField]
    public float? TotalIntensityMin;
    [DataField]
    public float? TotalIntensityMax;

    // --- ProjectileGrenadeComponent fields (null = don't override) ---
    [DataField]
    public int? CapacityMin;
    [DataField]
    public int? CapacityMax;
}
