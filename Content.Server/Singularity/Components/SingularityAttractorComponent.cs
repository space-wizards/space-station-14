using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Components;

/// <summary>
/// Overrides exactly how much energy this object gives to a singularity.
/// </summary>
[RegisterComponent]
public sealed class SingularityAttractorComponent : Component
{
    /// <summary>
    /// The range at which singularities will be unable to go away from the attractor.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("baseRange")]
    public float BaseRange { get; set; } = 25f;

    /// <summary>
    /// The amount of time that should elapse between pulses of this attractor.
    /// </summary>
    [DataField("gravPulsePeriod")]
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(SingularityAttractorSystem))]
    public TimeSpan TargetPulsePeriod { get; internal set; } = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// The next time at which this attractor should pulse.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(SingularityAttractorSystem))]
    public TimeSpan NextPulseTime { get; internal set; } = default!;

    /// <summary>
    /// The last time this attractor pulsed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(SingularityAttractorSystem))]
    public TimeSpan LastPulseTime { get; internal set; } = default!;
}
