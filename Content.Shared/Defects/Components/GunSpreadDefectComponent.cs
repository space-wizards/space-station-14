using Robust.Shared.Maths;

namespace Content.Shared.Defects.Components;

/// <summary>
/// Randomizes a gun's spread at MapInit via GunRefreshModifiersEvent.
/// Sampled angle deltas are added on top of the gun's base angles so they
/// compose correctly with other modifiers (e.g. GunWieldBonus).
/// </summary>
[RegisterComponent]
public sealed partial class GunSpreadDefectComponent : DefectComponent
{
    public GunSpreadDefectComponent()
    {
        Prob = 0.7f;
        DefectLabel = "warped barrel";
    }

    // Target angle ranges (absolute degrees)
    [DataField] public Angle? MinAngleMin;
    [DataField] public Angle? MinAngleMax;
    [DataField] public Angle? MaxAngleMin;
    [DataField] public Angle? MaxAngleMax;

    // Deltas computed at MapInit (sampled target - base angle) 
    // Added to args.MinAngle/MaxAngle in GunRefreshModifiersEvent.
    [DataField] public Angle MinAngleDelta;
    [DataField] public Angle MaxAngleDelta;

    // Multiplier-based spread (for ammo-spread weapons like the Hushpup)
    [DataField] public float? SpreadMultiplierMin;
    [DataField] public float? SpreadMultiplierMax;
}
