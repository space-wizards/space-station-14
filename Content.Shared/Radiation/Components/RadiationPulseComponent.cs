using Content.Shared.Radiation.Systems;
using Content.Shared.Spawners.Components;

namespace Content.Shared.Radiation.Components;

/// <summary>
///     Create circle pulse animation of radiation around object.
///     Drawn on client after creation only once per component lifetime.
/// </summary>
[RegisterComponent]
[Access(typeof(RadiationPulseSystem))]
public sealed partial class RadiationPulseComponent : Component
{
    /// <summary>
    ///     Timestamp when component was assigned to this entity.
    /// </summary>
    public TimeSpan StartTime;

    /// <summary>
    ///     How long will animation play in seconds.
    ///     Can be overridden by <see cref="TimedDespawnComponent"/>.
    /// </summary>
    public float VisualDuration = 2f;

    /// <summary>
    ///     The range of animation.
    ///     Can be overridden by <see cref="RadiationSourceComponent"/>.
    /// </summary>
    public float VisualRange = 5f;
}
