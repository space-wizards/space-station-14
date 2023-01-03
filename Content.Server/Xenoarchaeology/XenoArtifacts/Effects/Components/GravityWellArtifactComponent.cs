using Content.Server.Singularity.Components;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// A component used to apply variation to the properties of a gravity well produced by an artifact.
/// The most of the properties of this component correspond 1:1 with properties of an actual <see cref="GravityWellComponent"/>
/// and define the range of possible values the corresponding property may have.
/// </summary>
[RegisterComponent]
public sealed class GravityWellArtifactComponent : Component
{
    /// <summary>
    /// The bounds on the outer range of the produced gravity well.
    /// The upper bound should be lower than the lower bound for <see cref="MaxRange"/>.
    /// See <see cref="GravityWellComponent.MaxRange"/> for the property this corresponds to.
    /// </summary>
    [DataField("minRange")]
    public (float min, float max) MinRange = (0f, 0f);

    /// <summary>
    /// The bounds on the inner range of the produced gravity well.
    /// The lower bound should be higher than the upper bound for <see cref="MinRange"/>.
    /// See <see cref="GravityWellComponent.MinRange"/> for the property this corresponds to.
    /// </summary>
    [DataField("maxRange")]
    public (float min, float max) MaxRange = (0f, 0f);

    /// <summary>
    /// The bounds on the amount of radial acceleration the produced gravity well causes.
    /// See <see cref="GravityWellComponent.BaseRadialAcceleration"/> for the property this corresponds to.
    /// </summary>
    [DataField("radialAcceleration")]
    public (float min, float max) RadialAcceleration = (0f, 0f);

    /// <summary>
    /// The bounds on the amount of tangential acceleration the produced gravity well causes.
    /// See <see cref="GravityWellComponent.BaseTangentialAcceleration"/> for the property this corresponds to.
    /// </summary>
    [DataField("tangentialAcceleration")]
    public (float min, float max) TangentialAcceleration = (0f, 0f);

    /// <summary>
    /// The bounds on the amount of time between gravitational pulses produced by the artifact.
    /// This also scales the amount of impulse applied to everything in range upon pulsing.
    /// See <see cref="GravityWellComponent.TargetPulsePeriod"/> for the property this corresponds to.
    /// </summary>
    [DataField("pulsePeriod")]
    public (TimeSpan min, TimeSpan max) PulsePeriod = (TimeSpan.FromSeconds(.5f), TimeSpan.FromSeconds(.5f));
}
