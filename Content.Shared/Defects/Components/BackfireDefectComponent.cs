namespace Content.Shared.Defects.Components;

/// <summary>
/// Gives a gun a per-shot chance to backfire, triggering a small explosion at the
/// weapon's location. The blast affects the tile the shooter is standing on.
/// It does not directly apply damage, so anyone else on the same tile is also at risk.
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

    // Explosion prototype to use (determines damage types) - just in case we want to change it, almost always will remain at "Default"
    [DataField]
    public string ExplosionTypeId = "Default";

    // Total explosion intensity, scaled relative to weapon damage.
    [DataField]
    public float TotalIntensity = 1.5f;

    // Max per-tile intensity, kept low to contain the blast to roughly one tile.
    [DataField]
    public float MaxIntensity = 1.0f;

    // Intensity slope, high value = steep falloff.
    [DataField]
    public float IntensitySlope = 8.0f;
}
