using Content.Shared.Physics.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Physics.Components;

/// <summary>
/// Attracts the singularity.
/// </summary>
[RegisterComponent]
[Access(typeof(RandomWalkAttractorSystem))]
public sealed partial class RandomWalkAttractorComponent : Component
{
    /// <summary>
    /// Pair component used to limit the type of random walker we want to attract.
    /// </summary>
    [DataField(required: true)]
    public string Component;

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
    public TimeSpan LastPulseTime = default!;
}
