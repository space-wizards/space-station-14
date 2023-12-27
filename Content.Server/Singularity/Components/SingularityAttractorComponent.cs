using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Components;

/// <summary>
/// Attracts the singularity.
/// </summary>
[RegisterComponent]
[Access(typeof(SingularityAttractorSystem))]
public sealed partial class SingularityAttractorComponent : Component
{
    /// <summary>
    /// The range at which singularities will be unable to go away from the attractor.
    /// </summary>
    [DataField("baseRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseRange { get; set; } = 25f;

    /// <summary>
    /// The amount of time that should elapse between pulses of this attractor.
    /// </summary>
    [DataField("targetPulsePeriod")]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TargetPulsePeriod { get; internal set; } = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// The last time this attractor pulsed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastPulseTime { get; internal set; } = default!;
}
