namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

[RegisterComponent]
public sealed class GravityWellArtifactComponent : Component
{
    /// <summary>
    /// The bounds on the outer range of the produced gravity well.
    /// </summary>
    [DataField("minRange")]
    public (float min, float max) MinRange = (0f, 0f);

    /// <summary>
    ///  The bounds on the inner range of the produced gravity well.
    /// </summary>
    [DataField("maxRange")]
    public (float min, float max) MaxRange = (0f, 0f);

    /// <summary>
    /// The bounds on the amount of radial acceleration the produced gravity well causes.
    /// </summary>
    [DataField("radialAcceleration")]
    public (float min, float max) RadialAcceleration = (0f, 0f);

    /// <summary>
    /// The bounds on the amount of tangential acceleration the produced gravity well causes.
    /// </summary>
    [DataField("tangentialAcceleration")]
    public (float min, float max) TangentialAcceleration = (0f, 0f);

    /// <summary>
    /// The bounds on the amount of time between gravitational pulses produced by the artifact.
    /// </summary>
    [DataField("pulsePeriod")]
    public (TimeSpan min, TimeSpan max) PulsePeriod = (TimeSpan.FromSeconds(.5f), TimeSpan.FromSeconds(.5f));

    /// <summary>
    /// Whether the artifact was already a gravity well when this was applied.
    /// </summary>
    public bool EntityWasAlreadyAGravityWell = false;
}
