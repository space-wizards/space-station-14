namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent]
public sealed class GravityAnomalyComponent : Component
{
    /// <summary>
    /// The maximumum size the GravityWellComponent MaxRange can be.
    /// Is scaled linearly with stability.
    /// </summary>
    [DataField("maxGravityWellRange")]
    public float MaxGravityWellRange = 8f;

    /// <summary>
    /// The maximum distance from which the anomaly
    /// can throw you via a pulse.
    /// </summary>
    [DataField("maxThrowRange")]
    public float MaxThrowRange = 5f;

    [DataField("maxThrowStrength")]
    public float MaxThrowStrength = 5f;

    /// <summary>
    /// The minimum acceleration value for GravityWellComponent
    /// </summary>
    [DataField("minAccel")]
    public float MinAccel = 1f;

    /// <summary>
    /// The maximum acceleration value for GravityWellComponent
    /// </summary>
    [DataField("maxAccel")]
    public float MaxAccel = 5f;
}
