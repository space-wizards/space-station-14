namespace Content.Shared.Defects.Components;

/// <summary>
/// Gives a gun a per-shot chance to backfire, triggering a small explosion at the
/// weapon's location. The blast affects the tile the shooter is standing on — it does
/// not directly apply damage, so anyone else on the same tile is also at risk.
/// Intensity is tuned per weapon via YAML so heavier guns backfire harder.
/// </summary>
[RegisterComponent]
public sealed partial class BackfireDefectComponent : DefectComponent
{
    public BackfireDefectComponent()
    {
        Prob = 0.20f;
        DefectLabel = "cracked chamber";
    }

    // Per-shot probability of a backfire occurring.
    [DataField]
    public float BackfireChance = 0.04f;

    // Explosion prototype to use (determines damage types).
    [DataField]
    public string ExplosionTypeId = "Default";

    // Total explosion intensity — scales with weapon damage tier.
    [DataField]
    public float TotalIntensity = 1.5f;

    // Maximum per-tile intensity — keep low to stay on one tile.
    [DataField]
    public float MaxIntensity = 1.0f;

    // Intensity slope — high value means very steep falloff (stays localised).
    [DataField]
    public float IntensitySlope = 8.0f;
}
