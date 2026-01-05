using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Physics.Components;

/// <summary>
/// Attracts the singularity.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class RandomWalkAttractorComponent : Component
{
    /// <summary>
    /// Whitelist used to limit the type of random walker we want to attract.
    /// </summary>
    /// <remarks>
    /// RandomWalkComponent not necessary to include in the whitelist as its enforced regardless.
    /// </remarks>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    /// The range at which singularities will be unable to go away from the attractor.
    /// </summary>
    [DataField]
    public float BaseRange = 25f;

    /// <summary>
    /// The amount of time that should elapse between pulses of this attractor.
    /// </summary>
    [DataField]
    public TimeSpan TargetPulsePeriod = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The last time this attractor pulsed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan LastPulseTime = default!;
}
